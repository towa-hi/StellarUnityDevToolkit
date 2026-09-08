using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using Stellar;
using Stellar.RPC;
using StellarSDK;

public class GameController : MonoBehaviour
{
    public event Action<int, int> ScoreChanged;
    public event Action<int> StreakChanged;
    public event Action GameOver;
    public event Action SceneCleared;

    const int DropSnapRangeTiles = 1;

    [SerializeField] Board board = null;
    [SerializeField] ShapeOfferArea offerArea = null;
    [SerializeField] InputController inputController = null;
    [SerializeField] GameUI gameUI = null;
    [SerializeField] float dragPlaneDepth = 0.0f;
    [FormerlySerializedAs("useDragYMultiplier")]
    [SerializeField] bool useDragAxisMultipliers = false;
    [SerializeField] float dragXMultiplier = 1.0f;
    [SerializeField] float dragYMultiplier = 2.0f;
    [SerializeField] float dragScaleDuration = 0.12f;
    [SerializeField] float dropSettleDuration = 0.12f;
    [SerializeField] float dropSnapEdgeDistance = 0.35f;

    public GamePhase State => gameState != null ? gameState.Phase : GamePhase.NotStarted;
    public int Score => gameState != null ? gameState.Score : 0;
    public int CurrentStreak => gameState != null ? gameState.CurrentStreak : 0;
    public uint GameSeed => gameState != null ? gameState.GameSeed : 0u;

    GameState gameState = null;
    ShapeTray draggedShape = null;
    ShapeOfferSlot draggedFromSlot = null;
    Vector2 dragOriginScreenCoordinate = Vector2.zero;
    Vector2 dragGrabOffset = Vector2.zero;
    Vector2 dragCursorBoardPosition = Vector2.zero;
    Tween dropSettleTween = null;
    readonly List<Vector2Int> previewCoordsBuffer = new List<Vector2Int>();
    readonly List<BoardCell> placementCellsBuffer = new List<BoardCell>();
    bool isSubmittingGameLog = false;
    bool isStartingGame = false;

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
        if (State == GamePhase.NotStarted)
        {
            return;
        }
        HandlePointerInput();
    }

    public void StartNewGame()
    {
        StartNewGame(GenerateNewGameSeed());
    }

    public async void StartNewGame(uint seed)
    {
        if (isStartingGame)
        {
            return;
        }

        if (board == null)
        {
            Debug.LogWarning("GameController: Cannot start game without Board.", this);
            return;
        }

        isStartingGame = true;
        try
        {
            ClearScene();
            gameState = new GameState(seed);
            gameState.Phase = GamePhase.NotStarted;
            if (TryGetLiveNetworkContext(out NetworkContext context))
            {
                bool loaded = await TryLoadShapeCatalogsFromContract(context, seed);
                if (!loaded)
                {
                    Debug.LogError("GameController: start_game_check failed; not starting.", this);
                    gameState = null;
                    return;
                }
            }

            InitializeScene();
            gameState.Phase = GamePhase.WaitingForDrag;
        }
        finally
        {
            isStartingGame = false;
        }
    }

    public async void SubmitGameLog()
    {
        if (isSubmittingGameLog)
        {
            return;
        }

        if (gameState == null)
        {
            Debug.LogWarning("GameController: Cannot submit game log without an active game.", this);
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameController: Cannot submit game log without GameManager.", this);
            return;
        }

        NetworkContext context = GameManager.Instance.CurrentNetworkContext;
        if (context.userAccount == null)
        {
            Debug.LogWarning("GameController: Cannot submit game log without a signed-in account.", this);
            return;
        }

        isSubmittingGameLog = true;
        try
        {
            SCVal player = StellarClient.AccountStringToScvAddress(context.userAccount.AccountId);
            SCVal[] args = { player, BuildGameLogVal() };
            Debug.Log($"GameController: register_game_log seed={gameState.GameSeed} score={gameState.Score} moves={gameState.PackedMoves.Count}.", this);
            Result<(SimulateTransactionResult, SendTransactionResult, GetTransactionResult)> result =
                await StellarClient.CallContractFunction(context, "register_game_log", args, GameManager.Instance.ClientTask);
            if (result.IsError)
            {
                Debug.LogError($"GameController: register_game_log failed: {result.Message}", this);
                return;
            }

            Debug.Log($"GameController: register_game_log succeeded txHash={result.Value.Item2.Hash} status={result.Value.Item3.Status}.", this);
        }
        catch (Exception exception)
        {
            Debug.LogError($"GameController: register_game_log threw {exception.Message}", this);
        }
        finally
        {
            isSubmittingGameLog = false;
        }
    }

    SCVal BuildGameLogVal()
    {
        byte[] packed = gameState.PackedMoves.ToArray();
        uint finalScore = gameState.Score < 0 ? 0u : (uint)gameState.Score;
        return SMap(
            Entry(Sym("final_score"), U32(finalScore)),
            Entry(Sym("packed_moves"), SMap(Entry(Sym("packed"), SBytes(packed)))),
            Entry(Sym("seed"), U64(gameState.GameSeed)),
            Entry(Sym("submitted_ledger_seq"), U64(0)));
    }

    static SCVal U32(uint value) => new SCVal.ScvU32 { u32 = new uint32(value) };
    static SCVal U64(ulong value) => new SCVal.ScvU64 { u64 = new uint64(value) };
    static SCVal Sym(string value) => new SCVal.ScvSymbol { sym = new SCSymbol(value) };
    static SCVal SBytes(byte[] values) => new SCVal.ScvBytes { bytes = new SCBytes(values ?? Array.Empty<byte>()) };
    static SCVal SMap(params SCMapEntry[] entries) => new SCVal.ScvMap { map = new SCMap(entries) };
    static SCMapEntry Entry(SCVal key, SCVal val) => new SCMapEntry { key = key, val = val };

