using UnityEngine;

public class GameUI : MonoBehaviour
{
    [SerializeField] DifferencePopup differencePopup = null;
    [SerializeField] TotalScorePopup totalScorePopup = null;
    [SerializeField] StreakPopup streakPopup = null;
    [SerializeField] GameOverPopup gameOverPopup = null;
    GameController gameController = null;

    public void Initialize(GameController controller)
    {
        UnsubscribeFromController();
        gameController = controller;
        ResolveGameOverPopup();
        if (gameOverPopup != null)
        {
            gameOverPopup.Initialize(controller);
        }
        SubscribeToController();
        HideGameOverPopup();
        SyncScoreDisplay();
        Debug.Log($"GameUI: Initialized. gameOverPopup={(gameOverPopup != null ? gameOverPopup.name : "null")} popupActive={(gameOverPopup != null && gameOverPopup.gameObject.activeSelf)}.", this);
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

    void HandleSceneCleared()
    {
        HideGameOverPopup();
        SyncScoreDisplay();
    }

    void ResolveGameOverPopup()
    {
        if (gameOverPopup != null)
        {
            Debug.Log($"GameUI: Using serialized GameOverPopup on '{gameOverPopup.name}'.", this);
            return;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name != "GameOver Popup")
            {
                continue;
            }

            gameOverPopup = children[i].GetComponent<GameOverPopup>();
            if (gameOverPopup == null)
            {
                Debug.Log("GameUI: Found GameOver Popup object without component; adding GameOverPopup.", this);
                gameOverPopup = children[i].gameObject.AddComponent<GameOverPopup>();
            }
            else
            {
                Debug.Log("GameUI: Found GameOverPopup via child search.", this);
            }

            return;
        }

        Debug.LogWarning("GameUI: Could not find GameOver Popup.", this);
    }

    void HideGameOverPopup()
    {
        if (gameOverPopup != null)
        {
            gameOverPopup.Hide();
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
        gameController.SceneCleared -= HandleSceneCleared;
        gameController.SceneCleared += HandleSceneCleared;
    }

    void UnsubscribeFromController()
    {
        if (gameController == null)
        {
            return;
        }

        gameController.ScoreChanged -= HandleScoreChanged;
        gameController.StreakChanged -= HandleStreakChanged;
        gameController.SceneCleared -= HandleSceneCleared;
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
