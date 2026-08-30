using UnityEngine;

public class ShapeOfferSlot : MonoBehaviour
{
    [SerializeField] ShapeTray currentShape = null;
    [SerializeField] Transform slotAnchor = null;
    [SerializeField] bool isPreview = false;
    [SerializeField] float shapeScaleMultiplier = 1.0f;

    public ShapeTray CurrentShape => currentShape;
    public bool IsPreview => isPreview;
    public float ShapeScaleMultiplier => shapeScaleMultiplier;

    public bool HasShape()
    {
        return currentShape != null;
    }

    public Transform GetSlotAnchor()
    {
        return slotAnchor != null ? slotAnchor : transform;
    }

    public void ConfigureRole(bool preview, float scaleMultiplier)
    {
        isPreview = preview;
        shapeScaleMultiplier = Mathf.Max(0.1f, scaleMultiplier);
    }

    public void SetShape(ShapeTray shape, bool snapToPose = true)
    {
        currentShape = shape;
        if (currentShape == null)
        {
            return;
        }

        currentShape.SetOwnerSlot(this);
        if (snapToPose)
        {
            currentShape.SnapToSlotPose();
        }
    }

    public void Clear()
    {
        if (currentShape != null)
        {
            currentShape.SetOwnerSlot(null);
        }

        currentShape = null;
    }

    void OnValidate()
    {
        shapeScaleMultiplier = Mathf.Max(0.1f, shapeScaleMultiplier);
    }
}
