using UnityEngine;
using DG.Tweening;

public class PunchEffect : MonoBehaviour
{
    [SerializeField] Vector3 punchScale = new Vector3(0.2f, 0.2f, 0f);
    [SerializeField] float duration = 0.2f;
    [SerializeField] int vibrato = 8;
    [SerializeField] float elasticity = 0.8f;
    [SerializeField] Ease ease = Ease.OutExpo;
    [SerializeField] bool playOnEnable = false;

    Vector3 baseScale;
    Tween activeTween;
    bool hasBaseScale;

    void Awake()
    {
        CaptureBaseScale();
    }

    void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    void OnDisable()
    {
        KillActiveTween();
        CaptureBaseScale();
        transform.localScale = baseScale;
    }

    public void Punch()
    {
        Play();
    }

    public Tween Play()
    {
        CaptureBaseScale();
        KillActiveTween();
        transform.localScale = baseScale;
        activeTween = transform.DOPunchScale(punchScale, duration, vibrato, elasticity).SetEase(ease);
        return activeTween;
    }

    void CaptureBaseScale()
    {
        if (hasBaseScale)
        {
            return;
        }

        baseScale = transform.localScale;
        hasBaseScale = true;
    }

    void KillActiveTween()
    {
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
        }
    }
}
