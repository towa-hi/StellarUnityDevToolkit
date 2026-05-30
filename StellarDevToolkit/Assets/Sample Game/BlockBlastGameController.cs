using UnityEngine;
using System.Collections.Generic;

public class BlockBlastGameController : MonoBehaviour
{
    const string BoardCellLayerName = "BoardCell";

    [SerializeField] Board board = null;
    [SerializeField] ShapeOfferArea shapeOfferArea = null;
    [SerializeField] ShapeTray shapeTrayPrefab = null;
    [SerializeField] Camera gameplayCamera = null;
    [SerializeField] float dragPlaneDepth = 0.0f;
    [SerializeField] LayerMask boardCellLayerMask = 0;

    public GameState State { get; private set; } = GameState.WaitingForDrag;
    public int Score { get; private set; }

    ShapeTray draggedShape = null;
    ShapeOfferSlot draggedFromSlot = null;
    BoardCell highlightedBoardCell = null;
    void Awake()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        EnsureBoardCellLayerMask();
        ValidateSceneWiring();
    }

    void Update()
    {
        HandleDragInput();
    }

    void OnValidate()
    {
        EnsureBoardCellLayerMask();
        ValidateSceneWiring();
    }

    void EnsureBoardCellLayerMask()
    {
        int boardCellLayerIndex = LayerMask.NameToLayer(BoardCellLayerName);
        if (boardCellLayerIndex < 0)
        {
            Debug.LogWarning($"BlockBlastGameController: Layer '{BoardCellLayerName}' was not found. Assign boardCellLayerMask manually.", this);
            return;
        }

        boardCellLayerMask = 1 << boardCellLayerIndex;
    }

    public void StartNewGame()
    {
        if (board == null)
        {
            Debug.LogWarning("BlockBlastGameController: Cannot start game without Board.", this);
            return;
        }

        board.InitializeBoard(new Vector2Int(BlockBlastConstants.BoardSize, BlockBlastConstants.BoardSize));
        PopulateShapeOfferSlots();
        Score = 0;
        State = GameState.WaitingForDrag;
    }

    public void SetState(GameState state)
    {
        State = state;
    }

    public bool TryPlaceShape(ShapeDefinition shapeDefinition, Vector2Int anchorCoord)
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
            board.boardCells[targetCoord].SetOccupied(true);
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

    void HandleDragInput()
    {
        if (gameplayCamera == null)
        {
            return;
        }

        if (draggedShape == null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                TryBeginDrag();
            }

            return;
        }

        if (Input.GetMouseButton(0))
        {
            if (TryGetDragPlanePoint(out Vector3 dragPoint))
            {
                draggedShape.SetWorldDragPosition(dragPoint);
            }

            UpdateHoveredBoardCellPreview();
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }
    }

    void TryBeginDrag()
    {
        if (State == GameState.GameOver)
        {
            return;
        }

        if (!Physics.Raycast(gameplayCamera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo))
        {
            return;
        }

        ShapeTray shape = hitInfo.collider.GetComponentInParent<ShapeTray>();
        if (shape == null || shape.OwnerSlot == null)
        {
            return;
        }

        draggedShape = shape;
        draggedFromSlot = shape.OwnerSlot;
        draggedShape.transform.SetParent(null, true);
        draggedShape.EnterDragVisualState();
        State = GameState.DraggingShape;

        if (TryGetDragPlanePoint(out Vector3 dragPoint))
        {
            draggedShape.SetWorldDragPosition(dragPoint);
        }

        UpdateHoveredBoardCellPreview();
    }

    void EndDrag()
    {
        if (draggedShape == null || draggedFromSlot == null)
        {
            CancelActiveDrag();
            return;
        }

        bool placed = false;
        if (TryGetBoardCoordUnderCursor(out Vector2Int boardCoord))
        {
            placed = TryPlaceShape(draggedShape.Definition, boardCoord);
        }

        if (placed)
        {
            ShapeTray placedShape = draggedShape;
            draggedFromSlot.Clear();
            CancelActiveDrag();
            Destroy(placedShape.gameObject);
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

    void CancelActiveDrag()
    {
        ClearHoveredBoardCellPreview();
        draggedShape = null;
        draggedFromSlot = null;
    }

    bool TryGetDragPlanePoint(out Vector3 worldPoint)
    {
        Plane dragPlane = new Plane(Vector3.forward, new Vector3(0.0f, 0.0f, dragPlaneDepth));
        Ray ray = gameplayCamera.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float enterDistance))
        {
            worldPoint = ray.GetPoint(enterDistance);
            return true;
        }

        worldPoint = Vector3.zero;
        return false;
    }

    bool TryGetBoardCoordUnderCursor(out Vector2Int coord)
    {
        Ray ray = gameplayCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, boardCellLayerMask))
        {
            BoardCell boardCell = hitInfo.collider.GetComponentInParent<BoardCell>();
            if (boardCell != null)
            {
                coord = boardCell.Coord;
                return true;
            }
        }

        coord = default;
        return false;
    }

    void UpdateHoveredBoardCellPreview()
    {
        if (draggedShape == null)
        {
            ClearHoveredBoardCellPreview();
            return;
        }

        BoardCell nextHoveredBoardCell = null;
        Ray ray = gameplayCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, boardCellLayerMask))
        {
            nextHoveredBoardCell = hitInfo.collider.GetComponentInParent<BoardCell>();
        }

        if (highlightedBoardCell == nextHoveredBoardCell)
        {
            return;
        }

        if (highlightedBoardCell != null)
        {
            highlightedBoardCell.SetPreviewHighlight(false);
        }

        highlightedBoardCell = nextHoveredBoardCell;
        if (highlightedBoardCell != null)
        {
            highlightedBoardCell.SetPreviewHighlight(true);
        }
    }

    void ClearHoveredBoardCellPreview()
    {
        if (highlightedBoardCell == null)
        {
            return;
        }

        highlightedBoardCell.SetPreviewHighlight(false);
        highlightedBoardCell = null;
    }

    void ValidateSceneWiring()
    {
        if (board == null)
        {
            Debug.LogWarning("BlockBlastGameController: Board reference is missing.", this);
        }

        if (shapeOfferArea == null)
        {
            Debug.LogWarning("BlockBlastGameController: ShapeOfferArea reference is missing.", this);
            return;
        }

        if (shapeTrayPrefab == null)
        {
            Debug.LogWarning("BlockBlastGameController: ShapeTray prefab reference is missing.", this);
        }

        IReadOnlyList<ShapeOfferSlot> slots = shapeOfferArea.OfferSlots;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
            {
                Debug.LogWarning($"BlockBlastGameController: ShapeOfferArea slot {i} is missing.", this);
            }
        }
    }

    void PopulateShapeOfferSlots()
    {
        if (shapeOfferArea == null || shapeTrayPrefab == null)
        {
            return;
        }

        foreach (ShapeOfferSlot slot in shapeOfferArea.OfferSlots)
        {
            if (slot == null)
            {
                continue;
            }

            if (slot.HasShape())
            {
                Destroy(slot.CurrentShape.gameObject);
                slot.Clear();
            }

            Transform slotAnchor = slot.GetSlotAnchor();
            ShapeTray spawnedTray = Instantiate(shapeTrayPrefab, slotAnchor.position, slotAnchor.rotation);
            int randomPackedShapeData = GenerateRandomPackedShapeData();
            spawnedTray.InitializeFromPackedShapeData(randomPackedShapeData);
            slot.SetShape(spawnedTray);
        }
    }

    int GenerateRandomPackedShapeData()
    {
        int[] tetrominoes = BlockBlastConstants.TetrominoPackedShapes;
        if (tetrominoes == null || tetrominoes.Length == 0)
        {
            return 0;
        }

        int randomIndex = Random.Range(0, tetrominoes.Length);
        return tetrominoes[randomIndex];
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
            board.boardCells[coord].SetOccupied(false);
        }

        return cellsToClear.Count;
    }

    bool HasAnyValidPlacement()
    {
        if (board == null || shapeOfferArea == null)
        {
            return false;
        }

        foreach (ShapeOfferSlot offerSlot in shapeOfferArea.OfferSlots)
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
}