#pragma warning disable 0649
    struct ShapeCatalogsEntry
    {
        public uint[] trominos;
        public uint[] tetrominos;
    }
#pragma warning restore 0649

    static bool TryGetLiveNetworkContext(out NetworkContext context)
    {
        context = default;
        if (GameManager.Instance == null)
        {
            return false;
        }

        context = GameManager.Instance.CurrentNetworkContext;
        return context.online && !string.IsNullOrWhiteSpace(context.contractAddress);
    }

    async Task<bool> TryLoadShapeCatalogsFromContract(NetworkContext context, uint seed)
    {
        SCVal[] args = { U64(seed) };
        StellarClientTask task = GameManager.Instance != null ? GameManager.Instance.ClientTask : null;
        Result<(Transaction, SimulateTransactionResult)> result =
            await StellarClient.SimulateContractFunction(context, "start_game_check", args, true, task);
        if (result.IsError || result.Value.Item2 == null || result.Value.Item2.Error != null)
        {
            Debug.LogError($"GameController: start_game_check failed: {result.Message}", this);
            return false;
        }

        SCVal raw = result.Value.Item2.Results?.FirstOrDefault()?.Result;
        if (raw == null)
        {
            Debug.LogError("GameController: start_game_check returned no result.", this);
            return false;
        }

        ShapeCatalogsEntry catalogs;
        try
        {
            catalogs = SCUtility.SCValToNative<ShapeCatalogsEntry>(raw);
        }
        catch (Exception exception)
        {
            Debug.LogError($"GameController: start_game_check decode failed: {exception.Message}", this);
            return false;
        }

        if (catalogs.trominos == null || catalogs.trominos.Length == 0
            || catalogs.tetrominos == null || catalogs.tetrominos.Length == 0)
        {
            Debug.LogError("GameController: start_game_check returned empty catalogs.", this);
            return false;
        }

        gameState.TrominoPackedShapes = ToIntArray(catalogs.trominos);
        gameState.TetrominoPackedShapes = ToIntArray(catalogs.tetrominos);
        return true;
    }

    static int[] ToIntArray(uint[] values)
    {
        int[] converted = new int[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            converted[i] = unchecked((int)values[i]);
        }

        return converted;
    }

    public void ClearScene()
    {
        KillDropSettleTween();
        if (draggedFromSlot != null)
        {
            draggedFromSlot.Clear();
        }

        if (draggedShape != null)
        {
            Destroy(draggedShape.gameObject);
        }

        ClearDragState();
        if (board != null)
        {
            board.Clear();
        }

        if (offerArea != null)
        {
            offerArea.Clear();
        }

        gameState = null;
        SceneCleared?.Invoke();
    }

    void InitializeScene()
    {
        board.InitializeBoard(GameUtility.GetBoardSize());
        if (offerArea != null)
        {
            offerArea.PopulateShapeOfferSlots(GeneratePackedShapeBatch);
        }
    }

    public int[] GeneratePackedShapeBatch(int count)
    {
        int[] batch = new int[Mathf.Max(0, count)];
        if (batch.Length == 0 || gameState == null)
        {
            return batch;
        }

        int trominoSlotIndex = gameState.NextIndex(batch.Length);
        for (int i = 0; i < batch.Length; i++)
        {
            int[] sourceShapes = i == trominoSlotIndex
                ? gameState.TrominoPackedShapes
                : gameState.TetrominoPackedShapes;
            batch[i] = PickRandomPackedShape(sourceShapes);
        }

        return batch;
    }

    int PickRandomPackedShape(int[] packedShapes)
    {
        if (packedShapes == null || packedShapes.Length == 0)
        {
            packedShapes = gameState != null && gameState.TetrominoPackedShapes != null
                ? gameState.TetrominoPackedShapes
                : BlockBlastConstants.TetrominoPackedShapes;
        }

        if (packedShapes == null || packedShapes.Length == 0)
        {
            return 0;
        }

        return packedShapes[gameState.NextIndex(packedShapes.Length)];
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
        if (State != GamePhase.WaitingForDrag || sourceSlot == null || !sourceSlot.HasShape())
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
        gameState.Phase = GamePhase.DraggingShape;

        Vector2 pointerScreen = inputController != null ? inputController.PointerScreenCoordinate : Vector2.zero;
        dragOriginScreenCoordinate = pointerScreen;
        dragGrabOffset = Vector2.zero;
        if (TryGetPointerBoardPosition(pointerScreen, out Vector2 pointerBoardPosition))
        {
            Vector2 trayXY = draggedShape.transform.position;
            dragGrabOffset = trayXY - pointerBoardPosition;
            SetDragCursorBoardPosition(trayXY);
        }
        else
        {
            dragCursorBoardPosition = Vector2.zero;
        }

        UpdateHoveredBoardCellPreview();

        return true;
    }

    public void UpdateActiveDrag(Vector2 screenCoordinate)
    {
        if (draggedShape == null)
        {
            if (board != null)
            {
                board.ClearPreviewHighlights();
            }
            return;
        }

        if (TryGetDragCursorBoardPosition(screenCoordinate, out Vector2 dragCursor))
        {
            SetDragCursorBoardPosition(dragCursor);
        }

        UpdateHoveredBoardCellPreview();
    }

    public void EndActiveDrag()
    {
        if (State == GamePhase.ResolvingPlacement)
        {
            return;
        }

        if (draggedShape == null || draggedFromSlot == null)
        {
            CancelActiveDrag();
            return;
        }

        if (inputController != null && TryGetDragCursorBoardPosition(inputController.PointerScreenCoordinate, out Vector2 dragCursor))
        {
            SetDragCursorBoardPosition(dragCursor);
        }

        ShapeTray shape = draggedShape;
        ShapeOfferSlot sourceSlot = draggedFromSlot;
        shape.ExitDragVisualState();

        if (TryResolvePlacementCell(dragCursorBoardPosition, out BoardCell destinationCell, out Vector2Int anchorCoord))
        {
            BeginBoardDrop(shape, sourceSlot, destinationCell, anchorCoord);
            return;
        }

        BeginSlotReturn(shape, sourceSlot);
    }

    public bool TryPlaceShape(ShapeDefinition shapeDefinition, Vector2Int anchorCoord, ShapeTray sourceShape = null, ShapeOfferSlot sourceSlot = null)
    {
        if (shapeDefinition == null || board == null)
        {
            return false;
        }

        int trayIndex = -1;
        if (gameState != null)
        {
            trayIndex = offerArea != null ? offerArea.OfferSlotIndex(sourceSlot) : -1;
            if (trayIndex < 0 || trayIndex > 2)
            {
                Debug.LogError("GameController: Cannot pack a move without an offer tray index.", this);
                return false;
            }
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
        int earnedScore = GameUtility.CalculatePlacementScore(placementResolution.ClearedLineCount, CurrentStreak);
        SetScore(Score + earnedScore);

        SetStreak(placementResolution.ClearedLineCount > 0
            ? CurrentStreak + 1
            : 0);

        if (gameState != null)
        {
            Vector2Int dropCell = GameUtility.GetDropCellCoord(anchorCoord);
            gameState.PackedMoves.Add(GameMovePacking.Pack(dropCell.x, dropCell.y, trayIndex));
        }

        return true;
    }

    public bool CheckForGameOver()
    {
        if (gameState == null)
        {
            Debug.Log("GameController: CheckForGameOver skipped because gameState is null.", this);
            return false;
        }

        bool hasAnyMove = HasAnyValidPlacement();
        Debug.Log($"GameController: CheckForGameOver phase={gameState.Phase} hasAnyMove={hasAnyMove} remainingOfferShapes={CountRemainingOfferShapes()} gameOverListeners={GetGameOverListenerCount()}.", this);
        if (!hasAnyMove)
        {
            gameState.Phase = GamePhase.GameOver;
            if (GameOver == null)
            {
                Debug.LogWarning("GameController: Game over reached but GameOver has no subscribers.", this);
            }
            else
            {
                Debug.Log("GameController: Invoking GameOver.", this);
            }

            GameOver?.Invoke();
            return true;
        }

        return false;
    }

    int CountRemainingOfferShapes()
    {
        if (offerArea == null)
        {
            return 0;
        }

        int count = 0;
        foreach (ShapeOfferSlot offerSlot in offerArea.OfferSlots)
        {
            if (offerSlot != null && offerSlot.HasShape())
            {
                count++;
            }
        }

        return count;
    }

    int GetGameOverListenerCount()
    {
        return GameOver == null ? 0 : GameOver.GetInvocationList().Length;
    }

    bool TryResolvePlacementCell(Vector2 dragCursor, out BoardCell destinationCell, out Vector2Int anchorCoord)
    {
        destinationCell = null;
        anchorCoord = default;
        if (board == null || draggedShape == null)
        {
            return false;
        }

        ShapeDefinition definition = draggedShape.Definition;
        if (definition == null)
        {
            return false;
        }

        Vector2Int cursorCell = GameUtility.GetCellCoord(dragCursor);
        if (board.TryGetCell(cursorCell, out BoardCell hoveredCell))
        {
            Vector2Int hoverAnchor = GameUtility.GetPlacementAnchorCoord(cursorCell);
            if (CanPlaceShape(definition, hoverAnchor))
            {
                destinationCell = hoveredCell;
                anchorCoord = hoverAnchor;
                return true;
            }
        }

        if (!IsWithinDropSnapEdgeDistance(dragCursor, cursorCell))
        {
            return false;
        }

        return TryGetNearestEmptyNeighborDestination(definition, cursorCell, dragCursor, out destinationCell, out anchorCoord);
    }

    bool IsWithinDropSnapEdgeDistance(Vector2 dragCursor, Vector2Int cursorCell)
    {
        float edgeDistance = GameUtility.IsOnBoard(cursorCell)
            ? GameUtility.GetDistanceToNearestCellEdge(dragCursor, cursorCell)
            : GameUtility.GetDistanceOutsideBoard(dragCursor);
        return edgeDistance <= dropSnapEdgeDistance;
    }

    bool TryGetNearestEmptyNeighborDestination(
        ShapeDefinition definition,
        Vector2Int referenceCoord,
        Vector2 dragCursor,
        out BoardCell destinationCell,
        out Vector2Int anchorCoord)
    {
        destinationCell = null;
        anchorCoord = default;

        BoardCell closestEmptyCell = null;
        float closestDistSq = float.MaxValue;
        for (int y = referenceCoord.y - DropSnapRangeTiles; y <= referenceCoord.y + DropSnapRangeTiles; y++)
        {
            for (int x = referenceCoord.x - DropSnapRangeTiles; x <= referenceCoord.x + DropSnapRangeTiles; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (coord == referenceCoord
                    || !board.TryGetCell(coord, out BoardCell cell)
                    || cell.IsOccupied)
                {
                    continue;
                }

                float distSq = (GameUtility.GetCellCenter(coord) - dragCursor).sqrMagnitude;
                if (distSq >= closestDistSq)
                {
                    continue;
                }

                closestDistSq = distSq;
                closestEmptyCell = cell;
            }
        }

        if (closestEmptyCell == null)
        {
            return false;
        }

        Vector2Int neighborAnchor = GameUtility.GetPlacementAnchorCoord(closestEmptyCell.Coord);
        if (!CanPlaceShape(definition, neighborAnchor))
        {
            return false;
        }

        destinationCell = closestEmptyCell;
        anchorCoord = neighborAnchor;
        return true;
    }

    void CancelActiveDrag()
    {
        KillDropSettleTween();
        ClearDragState();
    }

    void BeginBoardDrop(ShapeTray shape, ShapeOfferSlot sourceSlot, BoardCell hoveredBoardCell, Vector2Int anchorCoord)
    {
        gameState.Phase = GamePhase.ResolvingPlacement;
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
        bool placed = TryPlaceShape(shape.Definition, anchorCoord, shape, sourceSlot);
        Debug.Log($"GameController: CommitBoardDrop placed={placed}.", this);
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

        bool isGameOver = CheckForGameOver();
        Debug.Log($"GameController: After placement isGameOver={isGameOver} phase={State}.", this);
        if (!isGameOver && gameState != null)
        {
            gameState.Phase = GamePhase.WaitingForDrag;
        }
    }

    void BeginSlotReturn(ShapeTray shape, ShapeOfferSlot sourceSlot)
    {
        gameState.Phase = GamePhase.ResolvingPlacement;
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
        if (gameState != null)
        {
            gameState.Phase = GamePhase.WaitingForDrag;
        }
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
        dragGrabOffset = Vector2.zero;
        dragCursorBoardPosition = Vector2.zero;
    }

    void UpdateHoveredBoardCellPreview()
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
        if (definition == null || !TryResolvePlacementCell(dragCursorBoardPosition, out _, out Vector2Int anchorCoord))
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
        if (inputController == null || State == GamePhase.ResolvingPlacement || State == GamePhase.GameOver)
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
            UpdateActiveDrag(inputController.PointerScreenCoordinate);
        }

        if (inputController.PointerUpThisFrame)
        {
            EndActiveDrag();
        }
    }

    void SetDragCursorBoardPosition(Vector2 boardPosition)
    {
        dragCursorBoardPosition = boardPosition;
        if (draggedShape != null)
        {
            draggedShape.SetWorldDragPosition(new Vector3(boardPosition.x, boardPosition.y, 0.0f));
        }
    }

    bool TryGetDragCursorBoardPosition(Vector2 screenCoordinate, out Vector2 boardPosition)
    {
        if (!TryGetPointerBoardPosition(screenCoordinate, out Vector2 pointerBoardPosition))
        {
            boardPosition = default;
            return false;
        }

        boardPosition = pointerBoardPosition + dragGrabOffset;
        return true;
    }

    bool TryGetPointerBoardPosition(Vector2 screenCoordinate, out Vector2 boardPosition)
    {
        if (!TryGetDragPlanePoint(GetMappedDragScreenCoordinate(screenCoordinate), out Vector3 worldPoint))
        {
            boardPosition = default;
            return false;
        }

        boardPosition = new Vector2(worldPoint.x, worldPoint.y);
        return true;
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

    bool TryGetDragPlanePoint(Vector2 screenCoordinate, out Vector3 worldPoint)
    {
        if (inputController == null)
        {
            worldPoint = Vector3.zero;
            return false;
        }

        Plane dragPlane = new Plane(Vector3.forward, new Vector3(0.0f, 0.0f, dragPlaneDepth));
        return inputController.TryGetWorldPointOnPlane(screenCoordinate, dragPlane, out worldPoint);
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
        if (gameState == null)
        {
            return;
        }

        int scoreDifference = newScore - gameState.Score;
        gameState.Score = newScore;
        ScoreChanged?.Invoke(gameState.Score, scoreDifference);
    }

    void SetStreak(int newStreak)
    {
        if (gameState == null || gameState.CurrentStreak == newStreak)
        {
            return;
        }

        gameState.CurrentStreak = newStreak;
        StreakChanged?.Invoke(gameState.CurrentStreak);
    }
}
