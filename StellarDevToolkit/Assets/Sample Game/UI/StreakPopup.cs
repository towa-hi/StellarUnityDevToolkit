using UnityEngine;
using TMPro;

public class StreakPopup : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI streakText = null;
    [SerializeField] PunchEffect punchEffect = null;
    CountingLabel streakLabel;

    void Awake()
    {
        if (punchEffect == null)
        {
            punchEffect = GetComponent<PunchEffect>();
        }

        streakLabel = CountingLabel.Bind(streakText, value => value.ToString() + "x");
    }

    void OnDisable()
    {
        streakLabel?.Kill();
    }

    public void SetStreak(int streak)
    {
        streakLabel?.Set(streak);
        if (punchEffect != null)
        {
            punchEffect.Punch();
        }
    }

    public void SetStreakImmediate(int streak)
    {
        streakLabel?.SetImmediate(streak);
    }
}
