using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardPopup : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI rewardText = null;
    [SerializeField] Button quitButton = null;

    void OnEnable()
    {
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(HandleQuitClicked);
            quitButton.onClick.AddListener(HandleQuitClicked);
        }
    }

    void OnDisable()
    {
        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(HandleQuitClicked);
        }
    }

    public void Show(string message)
    {
        if (rewardText != null)
        {
            rewardText.text = message;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    void HandleQuitClicked()
    {
        GameManager.Instance?.ReturnToPluginDemo();
    }
}
