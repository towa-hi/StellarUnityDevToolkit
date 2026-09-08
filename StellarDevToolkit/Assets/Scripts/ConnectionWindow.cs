using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StellarSDK;
using Stellar;
using StellarWallet;

public class ConnectionWindow : MonoBehaviour
{
    public Button networkToggleButton;
    public TextMeshProUGUI networkToggleButtonText;
    public TMP_InputField serverUriInputField;
    public TMP_InputField contractAddressInputField;
    public TMP_InputField privateKeyInputField;
    public Button newTestnetAccountButton;
    public Button privateKeyButton;
    public Button freighterWalletButton;

    public GameObject newTestnetAccountPanel;
    public GameObject privateKeyPanel;
    public TextMeshProUGUI publicKeyText;

    public GameObject freighterWalletPanel;

    public Button connectButton;

    static readonly Color invalidColor = new Color(1f, 0.4f, 0.4f);
    static readonly Color validColor = Color.white;

    bool isTestnet = true;
    bool walletFound;
    ConnectionMethod currentMethod = ConnectionMethod.NewTestnetAccount;

    enum ConnectionMethod
    {
        NewTestnetAccount,
        PrivateKey,
        FreighterWallet
    }

    void Start()
    {
        networkToggleButton.onClick.AddListener(ToggleNetwork);
        newTestnetAccountButton.onClick.AddListener(() => ToggleConnectionMethod(ConnectionMethod.NewTestnetAccount));
        privateKeyButton.onClick.AddListener(() => ToggleConnectionMethod(ConnectionMethod.PrivateKey));
        freighterWalletButton.onClick.AddListener(() => ToggleConnectionMethod(ConnectionMethod.FreighterWallet));
        serverUriInputField.onValueChanged.AddListener(_ => UpdateValidity());
        contractAddressInputField.onValueChanged.AddListener(_ => UpdateValidity());
        if (privateKeyInputField != null)
        {
            privateKeyInputField.onValueChanged.AddListener(OnPrivateKeyChanged);
        }

        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectClicked);
        }

        Init(null);
    }

    public void Init(NetworkContext? networkContext)
    {
        walletFound = false;

        if (networkContext is NetworkContext context)
        {
            ApplyNetwork(context.isTestnet, resetUriToDefault: false);
            SetInputText(serverUriInputField, context.serverUri);
            SetInputText(contractAddressInputField, context.contractAddress);

            if (!isTestnet || context.signingMethod == NetworkContext.SigningMethod.UnityWallet)
            {
                SetInputText(privateKeyInputField, string.Empty);
                SetPrivateKey(string.Empty);
                ToggleConnectionMethod(ConnectionMethod.FreighterWallet);
                return;
            }

            ToggleConnectionMethod(ConnectionMethod.PrivateKey);
            string restoredSeed = GetSecretSeed(context);
            ApplyPrivateKey(string.IsNullOrEmpty(restoredSeed) ? GetDefaultAccountSecretSeed() : restoredSeed);
            return;
        }

        ApplyNetwork(true, resetUriToDefault: true);
        SetInputText(contractAddressInputField, GetDefaultContractAddress());
        ApplyPrivateKey(GetDefaultAccountSecretSeed());
        ToggleConnectionMethod(ConnectionMethod.NewTestnetAccount);
    }

    void ToggleNetwork()
    {
        ApplyNetwork(!isTestnet, resetUriToDefault: true);
        if (!isTestnet)
        {
            ToggleConnectionMethod(ConnectionMethod.FreighterWallet);
            return;
        }

        UpdateConnectionMethodButtons();
        UpdateValidity();
    }

    void ApplyNetwork(bool testnet, bool resetUriToDefault)
    {
        isTestnet = testnet;
        TextMeshProUGUI label = GetNetworkToggleLabel();
        if (label != null)
        {
            label.text = isTestnet ? "TESTNET" : "MAINNET";
        }

        if (resetUriToDefault)
        {
            SetInputText(serverUriInputField, GetDefaultServerUri());
        }
    }

    TextMeshProUGUI GetNetworkToggleLabel()
    {
        if (networkToggleButtonText != null)
        {
            return networkToggleButtonText;
        }

        if (networkToggleButton != null)
        {
            networkToggleButtonText = networkToggleButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        return networkToggleButtonText;
    }

    void OnPrivateKeyChanged(string privateKeyInput)
    {
        SetPrivateKey(privateKeyInput);
        UpdateValidity();
    }

    void ApplyPrivateKey(string privateKeyInput)
    {
        SetInputText(privateKeyInputField, privateKeyInput);
        SetPrivateKey(privateKeyInput);
        UpdateValidity();
    }

    void SetPrivateKey(string privateKeyInput)
    {
        if (publicKeyText == null)
        {
            return;
        }

        bool valid = Stellar.Utilities.StrKey.IsValidEd25519SecretSeed(privateKeyInput);
        publicKeyText.text = valid
            ? "Your public Key: " + MuxedAccount.FromSecretSeed(privateKeyInput).AccountId
            : "INVALID PRIVATE KEY";
    }

    void ToggleConnectionMethod(ConnectionMethod method)
    {
        currentMethod = method;
        switch (method)
        {
            case ConnectionMethod.NewTestnetAccount:
                newTestnetAccountPanel.SetActive(true);
                privateKeyPanel.SetActive(false);
                freighterWalletPanel.SetActive(false);
                UpdateConnectionMethodButtons();
                UpdateValidity();
                break;
            case ConnectionMethod.PrivateKey:
                newTestnetAccountPanel.SetActive(false);
                privateKeyPanel.SetActive(true);
                freighterWalletPanel.SetActive(false);
                UpdateConnectionMethodButtons();
                string seed = privateKeyInputField != null ? privateKeyInputField.text : string.Empty;
                if (string.IsNullOrEmpty(seed))
                {
                    seed = GetDefaultAccountSecretSeed();
                }
                ApplyPrivateKey(seed);
                break;
            case ConnectionMethod.FreighterWallet:
                newTestnetAccountPanel.SetActive(false);
                privateKeyPanel.SetActive(false);
                freighterWalletPanel.SetActive(true);
                UpdateConnectionMethodButtons();
                walletFound = false;
                UpdateValidity();
                _ = CheckFreighterAsync();
                break;
            default:
                break;
        }
    }

    void UpdateConnectionMethodButtons()
    {
        bool allowKeyMethods = isTestnet;
        newTestnetAccountButton.interactable = allowKeyMethods && currentMethod != ConnectionMethod.NewTestnetAccount;
        privateKeyButton.interactable = allowKeyMethods && currentMethod != ConnectionMethod.PrivateKey;
        freighterWalletButton.interactable = currentMethod != ConnectionMethod.FreighterWallet;
    }

    void OnConnectClicked()
    {
        switch (currentMethod)
        {
            case ConnectionMethod.NewTestnetAccount:
                _ = ConnectNewTestnetAccountAsync();
                break;
            case ConnectionMethod.PrivateKey:
                ConnectPrivateKey();
                break;
            case ConnectionMethod.FreighterWallet:
                _ = ConnectFreighterAsync();
                break;
        }
    }

    async Task ConnectNewTestnetAccountAsync()
    {
        SetConnectBusy(true);

        try
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("ConnectNewTestnetAccount: GameManager.Instance is missing.");
                return;
            }

            Result<MuxedAccount> result = await StellarClient.CreateAccount(GameManager.Instance.ClientTask);
            if (result.IsError)
            {
                Debug.LogError($"ConnectNewTestnetAccount: error: {result.Message}");
                return;
            }

            ApplyConnectedContext(BuildNetworkContext(
                NetworkContext.SigningMethod.PrivateKey,
                result.Value,
                GetServerUri()));
        }
        catch (Exception exception)
        {
            Debug.LogError($"ConnectNewTestnetAccount failed: {exception.Message}");
        }
        finally
        {
            UpdateValidity();
        }
    }

    void ConnectPrivateKey()
    {
        string seed = privateKeyInputField != null ? privateKeyInputField.text : string.Empty;
        if (!Stellar.Utilities.StrKey.IsValidEd25519SecretSeed(seed))
        {
            return;
        }

        ApplyConnectedContext(BuildNetworkContext(
            NetworkContext.SigningMethod.PrivateKey,
            MuxedAccount.FromSecretSeed(seed),
            GetServerUri()));
    }

    async Task ConnectFreighterAsync()
    {
        SetConnectBusy(true);

        try
        {
            WalletResult<WalletManager.WalletConnection> result = await WalletManager.ConnectWallet(isTestnet);
            if (result.IsError)
            {
                Debug.LogError($"ConnectFreighter: error: {result.Message}");
                return;
            }

            string serverUri = GetServerUri();
            if (!string.IsNullOrWhiteSpace(result.Value.networkDetails?.sorobanRpcUrl))
            {
                serverUri = result.Value.networkDetails.sorobanRpcUrl;
            }

            ApplyConnectedContext(BuildNetworkContext(
                NetworkContext.SigningMethod.UnityWallet,
                MuxedAccount.FromAccountId(result.Value.address),
                serverUri,
                GameManager.SignWithUnityWallet));
        }
        catch (Exception exception)
        {
            Debug.LogError($"ConnectFreighter failed: {exception.Message}");
        }
        finally
        {
            UpdateValidity();
        }
    }

    NetworkContext BuildNetworkContext(
        NetworkContext.SigningMethod signingMethod,
        MuxedAccount account,
        string serverUri,
        Func<string, string, Task<Result<string>>> signer = null)
    {
        DefaultSettings settings = GetDefaultSettings();
        return new NetworkContext(
            true,
            signingMethod,
            account,
            isTestnet,
            serverUri,
            contractAddressInputField != null ? contractAddressInputField.text : string.Empty,
            isTestnet ? settings?.testnetAssetIssuerAddress : settings?.mainnetAssetIssuerAddress,
            isTestnet ? settings?.testnetAssetCode : settings?.mainnetAssetCode,
            1000,
            30,
            signer);
    }

    static void ApplyConnectedContext(NetworkContext networkContext)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("ConnectionWindow: GameManager.Instance is missing.");
            return;
        }

        GameManager.Instance.SetNetworkContext(networkContext);
        GameManager.Instance.ShowMainWindow();
    }

    void SetConnectBusy(bool busy)
    {
        if (connectButton != null && busy)
        {
            connectButton.interactable = false;
        }
    }

    string GetServerUri()
    {
        return serverUriInputField != null ? serverUriInputField.text : string.Empty;
    }

    async Task CheckFreighterAsync()
    {
        walletFound = await WalletManager.CanFindWalletAsync();
        if (!walletFound)
        {
            Debug.LogWarning("Freighter wallet not available");
        }

        UpdateValidity();
    }

    void UpdateValidity()
    {
        bool serverUriValid = Uri.TryCreate(serverUriInputField.text, UriKind.Absolute, out Uri uri)
            && (uri.Scheme == "https" || uri.Scheme == "http");
        bool contractAddressValid = Stellar.Utilities.StrKey.IsValidContractId(contractAddressInputField.text);
        bool privateKeyValid = privateKeyInputField != null
            && Stellar.Utilities.StrKey.IsValidEd25519SecretSeed(privateKeyInputField.text);

        SetFieldValid(serverUriInputField, serverUriValid);
        SetFieldValid(contractAddressInputField, contractAddressValid);
        if (currentMethod == ConnectionMethod.PrivateKey)
        {
            SetFieldValid(privateKeyInputField, privateKeyValid);
        }

        bool methodValid = currentMethod switch
        {
            ConnectionMethod.NewTestnetAccount => true,
            ConnectionMethod.PrivateKey => privateKeyValid,
            ConnectionMethod.FreighterWallet => true,
            _ => false,
        };

        if (connectButton != null)
        {
            connectButton.interactable = serverUriValid && contractAddressValid && methodValid;
        }
    }

    static void SetFieldValid(TMP_InputField field, bool valid)
    {
        if (field == null || field.image == null)
        {
            return;
        }

        field.image.color = valid ? validColor : invalidColor;
    }

    static void SetInputText(TMP_InputField field, string value)
    {
        if (field == null)
        {
            return;
        }

        string text = value ?? string.Empty;
        field.text = text;
        field.SetTextWithoutNotify(text);
        if (field.textComponent != null)
        {
            field.textComponent.text = text;
        }
    }

    static string GetSecretSeed(NetworkContext context)
    {
        return context.userAccount != null ? context.userAccount.SecretSeed ?? string.Empty : string.Empty;
    }

    string GetDefaultServerUri()
    {
        DefaultSettings settings = GetDefaultSettings();
        if (settings == null)
        {
            return string.Empty;
        }

        return isTestnet ? settings.testnetUri ?? string.Empty : settings.mainnetUri ?? string.Empty;
    }

    static string GetDefaultContractAddress()
    {
        DefaultSettings settings = GetDefaultSettings();
        return settings != null ? settings.contractAddress ?? string.Empty : string.Empty;
    }

    static string GetDefaultAccountSecretSeed()
    {
        DefaultSettings settings = GetDefaultSettings();
        return settings != null ? settings.accountSecretSeed ?? string.Empty : string.Empty;
    }

    static DefaultSettings GetDefaultSettings()
    {
        if (GameManager.Instance != null && GameManager.Instance.defaultSettings != null)
        {
            return GameManager.Instance.defaultSettings;
        }

        return Resources.Load<DefaultSettings>("DefaultSettings");
    }
}
