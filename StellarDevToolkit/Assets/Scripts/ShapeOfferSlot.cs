using UnityEngine;

public class ShapeOfferSlot : MonoBehaviour
{
    [SerializeField] ShapeTray currentShape = null;
    [SerializeField] Transform slotAnchor = null;

    public ShapeTray CurrentShape => currentShape;

    public bool HasShape()
    {
        return currentShape != null;
    }

    public Transform GetSlotAnchor()
    {
        return slotAnchor != null ? slotAnchor : transform;
    }

    public void SetShape(ShapeTray shape)
    {
        currentShape = shape;
        if (currentShape == null)
        {
            return;
        }

        currentShape.SetOwnerSlot(this);
        currentShape.SnapToSlotPose();
    }

    public void Clear()
    {
        if (currentShape != null)
        {
            currentShape.SetOwnerSlot(null);
        }

        currentShape = null;
    }
}
