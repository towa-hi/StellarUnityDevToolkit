using UnityEngine;
using UnityEngine.UI;

public class GameOverPopup : MonoBehaviour
{
    [SerializeField] Button backButton = null;
    [SerializeField] Button submitButton = null;

    void Awake()
    {
        ResolveButtons();
    }

    void OnEnable()
    {
        ResolveButtons();
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackClicked);
            backButton.onClick.AddListener(HandleBackClicked);
        }
    }

    void OnDisable()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HandleBackClicked);
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

    void ResolveButtons()
    {
        if (backButton == null)
        {
            Transform back = transform.Find("Back Button");
            if (back != null)
            {
                backButton = back.GetComponent<Button>();
            }
        }

        if (submitButton == null)
        {
            Transform submit = transform.Find("Submit Button");
            if (submit != null)
            {
                submitButton = submit.GetComponent<Button>();
            }
        }
    }

    void HandleBackClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToPluginDemo();
        }
    }
}
