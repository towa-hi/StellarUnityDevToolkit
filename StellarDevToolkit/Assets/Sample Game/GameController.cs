using UnityEngine;
using System.Collections.Generic;
using System;

public class GameController : MonoBehaviour
{
    public event Action<int, int> ScoreChanged;

    public enum GameState
    {
        NotStarted,
        WaitingForDrag,
        DraggingShape,
        ResolvingPlacement,
        GameOver
    }

    [SerializeField] Board board = null;
    [SerializeField] ShapeOfferArea offerArea = null;
    [SerializeField] InputController inputController = null;
    [SerializeField] GameUI gameUI = null;
    [SerializeField] Camera gameplayCamera = null;
    [SerializeField] float dragPlaneDepth = 0.0f;

    public GameState State { get; private set; } = GameState.NotStarted;
    public int Score { get; private set; }
    public uint GameSeed { get; private set; }
    int lineClearStreak = 0;

    ShapeTray draggedShape = null;
    ShapeOfferSlot draggedFromSlot = null;
    readonly List<Vector2Int> previewCoordsBuffer = new List<Vector2Int>();
    readonly List<BoardCell> placementCellsBuffer = new List<BoardCell>();
    IntegerRng gameRandom;

    void Awake()
    {
        if (gameUI != null)
        {
            gameUI.Initialize(this);
        }
    }

    void Update()
    {
        if (State == GameState.NotStarted)
        {
            return;
        }
        HandlePointerInput();
    }

    public void StartNewGame()
    {
        StartNewGame(GenerateNewGameSeed());
    }

    public void StartNewGame(uint seed)
    {
        if (board == null)
        {
            Debug.LogWarning("GameController: Cannot start game without Board.", this);
            return;
        }

        GameSeed = seed;
        gameRandom = new IntegerRng(GameSeed);

        CancelActiveDrag();
        board.InitializeBoard(GameUtility.GetBoardSize());
        if (offerArea != null)
        {
            offerArea.PopulateShapeOfferSlots(GeneratePackedShapeBatch);
        }

        SetScore(0);
        lineClearStreak = 0;
        State = GameState.WaitingForDrag;
    }

    public int[] GeneratePackedShapeBatch(int count)
    {
        int[] batch = new int[Mathf.Max(0, count)];
        if (batch.Length == 0)
        {
            return batch;
        }

        int trominoSlotIndex = gameRandom.NextIndex(batch.Length);
        for (int i = 0; i < batch.Length; i++)
        {
            int[] sourceShapes = i == trominoSlotIndex
                ? BlockBlastConstants.TrominoPackedShapes
                : BlockBlastConstants.TetrominoPackedShapes;
            batch[i] = PickRandomPackedShape(sourceShapes);
        }

        return batch;
    }

    int PickRandomPackedShape(int[] packedShapes)
    {
        if (packedShapes == null || packedShapes.Length == 0)
        {
            packedShapes = BlockBlastConstants.TetrominoPackedShapes;
        }

        if (packedShapes == null || packedShapes.Length == 0)
        {
            return 0;
        }

        return packedShapes[gameRandom.NextIndex(packedShapes.Length)];
    }

