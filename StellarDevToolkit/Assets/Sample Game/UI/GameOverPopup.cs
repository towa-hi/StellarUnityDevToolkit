using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverPopup : MonoBehaviour
{
    [SerializeField] Button backButton = null;
    [SerializeField] Button submitButton = null;
    [SerializeField] Button retryButton = null;
    [SerializeField] TextMeshProUGUI infoText = null;

    GameController gameController = null;

    public void Initialize(GameController controller)
    {
        gameController = controller;
    }

    void OnEnable()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackClicked);
            backButton.onClick.AddListener(HandleBackClicked);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(HandleRetryClicked);
            retryButton.onClick.AddListener(HandleRetryClicked);
        }

        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(HandleSubmitClicked);
            submitButton.onClick.AddListener(HandleSubmitClicked);
        }
    }

    void OnDisable()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackClicked);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(HandleRetryClicked);
        }

        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(HandleSubmitClicked);
        }
    }

    public void Show()
    {
        SetButtonsInteractable(true);
        RefreshInfoText();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void RefreshInfoText()
    {
        if (infoText == null)
        {
            return;
        }

        int pieces = gameController != null ? gameController.PiecesPlaced : 0;
        int score = gameController != null ? gameController.Score : 0;
        int longest = gameController != null ? gameController.LongestStreak : 0;
        infoText.text = $"Number of placed pieces: {pieces}\nYour final score: {score}\nYour longest streak: {longest}";
    }

    void HandleBackClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToPluginDemo();
        }
    }

    void HandleRetryClicked()
    {
        if (gameController != null)
        {
            gameController.StartNewGame();
            return;
        }

        Debug.LogWarning("GameOverPopup: Retry clicked but GameController is missing.", this);
    }

    async void HandleSubmitClicked()
    {
        if (gameController == null)
        {
            Debug.LogWarning("GameOverPopup: Submit clicked but GameController is missing.", this);
            return;
        }

        SetButtonsInteractable(false);
        try
        {
            bool submitted = await gameController.SubmitGameLog();
            if (!submitted)
            {
                SetButtonsInteractable(true);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"GameOverPopup: Submit failed: {exception.Message}", this);
            SetButtonsInteractable(true);
        }
    }

    void SetButtonsInteractable(bool interactable)
    {
        if (backButton != null)
        {
            backButton.interactable = interactable;
        }

        if (retryButton != null)
        {
            retryButton.interactable = interactable;
        }

        if (submitButton != null)
        {
            submitButton.interactable = interactable;
        }
    }
}
