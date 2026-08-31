using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] DifferencePopup differencePopup = null;
    [SerializeField] TotalScorePopup totalScorePopup = null;
    [SerializeField] StreakPopup streakPopup = null;
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

    void HandleStreakChanged(int currentStreak)
    {
        if (streakPopup != null)
        {
            streakPopup.SetStreak(currentStreak);
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
        gameController.StreakChanged -= HandleStreakChanged;
        gameController.StreakChanged += HandleStreakChanged;
    }

    void UnsubscribeFromController()
    {
        if (gameController == null)
        {
            return;
        }

        gameController.ScoreChanged -= HandleScoreChanged;
        gameController.StreakChanged -= HandleStreakChanged;
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

        if (streakPopup != null && gameController != null)
        {
            streakPopup.SetStreakImmediate(gameController.CurrentStreak);
        }
    }
}
