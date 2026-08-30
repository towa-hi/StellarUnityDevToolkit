using UnityEngine;
using TMPro;
using DG.Tweening;

public class DifferencePopup : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI differenceText = null;
    [SerializeField] PunchEffect punchEffect = null;

    Vector3 baseScale;
    public CanvasGroup canvasGroup;

    void Awake()
    {
        if (punchEffect == null)
        {
            punchEffect = GetComponent<PunchEffect>();
        }

        baseScale = transform.localScale;
        RefreshDifferenceText(0);
        HideImmediate();
    }

    public void SetDifference(int difference)
    {
        if (difference == 0)
        {
            return;
        }

        RefreshDifferenceText(difference);
        PlayPopupAnimation();
    }

    void RefreshDifferenceText(int difference)
    {
        differenceText.text = difference > 0 ? "+" + difference : difference.ToString();
    }

    void PlayPopupAnimation()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        if (punchEffect == null)
        {
            HideImmediate();
            return;
        }

        Tween punchTween = punchEffect.Play();
        punchTween.OnComplete(HideImmediate);
    }

    void HideImmediate()
    {
        transform.localScale = baseScale;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

    }
}
