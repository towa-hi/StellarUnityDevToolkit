using UnityEngine;
using TMPro;
public class ArrowContainer : MonoBehaviour
{
    public GameObject arrow;
    public Transform arrowImageTransform;
    public TextMeshProUGUI label;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SetArrow(bool isLeft, string label)
    {
        this.label.text = label;
        arrowImageTransform.localRotation = isLeft ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 0, 180);
    }

    public void SetArrowVisible(bool visible)
    {
        // set arrow visible without changing the transform
        arrow.SetActive(visible);
    }
}
