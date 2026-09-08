using UnityEngine;
using UnityEngine.UI;

public class MainWindow : MonoBehaviour
{
    public Button startGameButton;
    public Button runTestsButton;
    public Button marketButton;
    public Button backToConnectionButton;

    void Start()
    {
        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        if (runTestsButton != null)
        {
            runTestsButton.onClick.AddListener(OnRunTestsClicked);
        }

        if (marketButton != null)
        {
            marketButton.onClick.AddListener(OnMarketClicked);
        }

        if (backToConnectionButton != null)
        {
            backToConnectionButton.onClick.AddListener(OnBackToConnectionClicked);
        }
    }

    void OnStartGameClicked()
    {
        GameManager.Instance?.StartGame();
    }

    void OnRunTestsClicked()
    {
        GameManager.Instance?.RunTests();
    }

    void OnMarketClicked()
    {
        GameManager.Instance?.ShowMarketWindow();
    }

    void OnBackToConnectionClicked()
    {
        GameManager.Instance?.ShowConnectionWindow();
    }
}
