using UnityEngine;
using UnityEngine.UI;
using StellarSDK;

public class TestWindow : MonoBehaviour
{
    public Button runTestsButton;
    public Button createTestnetAccountButton;
    public Button connectWalletButton;
    public Button startGameButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        runTestsButton.onClick.AddListener(RunTests);
        createTestnetAccountButton.onClick.AddListener(CreateTestnetAccount);
        connectWalletButton.onClick.AddListener(ConnectWallet);
        startGameButton.onClick.AddListener(StartGame);
    }

    void RunTests()
    {
        GameManager.Instance.RunTests();
    }

    void CreateTestnetAccount()
    {
        GameManager.Instance.CreateTestnetAccount();
    }

    void ConnectWallet()
    {
        GameManager.Instance.ConnectWallet();
    }

    void StartGame()
    {
        GameManager.Instance.StartGame();
    }
}
