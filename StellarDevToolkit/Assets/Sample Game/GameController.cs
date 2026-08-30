using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using System;
using DG.Tweening;

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
    [FormerlySerializedAs("useDragYMultiplier")]
    [SerializeField] bool useDragAxisMultipliers = false;
    [SerializeField] float dragXMultiplier = 1.0f;
    [SerializeField] float dragYMultiplier = 2.0f;
    [SerializeField] float dragScaleDuration = 0.12f;
    [SerializeField] float dropSettleDuration = 0.12f;

    public GameState State { get; private set; } = GameState.NotStarted;
    public int Score { get; private set; }
    public uint GameSeed { get; private set; }
    int lineClearStreak = 0;

    ShapeTray draggedShape = null;
    ShapeOfferSlot draggedFromSlot = null;
    Vector2 dragOriginScreenCoordinate = Vector2.zero;
    Vector3 dragGrabOffset = Vector3.zero;
    Tween dropSettleTween = null;
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

    void OnDisable()
    {
        KillDropSettleTween();
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
        if (State != GameState.WaitingForDrag || sourceSlot == null || !sourceSlot.HasShape())
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
        draggedShape.EnterDragVisualState(dragScaleDuration);
        State = GameState.DraggingShape;

        Vector2 pointerScreen = inputController != null ? inputController.PointerScreenCoordinate : Vector2.zero;
        dragOriginScreenCoordinate = pointerScreen;
        if (TryGetDragWorldPoint(pointerScreen, out Vector3 dragPoint))
        {
            Vector3 trayPos = draggedShape.transform.position;
            dragGrabOffset = new Vector3(trayPos.x - dragPoint.x, trayPos.y - dragPoint.y, 0.0f);
            draggedShape.SetWorldDragPosition(ApplyDragGrabOffset(dragPoint));
            UpdateHoveredBoardCellPreview(ResolveDragHoverCell(pointerScreen));
        }
        else
        {
            dragGrabOffset = Vector3.zero;
            UpdateHoveredBoardCellPreview(null);
        }

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

        if (TryGetDragWorldPoint(screenCoordinate, out Vector3 dragPoint))
        {
            draggedShape.SetWorldDragPosition(ApplyDragGrabOffset(dragPoint));
            hoveredBoardCell = ResolveDragHoverCell(screenCoordinate, hoveredBoardCell);
        }

        UpdateHoveredBoardCellPreview(hoveredBoardCell);
    }

    public void EndActiveDrag(BoardCell hoveredBoardCell)
    {
        if (State == GameState.ResolvingPlacement)
        {
            return;
        }

        if (draggedShape == null || draggedFromSlot == null)
        {
            CancelActiveDrag();
            return;
        }

        if (inputController != null)
        {
            hoveredBoardCell = ResolveDragHoverCell(inputController.PointerScreenCoordinate, hoveredBoardCell);
        }

        ShapeTray shape = draggedShape;
        ShapeOfferSlot sourceSlot = draggedFromSlot;
        shape.ExitDragVisualState();

        if (TryGetPlacementAnchorFromHoveredCell(hoveredBoardCell, out Vector2Int anchorCoord)
            && CanPlaceShape(shape.Definition, anchorCoord))
        {
            BeginBoardDrop(shape, sourceSlot, hoveredBoardCell, anchorCoord);
            return;
        }

        BeginSlotReturn(shape, sourceSlot);
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
        KillDropSettleTween();
        ClearDragState();
    }

    void BeginBoardDrop(ShapeTray shape, ShapeOfferSlot sourceSlot, BoardCell hoveredBoardCell, Vector2Int anchorCoord)
    {
        State = GameState.ResolvingPlacement;
        Vector3 targetPos = hoveredBoardCell.transform.position;
        targetPos.z += 0.5f;
        Quaternion targetRot = hoveredBoardCell.transform.rotation;
        Tween settleTween = shape.LerpToWorldPose(targetPos, targetRot, shape.FullScale, dropSettleDuration);
        if (settleTween == null)
        {
            CommitBoardDrop(shape, sourceSlot, anchorCoord);
            return;
        }

        dropSettleTween = settleTween.OnComplete(() => CommitBoardDrop(shape, sourceSlot, anchorCoord));
    }

    void CommitBoardDrop(ShapeTray shape, ShapeOfferSlot sourceSlot, Vector2Int anchorCoord)
    {
        dropSettleTween = null;
        bool placed = TryPlaceShape(shape.Definition, anchorCoord, shape);
        if (!placed)
        {
            BeginSlotReturn(shape, sourceSlot);
            return;
        }

        ClearDragState();
        if (offerArea != null)
        {
            offerArea.ConsumePlacedShape(sourceSlot, shape);
        }
        else
        {
            sourceSlot.Clear();
            Destroy(shape.gameObject);
        }

        if (!CheckForGameOver())
        {
            State = GameState.WaitingForDrag;
        }
    }

    void BeginSlotReturn(ShapeTray shape, ShapeOfferSlot sourceSlot)
    {
        State = GameState.ResolvingPlacement;
        if (board != null)
        {
            board.ClearPreviewHighlights();
        }

        sourceSlot.SetShape(shape, snapToPose: false);
        Tween settleTween = shape.LerpToSlotPose(dropSettleDuration);
        if (settleTween == null)
        {
            CompleteSlotReturn();
            return;
        }

        dropSettleTween = settleTween.OnComplete(CompleteSlotReturn);
    }

    void CompleteSlotReturn()
    {
        dropSettleTween = null;
        ClearDragState();
        State = GameState.WaitingForDrag;
    }

    void KillDropSettleTween()
    {
        if (dropSettleTween != null && dropSettleTween.IsActive())
        {
            dropSettleTween.Kill();
        }

        dropSettleTween = null;
    }

    void ClearDragState()
    {
        if (board != null)
        {
            board.ClearPreviewHighlights();
        }

        draggedShape = null;
        draggedFromSlot = null;
        dragOriginScreenCoordinate = Vector2.zero;
        dragGrabOffset = Vector3.zero;
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
        if (inputController == null || State == GameState.ResolvingPlacement || State == GameState.GameOver)
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

    Vector3 ApplyDragGrabOffset(Vector3 dragPoint)
    {
        dragPoint.x += dragGrabOffset.x;
        dragPoint.y += dragGrabOffset.y;
        return dragPoint;
    }

    Vector2 GetMappedDragScreenCoordinate(Vector2 screenCoordinate)
    {
        if (useDragAxisMultipliers)
        {
            Vector2 delta = screenCoordinate - dragOriginScreenCoordinate;
            screenCoordinate.x = dragOriginScreenCoordinate.x + delta.x * dragXMultiplier;
            screenCoordinate.y = dragOriginScreenCoordinate.y + delta.y * dragYMultiplier;
        }

        return screenCoordinate;
    }

    bool TryGetDragWorldPoint(Vector2 screenCoordinate, out Vector3 worldPoint)
    {
        return TryGetDragPlanePoint(GetMappedDragScreenCoordinate(screenCoordinate), out worldPoint);
    }

    BoardCell ResolveDragHoverCell(Vector2 screenCoordinate, BoardCell pointerHoveredCell = null)
    {
        if (!useDragAxisMultipliers)
        {
            return pointerHoveredCell != null
                ? pointerHoveredCell
                : (inputController != null ? inputController.HoveredBoardCell : null);
        }

        if (inputController == null)
        {
            return null;
        }

        return inputController.TryGetBoardCellAtScreenCoordinate(GetMappedDragScreenCoordinate(screenCoordinate), out BoardCell hoverCell)
            ? hoverCell
            : null;
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
