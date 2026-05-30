using UnityEngine;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    [SerializeField] Board board = null;
    [SerializeField] ShapeOfferArea offerArea = null;
    [SerializeField] InputController inputController = null;
    [SerializeField] Camera gameplayCamera = null;
    [SerializeField] float dragPlaneDepth = 0.0f;

    public GameState State { get; private set; } = GameState.WaitingForDrag;
    public int Score { get; private set; }

    ShapeTray draggedShape = null;
    ShapeOfferSlot draggedFromSlot = null;

    void Awake()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        ValidateSceneWiring();
    }

    void Update()
    {
        HandlePointerInput();
    }

    void OnValidate()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        ValidateSceneWiring();
    }

    public void StartNewGame()
    {
        if (board == null)
        {
            Debug.LogWarning("GameController: Cannot start game without Board.", this);
            return;
        }

        CancelActiveDrag();
        board.InitializeBoard(new Vector2Int(BlockBlastConstants.BoardSize, BlockBlastConstants.BoardSize));
        if (offerArea != null)
        {
            offerArea.PopulateShapeOfferSlots();
        }

        Score = 0;
        State = GameState.WaitingForDrag;
    }

    public void SetState(GameState state)
    {
        State = state;
    }

    public bool HasActiveDrag()
    {
        return draggedShape != null;
    }

    public bool TryBeginDrag(ShapeOfferSlot sourceSlot)
    {
        if (State == GameState.GameOver || sourceSlot == null || !sourceSlot.HasShape())
        {
            return false;
        }

        ShapeTray shape = sourceSlot.CurrentShape;
        if (shape == null)
        {
            return false;
        }

        draggedShape = shape;
        draggedFromSlot = sourceSlot;
        draggedShape.transform.SetParent(null, true);
        draggedShape.EnterDragVisualState();
        State = GameState.DraggingShape;

        if (TryGetDragPlanePoint(inputController != null ? inputController.PointerScreenCoordinate : Vector2.zero, out Vector3 dragPoint))
        {
            draggedShape.SetWorldDragPosition(dragPoint);
        }

        UpdateHoveredBoardCellPreview(null);
        return true;
    }

    public void UpdateActiveDrag(Vector2 screenCoordinate, BoardCell hoveredBoardCell)
    {
        if (draggedShape == null)
        {
            if (board != null)
            {
                board.ClearPreviewHighlights();
            }
            return;
        }

        if (TryGetDragPlanePoint(screenCoordinate, out Vector3 dragPoint))
        {
            draggedShape.SetWorldDragPosition(dragPoint);
        }

        UpdateHoveredBoardCellPreview(hoveredBoardCell);
    }

    public void EndActiveDrag(BoardCell hoveredBoardCell)
    {
        if (draggedShape == null || draggedFromSlot == null)
        {
            CancelActiveDrag();
            return;
        }

        bool placed = false;
        if (TryGetPlacementAnchorFromHoveredCell(hoveredBoardCell, out Vector2Int anchorCoord))
        {
            placed = TryPlaceShape(draggedShape.Definition, anchorCoord, draggedShape);
        }

        if (placed)
        {
            ShapeTray placedShape = draggedShape;
            ShapeOfferSlot sourceSlot = draggedFromSlot;
            CancelActiveDrag();

            if (offerArea != null)
            {
                offerArea.ConsumePlacedShape(sourceSlot, placedShape);
            }
            else
            {
                sourceSlot.Clear();
                Destroy(placedShape.gameObject);
            }

            State = GameState.ResolvingPlacement;
            if (!CheckForGameOver())
            {
                State = GameState.WaitingForDrag;
            }
            return;
        }

        draggedShape.ExitDragVisualState();
        draggedFromSlot.SetShape(draggedShape);
        CancelActiveDrag();
        State = GameState.WaitingForDrag;
    }

    public bool TryPlaceShape(ShapeDefinition shapeDefinition, Vector2Int anchorCoord, ShapeTray sourceShape = null)
    {
        if (shapeDefinition == null)
        {
            return false;
        }

        if (!CanPlaceShape(shapeDefinition, anchorCoord))
        {
            return false;
        }

        foreach (Vector2Int offset in shapeDefinition.TileOffsets)
        {
            Vector2Int targetCoord = anchorCoord + offset;
            BoardCell targetCell = board.boardCells[targetCoord];

            if (sourceShape != null && sourceShape.TryGetTile(offset, out Tile tile) && tile != null)
            {
                AttachTileToBoardCell(tile, targetCell);
                targetCell.SetOccupied(tile);
            }
            else
            {
                targetCell.SetOccupied(true);
            }
        }

        Score += shapeDefinition.TileOffsets.Count;
        int clearedTileCount = ClearCompletedLines();
        Score += clearedTileCount;

        return true;
    }

    public bool CheckForGameOver()
    {
        bool hasAnyMove = HasAnyValidPlacement();
        if (!hasAnyMove)
        {
            State = GameState.GameOver;
            return true;
        }

        return false;
    }

    bool TryGetPlacementAnchorFromHoveredCell(BoardCell hoveredBoardCell, out Vector2Int anchorCoord)
    {
        if (hoveredBoardCell == null)
        {
            anchorCoord = default;
            return false;
        }

        int pivotOffset = ShapeDefinition.GridSize / 2;
        anchorCoord = hoveredBoardCell.Coord - new Vector2Int(pivotOffset, pivotOffset);
        return true;
    }

    void CancelActiveDrag()
    {
        if (board != null)
        {
            board.ClearPreviewHighlights();
        }

        draggedShape = null;
        draggedFromSlot = null;
    }

    void UpdateHoveredBoardCellPreview(BoardCell hoveredBoardCell)
    {
        if (draggedShape == null || board == null)
        {
            if (board != null)
            {
                board.ClearPreviewHighlights();
            }
            return;
        }

        ShapeDefinition definition = draggedShape.Definition;
        if (definition == null || !TryGetPlacementAnchorFromHoveredCell(hoveredBoardCell, out Vector2Int anchorCoord) || !CanPlaceShape(definition, anchorCoord))
        {
            board.ClearPreviewHighlights();
            return;
        }

        List<Vector2Int> previewCoords = new List<Vector2Int>();
        foreach (Vector2Int offset in definition.TileOffsets)
        {
            Vector2Int targetCoord = anchorCoord + offset;
            if (!board.boardCells.ContainsKey(targetCoord))
            {
                board.ClearPreviewHighlights();
                return;
            }

            previewCoords.Add(targetCoord);
        }

        board.SetPreviewHighlights(previewCoords);
    }

    void HandlePointerInput()
    {
        if (inputController == null)
        {
            return;
        }

        if (!HasActiveDrag())
        {
            if (inputController.PointerDownThisFrame && inputController.HoveredSelectionSlot != null)
            {
                TryBeginDrag(inputController.HoveredSelectionSlot);
            }

            return;
        }

        if (inputController.PointerHeld)
        {
            UpdateActiveDrag(inputController.PointerScreenCoordinate, inputController.HoveredBoardCell);
        }

        if (inputController.PointerUpThisFrame)
        {
            EndActiveDrag(inputController.HoveredBoardCell);
        }
    }

    bool TryGetDragPlanePoint(Vector2 screenCoordinate, out Vector3 worldPoint)
    {
        if (gameplayCamera == null)
        {
            worldPoint = Vector3.zero;
            return false;
        }

        Plane dragPlane = new Plane(Vector3.forward, new Vector3(0.0f, 0.0f, dragPlaneDepth));
        Ray ray = gameplayCamera.ScreenPointToRay(screenCoordinate);
        if (dragPlane.Raycast(ray, out float enterDistance))
        {
            worldPoint = ray.GetPoint(enterDistance);
            return true;
        }

        worldPoint = Vector3.zero;
        return false;
    }

    void ValidateSceneWiring()
    {
        if (board == null)
        {
            Debug.LogWarning("GameController: Board reference is missing.", this);
        }

        if (offerArea == null)
        {
            Debug.LogWarning("GameController: ShapeOfferArea reference is missing.", this);
        }

        if (inputController == null)
        {
            Debug.LogWarning("GameController: InputController reference is missing.", this);
        }

        if (gameplayCamera == null)
        {
            Debug.LogWarning("GameController: Gameplay camera reference is missing.", this);
        }

        if (offerArea != null)
        {
            offerArea.ValidateSceneWiring(this);
        }
    }

    bool CanPlaceShape(ShapeDefinition shapeDefinition, Vector2Int anchorCoord)
    {
        if (board == null || board.boardCells == null || shapeDefinition == null)
        {
            return false;
        }

        foreach (Vector2Int offset in shapeDefinition.TileOffsets)
        {
            Vector2Int targetCoord = anchorCoord + offset;
            if (!board.boardCells.TryGetValue(targetCoord, out BoardCell boardCell) || boardCell.IsOccupied)
            {
                return false;
            }
        }

        return true;
    }

    int ClearCompletedLines()
    {
        HashSet<Vector2Int> cellsToClear = new HashSet<Vector2Int>();
        int boardSize = BlockBlastConstants.BoardSize;

        for (int y = 0; y < boardSize; y++)
        {
            bool isFullRow = true;
            for (int x = 0; x < boardSize; x++)
            {
                if (!board.boardCells.TryGetValue(new Vector2Int(x, y), out BoardCell cell) || !cell.IsOccupied)
                {
                    isFullRow = false;
                    break;
                }
            }

            if (isFullRow)
            {
                for (int x = 0; x < boardSize; x++)
                {
                    cellsToClear.Add(new Vector2Int(x, y));
                }
            }
        }

        for (int x = 0; x < boardSize; x++)
        {
            bool isFullColumn = true;
            for (int y = 0; y < boardSize; y++)
            {
                if (!board.boardCells.TryGetValue(new Vector2Int(x, y), out BoardCell cell) || !cell.IsOccupied)
                {
                    isFullColumn = false;
                    break;
                }
            }

            if (isFullColumn)
            {
                for (int y = 0; y < boardSize; y++)
                {
                    cellsToClear.Add(new Vector2Int(x, y));
                }
            }
        }

        foreach (Vector2Int coord in cellsToClear)
        {
            if (!board.boardCells.TryGetValue(coord, out BoardCell boardCell))
            {
                continue;
            }

            Tile occupiedTile = boardCell.OccupiedTile;
            boardCell.SetOccupied(false);
            if (occupiedTile != null)
            {
                Destroy(occupiedTile.gameObject);
            }
        }

        return cellsToClear.Count;
    }

    bool HasAnyValidPlacement()
    {
        if (board == null || offerArea == null)
        {
            return false;
        }

        foreach (ShapeOfferSlot offerSlot in offerArea.OfferSlots)
        {
            if (offerSlot == null || !offerSlot.HasShape())
            {
                continue;
            }

            ShapeDefinition definition = offerSlot.CurrentShape.Definition;
            if (definition == null)
            {
                continue;
            }

            for (int y = 0; y < BlockBlastConstants.BoardSize; y++)
            {
                for (int x = 0; x < BlockBlastConstants.BoardSize; x++)
                {
                    if (CanPlaceShape(definition, new Vector2Int(x, y)))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    void AttachTileToBoardCell(Tile tile, BoardCell boardCell)
    {
        if (tile == null || boardCell == null)
        {
            return;
        }

        tile.transform.SetParent(boardCell.transform, false);
        tile.transform.localPosition = Vector3.zero;
        tile.transform.localRotation = Quaternion.identity;
        tile.transform.localScale = Vector3.one;
    }
}