    static uint GenerateNewGameSeed()
    {
        uint seed = unchecked((uint)(Environment.TickCount ^ Guid.NewGuid().GetHashCode()));
        return seed == 0u ? 1u : seed;
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

        if (offerArea != null && !offerArea.CanBeginDragFrom(sourceSlot))
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
        if (shapeDefinition == null || board == null)
        {
            return false;
        }

        BoardState boardState = BuildBoardStateSnapshot();
        PlacementResolution placementResolution = ResolvePlacement(boardState, shapeDefinition, anchorCoord);
        if (!placementResolution.IsValid)
        {
            return false;
        }

        // Resolve every destination cell before mutating any of them, so a failed
        // lookup cannot leave the board half-placed.
        IReadOnlyList<Vector2Int> tileOffsets = shapeDefinition.TileOffsets;
        placementCellsBuffer.Clear();
        for (int i = 0; i < tileOffsets.Count; i++)
        {
            Vector2Int targetCoord = GameUtility.GetPlacementTargetCoord(anchorCoord, tileOffsets[i]);
            if (!board.TryGetCell(targetCoord, out BoardCell targetCell))
            {
                return false;
            }

            placementCellsBuffer.Add(targetCell);
        }

        for (int i = 0; i < tileOffsets.Count; i++)
        {
            BoardCell targetCell = placementCellsBuffer[i];
            if (sourceShape != null && sourceShape.TryGetTile(tileOffsets[i], out Tile tile) && tile != null)
            {
                AttachTileToBoardCell(tile, targetCell);
                targetCell.SetOccupiedState(true, tile);
            }
            else
            {
                targetCell.SetOccupiedState(true);
            }
        }

        ClearResolvedCells(placementResolution.ClearedCoords);
        int earnedScore = GameUtility.CalculatePlacementScore(placementResolution.ClearedLineCount, lineClearStreak);
        SetScore(Score + earnedScore);

        lineClearStreak = placementResolution.ClearedLineCount > 0
            ? lineClearStreak + 1
            : 0;

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

        anchorCoord = GameUtility.GetPlacementAnchorCoord(hoveredBoardCell.Coord);
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

        previewCoordsBuffer.Clear();
        foreach (Vector2Int offset in definition.TileOffsets)
        {
            Vector2Int targetCoord = GameUtility.GetPlacementTargetCoord(anchorCoord, offset);
            if (!board.TryGetCell(targetCoord, out _))
            {
                board.ClearPreviewHighlights();
                return;
            }

            previewCoordsBuffer.Add(targetCoord);
        }

        board.SetPreviewHighlights(previewCoordsBuffer);
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

    bool CanPlaceShape(ShapeDefinition shapeDefinition, Vector2Int anchorCoord)
    {
        if (board == null || shapeDefinition == null)
        {
            return false;
        }

        BoardState boardState = BuildBoardStateSnapshot();
        return CanPlaceShape(boardState, shapeDefinition, anchorCoord);
    }

    bool CanPlaceShape(BoardState boardState, ShapeDefinition shapeDefinition, Vector2Int anchorCoord)
    {
        if (shapeDefinition == null)
        {
            return false;
        }

        foreach (Vector2Int offset in shapeDefinition.TileOffsets)
        {
            Vector2Int targetCoord = GameUtility.GetPlacementTargetCoord(anchorCoord, offset);
            if (!boardState.IsInBounds(targetCoord) || boardState.IsOccupied(targetCoord))
            {
                return false;
            }
        }

        return true;
    }

    BoardState BuildBoardStateSnapshot()
    {
        return BoardState.FromBoard(board);
    }

    PlacementResolution ResolvePlacement(BoardState boardState, ShapeDefinition shapeDefinition, Vector2Int anchorCoord)
    {
        if (!CanPlaceShape(boardState, shapeDefinition, anchorCoord))
        {
            return PlacementResolution.Invalid();
        }

        BoardState placedState = boardState;
        foreach (Vector2Int offset in shapeDefinition.TileOffsets)
        {
            Vector2Int targetCoord = GameUtility.GetPlacementTargetCoord(anchorCoord, offset);
            placedState = placedState.WithCellOccupied(targetCoord, true);
        }

        HashSet<Vector2Int> cellsToClear = placedState.GetCompletedLineCells(out int clearedLineCount);
        Vector2Int[] clearedCoords = new Vector2Int[cellsToClear.Count];
        cellsToClear.CopyTo(clearedCoords);
        return new PlacementResolution(true, clearedLineCount, clearedCoords);
    }

    void ClearResolvedCells(IReadOnlyCollection<Vector2Int> cellsToClear)
    {
        if (board == null || cellsToClear == null || cellsToClear.Count == 0)
        {
            return;
        }

        foreach (Vector2Int coord in cellsToClear)
        {
            if (!board.TryGetCell(coord, out BoardCell boardCell))
            {
                continue;
            }

            Tile occupiedTile = boardCell.OccupiedTile;
            boardCell.SetOccupiedState(false);
            if (occupiedTile != null)
            {
                ShrinkEffect shrinkEffect = occupiedTile.GetComponent<ShrinkEffect>();
                if (shrinkEffect != null)
                {
                    shrinkEffect.Play();
                }
                else
                {
                    Destroy(occupiedTile.gameObject);
                }
            }
        }
    }

    bool HasAnyValidPlacement()
    {
        if (board == null || offerArea == null)
        {
            return false;
        }

        BoardState boardState = BuildBoardStateSnapshot();
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
                    Vector2Int anchorCoord = GameUtility.GetPlacementAnchorCoord(new Vector2Int(x, y));
                    if (CanPlaceShape(boardState, definition, anchorCoord))
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

    void SetScore(int newScore)
    {
        int scoreDifference = newScore - Score;
        Score = newScore;
        ScoreChanged?.Invoke(Score, scoreDifference);
    }
}
