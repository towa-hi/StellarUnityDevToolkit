using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StellarSDK;

public class TestWindow : MonoBehaviour
{
    public Button runTestsButton;
    public Button createTestnetAccountButton;
    public Button connectWalletButton;
    public Button startGameButton;
    public TMP_InputField sep50AssetContractAddressInputField;
    public TMP_InputField sep50OwnerAddressInputField;
    public Button setAssetOwnerToContextButton;
    public Button getSep50BalanceButton;
    public TextMeshProUGUI sep50BalanceResultText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        runTestsButton.onClick.AddListener(RunTests);
        createTestnetAccountButton.onClick.AddListener(CreateTestnetAccount);
        connectWalletButton.onClick.AddListener(ConnectWallet);
        startGameButton.onClick.AddListener(StartGame);
        setAssetOwnerToContextButton.onClick.AddListener(SetAssetOwnerToContext);
        getSep50BalanceButton.onClick.AddListener(GetSep50Balance);

        GameManager manager = GameManager.Instance;
        if (manager == null)
        {
            return;
        }

        if (sep50AssetContractAddressInputField != null && string.IsNullOrWhiteSpace(sep50AssetContractAddressInputField.text))
        {
            string defaultSep50AssetContractAddress = manager.defaultSettings != null
                ? manager.defaultSettings.sep50AssetContractAddress
                : null;
            sep50AssetContractAddressInputField.text = !string.IsNullOrWhiteSpace(defaultSep50AssetContractAddress)
                ? defaultSep50AssetContractAddress
                : manager.GetContextContractId();
        }
        if (sep50OwnerAddressInputField != null && string.IsNullOrWhiteSpace(sep50OwnerAddressInputField.text))
        {
            sep50OwnerAddressInputField.text = manager.GetContextAssetOwnerId();
        }
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

    async void GetSep50Balance()
    {
        if (sep50BalanceResultText != null)
        {
            sep50BalanceResultText.text = "Fetching balance...";
        }

        string assetContractAddress = sep50AssetContractAddressInputField != null ? sep50AssetContractAddressInputField.text : null;
        string ownerAddressOverride = sep50OwnerAddressInputField != null ? sep50OwnerAddressInputField.text : null;
        Result<int> result = await GameManager.Instance.GetSEP50AssetBalanceAsync(assetContractAddress, ownerAddressOverride);

        if (sep50BalanceResultText != null)
        {
            sep50BalanceResultText.text = result.IsOk
                ? result.Value.ToString()
                : $"Error: {result.Message}";
        }
    }

    void SetAssetOwnerToContext()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
        {
            return;
        }

        if (sep50OwnerAddressInputField != null)
        {
            sep50OwnerAddressInputField.text = manager.GetContextAssetOwnerId();
        }
    }
}
