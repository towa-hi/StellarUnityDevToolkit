using UnityEngine;

public class InputController : MonoBehaviour
{
    [SerializeField] Camera gameplayCamera = null;

    public Vector2 PointerScreenCoordinate { get; private set; }
    public bool PointerDownThisFrame { get; private set; }
    public bool PointerHeld { get; private set; }
    public bool PointerUpThisFrame { get; private set; }
    public ShapeOfferSlot HoveredSelectionSlot { get; private set; }

    void Awake()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }
    }

    void OnValidate()
    {
        if (gameplayCamera == null)
        {
            gameplayCamera = Camera.main;
        }
    }

    void Update()
    {
        PointerDownThisFrame = IsPointerDownThisFrame();
        PointerHeld = IsPointerHeld();
        PointerUpThisFrame = IsPointerUpThisFrame();
        PointerScreenCoordinate = GetPointerScreenCoordinate();
        HoveredSelectionSlot = TryGetHoveredSelectionSlot(out ShapeOfferSlot slot) ? slot : null;
    }

    public bool TryGetWorldPointOnPlane(Vector2 screenCoordinate, Plane plane, out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        if (gameplayCamera == null)
        {
            return false;
        }

        Ray ray = gameplayCamera.ScreenPointToRay(screenCoordinate);
        if (!plane.Raycast(ray, out float enterDistance))
        {
            return false;
        }

        worldPoint = ray.GetPoint(enterDistance);
        return true;
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
