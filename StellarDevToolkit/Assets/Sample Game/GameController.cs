using UnityEngine;
using System.Collections.Generic;
using System;

public struct BoardState
{
    public const int Size = BlockBlastConstants.BoardSize;

    public readonly ulong OccupiedBits;

    public BoardState(ulong occupiedBits)
    {
        OccupiedBits = occupiedBits;
    }

    public static BoardState Empty => new BoardState(0UL);

    public static BoardState FromBoard(Board board)
    {
        if (board == null)
        {
            return Empty;
        }

        ulong occupiedBits = 0UL;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (!board.TryGetCell(coord, out BoardCell boardCell) || !boardCell.IsOccupied)
                {
                    continue;
                }

                int bitIndex = ToBitIndex(x, y);
                occupiedBits |= 1UL << bitIndex;
            }
        }

        return new BoardState(occupiedBits);
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Size && y >= 0 && y < Size;
    }

    public bool IsInBounds(Vector2Int coord)
    {
        return IsInBounds(coord.x, coord.y);
    }

    public bool IsOccupied(int x, int y)
    {
        if (!IsInBounds(x, y))
        {
            return false;
        }

        int bitIndex = ToBitIndex(x, y);
        return (OccupiedBits & (1UL << bitIndex)) != 0;
    }

    public bool IsOccupied(Vector2Int coord)
    {
        return IsOccupied(coord.x, coord.y);
    }

    public BoardState WithCellOccupied(int x, int y, bool occupied)
    {
        if (!IsInBounds(x, y))
        {
            return this;
        }

        int bitIndex = ToBitIndex(x, y);
        ulong mask = 1UL << bitIndex;
        ulong nextBits = occupied ? OccupiedBits | mask : OccupiedBits & ~mask;
        return new BoardState(nextBits);
    }

    public BoardState WithCellOccupied(Vector2Int coord, bool occupied)
    {
        return WithCellOccupied(coord.x, coord.y, occupied);
    }

    public BoardState ClearCells(IEnumerable<Vector2Int> coords)
    {
        if (coords == null)
        {
            return this;
        }

        ulong nextBits = OccupiedBits;
        foreach (Vector2Int coord in coords)
        {
            if (!IsInBounds(coord))
            {
                continue;
            }

            int bitIndex = ToBitIndex(coord.x, coord.y);
            nextBits &= ~(1UL << bitIndex);
        }

        return new BoardState(nextBits);
    }

    static int ToBitIndex(int x, int y)
    {
        return y * Size + x;
    }
}

public struct PlacementResolution
{
    public readonly bool IsValid;
    public readonly int PlacedTileCount;
    public readonly int ClearedCellCount;
    public readonly int ClearedLineCount;
    public readonly Vector2Int[] ClearedCoords;
    public readonly BoardState BoardStateAfterPiecePlacement;
    public readonly BoardState BoardStateAfterPlacement;

    public PlacementResolution(
        bool isValid,
        int placedTileCount,
        int clearedCellCount,
        int clearedLineCount,
        Vector2Int[] clearedCoords,
        BoardState boardStateAfterPiecePlacement,
        BoardState boardStateAfterPlacement)
    {
        IsValid = isValid;
        PlacedTileCount = placedTileCount;
        ClearedCellCount = clearedCellCount;
        ClearedLineCount = clearedLineCount;
        ClearedCoords = clearedCoords ?? new Vector2Int[0];
        BoardStateAfterPiecePlacement = boardStateAfterPiecePlacement;
        BoardStateAfterPlacement = boardStateAfterPlacement;
    }

    public static PlacementResolution Invalid(BoardState state)
    {
        return new PlacementResolution(false, 0, 0, 0, null, state, state);
    }
}

public struct IntegerRng
{
    public uint State;

    public IntegerRng(uint seed)
    {
        State = seed == 0u ? 0x9E3779B9u : seed;
    }

    public uint NextU32()
    {
        uint x = State;
        if (x == 0u)
        {
            x = 0x9E3779B9u;
        }

        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        State = x;
        return x;
    }

    public int NextIndex(int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        return (int)(NextU32() % (uint)count);
    }
}

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

    [SerializeField] public GameState State = GameState.NotStarted;
    public int Score = 0;
    public uint GameSeed { get; private set; }
    int lineClearStreak = 0;

    ShapeTray draggedShape = null;
    ShapeOfferSlot draggedFromSlot = null;
    readonly List<Vector2Int> previewCoordsBuffer = new List<Vector2Int>();
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

        foreach (Vector2Int offset in shapeDefinition.TileOffsets)
        {
            Vector2Int targetCoord = GameUtility.GetPlacementTargetCoord(anchorCoord, offset);
            if (!board.TryGetCell(targetCoord, out BoardCell targetCell))
            {
                return false;
            }

            if (sourceShape != null && sourceShape.TryGetTile(offset, out Tile tile) && tile != null)
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
        int earnedScore = GameUtility.CalculatePlacementScore(
            boardState,
            placementResolution.BoardStateAfterPiecePlacement,
            lineClearStreak);
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
            return PlacementResolution.Invalid(boardState);
        }

        BoardState placedState = boardState;
        foreach (Vector2Int offset in shapeDefinition.TileOffsets)
        {
            Vector2Int targetCoord = GameUtility.GetPlacementTargetCoord(anchorCoord, offset);
            placedState = placedState.WithCellOccupied(targetCoord, true);
        }

        HashSet<Vector2Int> cellsToClear = GetCompletedLineCells(placedState, out int clearedLineCount);
        BoardState finalState = placedState.ClearCells(cellsToClear);

        Vector2Int[] clearedCoords = new Vector2Int[cellsToClear.Count];
        cellsToClear.CopyTo(clearedCoords);
        return new PlacementResolution(
            true,
            shapeDefinition.TileOffsets.Count,
            cellsToClear.Count,
            clearedLineCount,
            clearedCoords,
            placedState,
            finalState);
    }

    HashSet<Vector2Int> GetCompletedLineCells(BoardState boardState, out int clearedLineCount)
    {
        HashSet<Vector2Int> cellsToClear = new HashSet<Vector2Int>();
        int boardSize = BlockBlastConstants.BoardSize;
        clearedLineCount = 0;

        for (int y = 0; y < boardSize; y++)
        {
            bool isFullRow = true;
            for (int x = 0; x < boardSize; x++)
            {
                if (!boardState.IsOccupied(x, y))
                {
                    isFullRow = false;
                    break;
                }
            }

            if (isFullRow)
            {
                clearedLineCount++;
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
                if (!boardState.IsOccupied(x, y))
                {
                    isFullColumn = false;
                    break;
                }
            }

            if (isFullColumn)
            {
                clearedLineCount++;
                for (int y = 0; y < boardSize; y++)
                {
                    cellsToClear.Add(new Vector2Int(x, y));
                }
            }
        }

        return cellsToClear;
    }

    int ClearResolvedCells(IReadOnlyCollection<Vector2Int> cellsToClear)
    {
        if (board == null || cellsToClear == null || cellsToClear.Count == 0)
        {
            return 0;
        }

        int clearedCellCount = 0;
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

            clearedCellCount++;
        }

        return clearedCellCount;
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
