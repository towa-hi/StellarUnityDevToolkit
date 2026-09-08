using UnityEngine;
using UnityEngine.UI;

public class GameOverPopup : MonoBehaviour
{
    [SerializeField] Button backButton = null;
    [SerializeField] Button submitButton = null;

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

        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(HandleSubmitClicked);
        }
    }

    public void Show()
    {
        Debug.Log($"GameOverPopup: Show before activeSelf={gameObject.activeSelf} activeInHierarchy={gameObject.activeInHierarchy}.", this);
        gameObject.SetActive(true);
        Debug.Log($"GameOverPopup: Show after activeSelf={gameObject.activeSelf} activeInHierarchy={gameObject.activeInHierarchy} worldPos={transform.position}.", this);
    }

    public void Hide()
    {
        Debug.Log($"GameOverPopup: Hide activeSelf={gameObject.activeSelf}.", this);
        gameObject.SetActive(false);
    }

    void HandleBackClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToPluginDemo();
        }
    }

    void HandleSubmitClicked()
    {
        if (gameController != null)
        {
            gameController.SubmitGameLog();
            return;
        }

        Debug.LogWarning("GameOverPopup: Submit clicked but GameController is missing.", this);
    }
}
