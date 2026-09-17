using UnityEngine;
using TMPro;

public class TotalScorePopup : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI totalScoreText = null;
    [SerializeField] PunchEffect punchEffect = null;
    CountingLabel scoreLabel;

    public Transform FlyTarget => totalScoreText != null ? totalScoreText.transform : transform;

    void Awake()
    {
        if (punchEffect == null)
        {
            punchEffect = GetComponent<PunchEffect>();
        }

        scoreLabel = CountingLabel.Bind(totalScoreText);
    }

    void OnDisable()
    {
        scoreLabel?.Kill();
    }

    public void SetTotalScore(int totalScore)
    {
        scoreLabel?.Set(totalScore);
        if (punchEffect != null)
        {
            punchEffect.Punch();
        }
    }

    public void SetTotalScoreImmediate(int totalScore)
    {
        scoreLabel?.SetImmediate(totalScore);
    }
}
