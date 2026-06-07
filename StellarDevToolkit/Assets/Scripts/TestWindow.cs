using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Stellar;
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
    public Button mintSep50AssetButton;
    public TextMeshProUGUI sep50MintedIdResultText;
    public Button getSep50OwnerMapButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        runTestsButton.onClick.AddListener(RunTests);
        createTestnetAccountButton.onClick.AddListener(CreateTestnetAccount);
        connectWalletButton.onClick.AddListener(ConnectWallet);
        startGameButton.onClick.AddListener(StartGame);
        setAssetOwnerToContextButton.onClick.AddListener(SetAssetOwnerToContext);
        getSep50BalanceButton.onClick.AddListener(GetSep50Balance);
        mintSep50AssetButton.onClick.AddListener(MintSep50Asset);
        if (getSep50OwnerMapButton != null)
        {
            getSep50OwnerMapButton.onClick.AddListener(GetSep50OwnerMap);
        }

        PopulateDefaultFields();
    }

    public void PopulateDefaultFields(bool refreshOwnerFromContext = false)
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
        {
            return;
        }

        if (sep50AssetContractAddressInputField != null && string.IsNullOrWhiteSpace(sep50AssetContractAddressInputField.text))
        {
            sep50AssetContractAddressInputField.text = manager.GetDefaultSep50AssetContractAddress();
        }

        if (sep50OwnerAddressInputField != null)
        {
            string ownerId = manager.GetContextAssetOwnerId();
            if (refreshOwnerFromContext || string.IsNullOrWhiteSpace(sep50OwnerAddressInputField.text))
            {
                sep50OwnerAddressInputField.text = ownerId;
            }
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

    async void MintSep50Asset()
    {
        if (sep50MintedIdResultText != null)
        {
            sep50MintedIdResultText.text = "Minting...";
        }

        string assetContractAddress = sep50AssetContractAddressInputField != null ? sep50AssetContractAddressInputField.text : null;
        string ownerAddressOverride = sep50OwnerAddressInputField != null ? sep50OwnerAddressInputField.text : null;
        Result<SorobanInvocationMeta> result = await GameManager.Instance.MintSEP50AssetAsync(assetContractAddress, ownerAddressOverride);

        if (sep50MintedIdResultText != null)
        {
            if (!result.IsOk)
            {
                sep50MintedIdResultText.text = $"Error: {result.Message}";
                return;
            }

            Result<SCVal> returnValueResult = StellarClient.GetSorobanReturnValue(result.Value);
            if (returnValueResult.IsError)
            {
                sep50MintedIdResultText.text = $"Error: {returnValueResult.Message}";
                return;
            }

            Result<int> mintIdResult = StellarClient.GetU32ReturnValue(returnValueResult.Value);
            if (mintIdResult.IsError)
            {
                sep50MintedIdResultText.text = $"Error: {mintIdResult.Message}";
                return;
            }

            Result<SorobanFees> feesResult = StellarClient.GetSorobanFees(result.Value);
            string feesText = feesResult.IsOk
                ? $" (resource fee: {feesResult.Value.ResourceFeeCharged} stroops)"
                : string.Empty;
            sep50MintedIdResultText.text = $"{mintIdResult.Value}{feesText}";
        }
    }

    async void GetSep50OwnerMap()
    {
        string assetContractAddress = sep50AssetContractAddressInputField != null ? sep50AssetContractAddressInputField.text : null;
        Result<Dictionary<int, string>> result = await GameManager.Instance.GetSEP50AssetOwnerMapAsync(assetContractAddress);

        if (!result.IsOk)
        {
            Debug.LogError($"GetSEP50OwnerMap failed: {result.Message}");
            return;
        }

        var logBuilder = new StringBuilder();
        logBuilder.AppendLine($"GetSEP50OwnerMap: contract={assetContractAddress?.Trim()}, count={result.Value.Count}");
        foreach (var entry in result.Value)
        {
            logBuilder.AppendLine($"tokenId={entry.Key} owner={entry.Value}");
        }
        Debug.Log(logBuilder.ToString());
    }
}
