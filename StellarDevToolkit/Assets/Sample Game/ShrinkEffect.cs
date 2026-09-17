using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class ShrinkEffect : MonoBehaviour
{
    public Transform target;

    [SerializeField] float duration = 0.45f;
    [SerializeField] Ease ease = Ease.OutCubic;
    [SerializeField] Ease moveEase = Ease.InCubic;
    [SerializeField] float overlayDistance = 0.35f;
    [SerializeField] bool destroyOnComplete = true;
    [SerializeField] bool playOnEnable = false;

    static readonly HashSet<ShrinkEffect> activeFlights = new HashSet<ShrinkEffect>();

    Tween activeTween;
    bool isPlaying;

    void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    void OnDisable()
    {
        activeFlights.Remove(this);
        KillActiveTween();
    }

    public static void DestroyActiveFlights()
    {
        if (activeFlights.Count == 0)
        {
            return;
        }

        List<ShrinkEffect> flights = new List<ShrinkEffect>(activeFlights);
        activeFlights.Clear();
        for (int i = 0; i < flights.Count; i++)
        {
            ShrinkEffect flight = flights[i];
            if (flight != null)
            {
                Destroy(flight.gameObject);
            }
        }
    }

    public Tween Play()
    {
        return Play(null);
    }

    public Tween Play(Transform flyTarget, float delay = 0f)
    {
        if (isPlaying)
        {
            return activeTween;
        }

        isPlaying = true;
        KillActiveTween();
        ReleaseFromBoard();

        Vector3 startScale = transform.localScale;
        Sequence sequence = DOTween.Sequence().SetLink(gameObject);
        if (flyTarget != null)
        {
            ScoreFxOverlay.Prepare(gameObject);
            Vector3 destination = GetOverlayWorldPoint(flyTarget.position);
            sequence.Insert(delay, transform.DOMove(destination, duration).SetEase(moveEase));
        }

        sequence.Insert(delay, DOTween.To(
            () => 0f,
            elapsed => transform.localScale = startScale * (1f - elapsed),
            1f,
            duration).SetEase(ease).SetTarget(transform));

        if (destroyOnComplete)
        {
            sequence.OnComplete(() => Destroy(gameObject));
        }

        activeTween = sequence;
        activeFlights.Add(this);
        return activeTween;
    }

    void ReleaseFromBoard()
    {
        transform.SetParent(null, true);
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    Vector3 GetOverlayWorldPoint(Vector3 uiWorldPoint)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            uiWorldPoint.z -= overlayDistance;
            return uiWorldPoint;
        }

        Vector3 toCamera = camera.transform.position - uiWorldPoint;
        if (toCamera.sqrMagnitude < 0.0001f)
        {
            return uiWorldPoint;
        }

        return uiWorldPoint + toCamera.normalized * overlayDistance;
    }

    void KillActiveTween()
    {
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
        }

        activeTween = null;
    }
}
