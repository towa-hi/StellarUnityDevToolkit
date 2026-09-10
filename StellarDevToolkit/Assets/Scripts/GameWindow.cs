using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameWindow : MonoBehaviour
{
    public TextMeshProUGUI piecesPlacedText;
    public TextMeshProUGUI pointsText;
    public TextMeshProUGUI streakText;
    public Button quitButton;
    public GameObject blocker;
    public GameOverPopup gameOverPopup;
    public RewardPopup rewardPopup;

    GameController gameController;

    void OnEnable()
    {
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuitClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        BindToController();
        HidePopups();
        RefreshDisplay();
    }

    void OnDisable()
    {
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        HidePopups();
        UnsubscribeFromController();
    }

    void BindToController()
    {
        GameController controller = GameManager.Instance != null
            ? GameManager.Instance.gameController
            : null;
        if (controller == gameController)
        {
            if (gameController != null)
            {
                SubscribeToController();
                InitializePopups();
            }

            return;
        }

        UnsubscribeFromController();
        gameController = controller;
        SubscribeToController();
        InitializePopups();
    }

    void InitializePopups()
    {
        if (gameOverPopup != null)
        {
            gameOverPopup.Initialize(gameController);
        }
    }

    void SubscribeToController()
    {
        if (gameController == null)
        {
            return;
        }

        gameController.StatsChanged -= HandleStatsChanged;
        gameController.StatsChanged += HandleStatsChanged;
        gameController.GameOver -= HandleGameOver;
        gameController.GameOver += HandleGameOver;
        gameController.SceneCleared -= HandleSceneCleared;
        gameController.SceneCleared += HandleSceneCleared;
        gameController.GameLogSubmitted -= HandleGameLogSubmitted;
        gameController.GameLogSubmitted += HandleGameLogSubmitted;
    }

    void UnsubscribeFromController()
    {
        if (gameController == null)
        {
            return;
        }

        gameController.StatsChanged -= HandleStatsChanged;
        gameController.GameOver -= HandleGameOver;
        gameController.SceneCleared -= HandleSceneCleared;
        gameController.GameLogSubmitted -= HandleGameLogSubmitted;
    }

    void HandleStatsChanged(int piecesPlaced, int score, int streak)
    {
        SetDisplay(piecesPlaced, score, streak);
    }

    void HandleGameOver()
    {
        if (rewardPopup != null)
        {
            rewardPopup.Hide();
        }

        SetBlockerActive(true);
        if (gameOverPopup != null)
        {
            gameOverPopup.Show();
        }
    }

    void HandleSceneCleared()
    {
        HidePopups();
    }

    void HandleGameLogSubmitted(int? rewardPoints)
    {
        if (gameOverPopup != null)
        {
            gameOverPopup.Hide();
        }

        SetBlockerActive(true);
        if (rewardPopup == null)
        {
            return;
        }

        string message = rewardPoints.HasValue
            ? $"You have received a {rewardPoints.Value} point reward"
            : "No reward this game";
        rewardPopup.Show(message);
    }

    void HidePopups()
    {
        if (gameOverPopup != null)
        {
            gameOverPopup.Hide();
        }

        if (rewardPopup != null)
        {
            rewardPopup.Hide();
        }

        SetBlockerActive(false);
    }

    void SetBlockerActive(bool active)
    {
        if (blocker != null)
        {
            blocker.SetActive(active);
        }
    }

    void RefreshDisplay()
    {
        if (gameController == null)
        {
            SetDisplay(0, 0, 0);
            return;
        }

        SetDisplay(gameController.PiecesPlaced, gameController.Score, gameController.CurrentStreak);
    }

    void SetDisplay(int piecesPlaced, int score, int streak)
    {
        if (piecesPlacedText != null)
        {
            piecesPlacedText.text = piecesPlaced.ToString();
        }

        if (pointsText != null)
        {
            pointsText.text = score.ToString();
        }

        if (streakText != null)
        {
            streakText.text = streak.ToString();
        }
    }

    void OnQuitClicked()
    {
        GameManager.Instance?.ReturnToPluginDemo();
    }
}
