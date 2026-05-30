using UnityEngine;
using DG.Tweening;

public class ShrinkEffect : MonoBehaviour
{
    public Transform target;

    [SerializeField] float duration = 0.2f;
    [SerializeField] Ease ease = Ease.InBack;
    [SerializeField] bool destroyOnComplete = true;
    [SerializeField] bool playOnEnable = false;

    Tween activeTween;
    bool isPlaying;

    Transform Target => target != null ? target : transform;

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
    }

    public Tween Shrink()
    {
        return Play();
    }

    public Tween Play()
    {
        if (isPlaying)
        {
            return activeTween;
        }

        isPlaying = true;
        KillActiveTween();
        activeTween = Target.DOScale(Vector3.zero, duration).SetEase(ease);
        if (destroyOnComplete)
        {
            activeTween.OnComplete(() => Destroy(gameObject));
        }

        return activeTween;
    }

    void KillActiveTween()
    {
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
        }
    }
}
