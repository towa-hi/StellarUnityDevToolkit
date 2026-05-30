using UnityEngine;
using TMPro;

public class TotalScorePopup : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI totalScoreText = null;
    [SerializeField] PunchEffect punchEffect = null;

    void Awake()
    {
        if (punchEffect == null)
        {
            punchEffect = GetComponent<PunchEffect>();
        }

        RefreshTotalScoreText(0, false);
    }

    public void SetTotalScore(int totalScore)
    {
        RefreshTotalScoreText(totalScore, true);
    }

    public void SetTotalScoreImmediate(int totalScore)
    {
        RefreshTotalScoreText(totalScore, false);
    }

    void RefreshTotalScoreText(int totalScore, bool playPunch)
    {
        totalScoreText.text = totalScore.ToString();
        if (playPunch && punchEffect != null)
        {
            punchEffect.Punch();
        }
    }
}
