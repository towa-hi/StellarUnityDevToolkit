using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] DifferencePopup differencePopup = null;
    [SerializeField] TotalScorePopup totalScorePopup = null;
    GameController gameController = null;

    public void Initialize(GameController controller)
    {
        UnsubscribeFromController();
        gameController = controller;
        SubscribeToController();
        SyncScoreDisplay();
    }

    void OnDestroy()
    {
        UnsubscribeFromController();
    }

    void HandleScoreChanged(int totalScore, int scoreDifference)
    {
        if (differencePopup != null)
        {
            differencePopup.SetDifference(scoreDifference);
        }

        if (totalScorePopup != null)
        {
            totalScorePopup.SetTotalScore(totalScore);
        }
    }

    void SubscribeToController()
    {
        if (gameController == null)
        {
            return;
        }

        gameController.ScoreChanged -= HandleScoreChanged;
        gameController.ScoreChanged += HandleScoreChanged;
    }

    void UnsubscribeFromController()
    {
        if (gameController == null)
        {
            return;
        }

        gameController.ScoreChanged -= HandleScoreChanged;
    }

    void SyncScoreDisplay()
    {
        if (differencePopup != null)
        {
            differencePopup.SetDifference(0);
        }

        if (totalScorePopup != null && gameController != null)
        {
            totalScorePopup.SetTotalScoreImmediate(gameController.Score);
        }
    }
}
