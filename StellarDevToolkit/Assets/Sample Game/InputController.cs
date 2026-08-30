using UnityEngine;

public class InputController : MonoBehaviour
{
    const string BoardCellLayerName = "BoardCell";

    [SerializeField] Camera gameplayCamera = null;
    [SerializeField] LayerMask boardCellLayerMask = 0;

    public Vector2 PointerScreenCoordinate { get; private set; }
    public bool PointerDownThisFrame { get; private set; }
    public bool PointerHeld { get; private set; }
    public bool PointerUpThisFrame { get; private set; }
    public BoardCell HoveredBoardCell { get; private set; }
    public ShapeOfferSlot HoveredSelectionSlot { get; private set; }

    void Awake()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        if (LayerMask.NameToLayer(BoardCellLayerName) < 0)
        {
            Debug.LogWarning($"InputController: Layer '{BoardCellLayerName}' was not found. Assign boardCellLayerMask manually.", this);
        }

        EnsureBoardCellLayerMask();
    }

    void OnValidate()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }

        EnsureBoardCellLayerMask();
    }

    void Update()
    {
        UpdateHoverState();
    }

    void EnsureBoardCellLayerMask()
    {
        int boardCellLayerIndex = LayerMask.NameToLayer(BoardCellLayerName);
        if (boardCellLayerIndex < 0)
        {
            return;
        }

        boardCellLayerMask = 1 << boardCellLayerIndex;
    }

    void UpdateHoverState()
    {
        PointerDownThisFrame = IsPointerDownThisFrame();
        PointerHeld = IsPointerHeld();
        PointerUpThisFrame = IsPointerUpThisFrame();
        PointerScreenCoordinate = GetPointerScreenCoordinate();
        HoveredBoardCell = TryGetHoveredBoardCell(out BoardCell boardCell) ? boardCell : null;
        HoveredSelectionSlot = TryGetHoveredSelectionSlot(out ShapeOfferSlot slot) ? slot : null;
    }

    bool TryGetHoveredBoardCell(out BoardCell boardCell)
    {
        boardCell = null;
        if (gameplayCamera == null)
        {
            return false;
        }

        Ray ray = gameplayCamera.ScreenPointToRay(PointerScreenCoordinate);
        if (Physics.Raycast(ray, out RaycastHit hitInfo, float.MaxValue, boardCellLayerMask))
        {
            boardCell = hitInfo.collider.GetComponentInParent<BoardCell>();
            if (boardCell != null)
            {
                return true;
            }
        }

        return false;
    }

    bool TryGetHoveredSelectionSlot(out ShapeOfferSlot slot)
    {
        slot = null;
        if (gameplayCamera == null)
        {
            return false;
        }

        Ray ray = gameplayCamera.ScreenPointToRay(PointerScreenCoordinate);
        if (!Physics.Raycast(ray, out RaycastHit hitInfo))
        {
            return false;
        }

        ShapeTray shapeTray = hitInfo.collider.GetComponentInParent<ShapeTray>();
        if (shapeTray == null || shapeTray.OwnerSlot == null)
        {
            return false;
        }

        slot = shapeTray.OwnerSlot;
        return true;
    }

    Vector2 GetPointerScreenCoordinate()
    {
        return Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;
    }

    bool IsPointerDownThisFrame()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).phase == TouchPhase.Began;
        }

        return Input.GetMouseButtonDown(0);
    }

    bool IsPointerHeld()
    {
        if (Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            return phase == TouchPhase.Began || phase == TouchPhase.Moved || phase == TouchPhase.Stationary;
        }

        return Input.GetMouseButton(0);
    }

    bool IsPointerUpThisFrame()
    {
        if (Input.touchCount > 0)
        {
            TouchPhase phase = Input.GetTouch(0).phase;
            return phase == TouchPhase.Ended || phase == TouchPhase.Canceled;
        }

        return Input.GetMouseButtonUp(0);
    }
}
