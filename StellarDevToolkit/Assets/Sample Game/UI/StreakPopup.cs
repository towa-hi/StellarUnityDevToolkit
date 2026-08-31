using UnityEngine;
using TMPro;

public class StreakPopup : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI streakText = null;
    [SerializeField] PunchEffect punchEffect = null;

    void Awake()
    {
        if (punchEffect == null)
        {
            punchEffect = GetComponent<PunchEffect>();
        }

        RefreshStreakText(0, false);
    }

    public void SetStreak(int streak)
    {
        RefreshStreakText(streak, true);
    }

    public void SetStreakImmediate(int streak)
    {
        RefreshStreakText(streak, false);
    }

    void RefreshStreakText(int streak, bool playPunch)
    {
        streakText.text = streak.ToString() + "x";
        if (playPunch && punchEffect != null)
        {
            punchEffect.Punch();
        }
    }
}
