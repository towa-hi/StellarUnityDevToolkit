using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StellarSDK;
using Stellar;
using System;
using System.Threading.Tasks;

public class NetworkContextWindow : MonoBehaviour
{
    public Button networkToggleButton;
    public TMP_InputField serverUriInputField;
    public TMP_InputField privateKeyInputField;
    public TMP_InputField publicAddressInputField;
    public TMP_InputField contractAddressInputField;
    public TMP_InputField assetIssuerAddressInputField;
    public TMP_InputField assetCodeInputField;
    public TextMeshProUGUI assetsHeldText;
    public Button saveButton;
    public TextMeshProUGUI saveButtonText;

    string originalServerUri;
    string originalPrivateKey;
    string originalContractAddress;
    string originalAssetIssuerAddress;
    string originalAssetCode;

    NetworkContext currentContext;
    bool walletConnected;

    static readonly Color invalidColor = new Color(1f, 0.4f, 0.4f);
    static readonly Color validColor = Color.white;

    void Start()
    {
        saveButton.onClick.AddListener(SaveNetworkContext);
        serverUriInputField.onValueChanged.AddListener(_ => UpdateSaveButton());
        privateKeyInputField.onValueChanged.AddListener(_ => OnPrivateKeyChanged());
        contractAddressInputField.onValueChanged.AddListener(_ => UpdateSaveButton());
        assetIssuerAddressInputField.onValueChanged.AddListener(_ => UpdateSaveButton());
        assetCodeInputField.onValueChanged.AddListener(_ => UpdateSaveButton());
    }

    void OnPrivateKeyChanged()
    {
        if (walletConnected)
        {
            UpdateSaveButton();
            return;
        }

        bool valid = Stellar.Utilities.StrKey.IsValidEd25519SecretSeed(privateKeyInputField.text);
        publicAddressInputField.text = valid ? MuxedAccount.FromSecretSeed(privateKeyInputField.text).AccountId : "INVALID PRIVATE KEY";
        UpdateSaveButton();
    }

    public void PopulateNetworkContext(NetworkContext networkContext)
    {
        Debug.Log("Populating network context: " + networkContext.serverUri);
        currentContext = networkContext;
        walletConnected = networkContext.signingMethod == NetworkContext.SigningMethod.UnityWallet;

        serverUriInputField.SetTextWithoutNotify(networkContext.serverUri);

        if (walletConnected)
        {
            privateKeyInputField.SetTextWithoutNotify(string.Empty);
            privateKeyInputField.interactable = false;
            privateKeyInputField.readOnly = true;
        }
        else
        {
            privateKeyInputField.SetTextWithoutNotify(networkContext.userAccount.SecretSeed ?? string.Empty);
            privateKeyInputField.interactable = true;
            privateKeyInputField.readOnly = false;
        }

        if (privateKeyInputField.placeholder is TMP_Text privateKeyPlaceholder)
        {
            privateKeyPlaceholder.text = walletConnected ? "Managed by connected wallet" : "Enter private key";
        }

        publicAddressInputField.SetTextWithoutNotify(networkContext.userAccount.AccountId);
        contractAddressInputField.SetTextWithoutNotify(networkContext.contractAddress);
        assetIssuerAddressInputField.SetTextWithoutNotify(networkContext.assetIssuerAddress);
        assetCodeInputField.SetTextWithoutNotify(networkContext.assetCode);
        assetsHeldText.text = "0";

        originalServerUri = serverUriInputField.text;
        originalPrivateKey = privateKeyInputField.text;
        originalContractAddress = contractAddressInputField.text;
        originalAssetIssuerAddress = assetIssuerAddressInputField.text;
        originalAssetCode = assetCodeInputField.text;

        UpdateSaveButton();
        saveButton.interactable = false;
    }

    void UpdateSaveButton()
    {
        bool hasChanges =
            serverUriInputField.text != originalServerUri ||
            privateKeyInputField.text != originalPrivateKey ||
            contractAddressInputField.text != originalContractAddress ||
            assetIssuerAddressInputField.text != originalAssetIssuerAddress ||
            assetCodeInputField.text != originalAssetCode;

        saveButton.interactable = hasChanges;

        bool serverUriValid = Uri.TryCreate(serverUriInputField.text, UriKind.Absolute, out Uri uri)
            && (uri.Scheme == "https" || uri.Scheme == "http");
        bool privateKeyValid = walletConnected || Stellar.Utilities.StrKey.IsValidEd25519SecretSeed(privateKeyInputField.text);
        bool contractAddressValid = Stellar.Utilities.StrKey.IsValidContractId(contractAddressInputField.text);
        bool assetIssuerAddressValid = Stellar.Utilities.StrKey.IsValidEd25519PublicKey(assetIssuerAddressInputField.text);
        bool assetCodeValid = !string.IsNullOrEmpty(assetCodeInputField.text) && assetCodeInputField.text.Length <= 12;

        SetFieldValid(serverUriInputField, serverUriValid);
        SetFieldValid(privateKeyInputField, privateKeyValid);
        SetFieldValid(contractAddressInputField, contractAddressValid);
        SetFieldValid(assetIssuerAddressInputField, assetIssuerAddressValid);
        SetFieldValid(assetCodeInputField, assetCodeValid);

        if (!serverUriValid)
            saveButtonText.text = "Invalid RPC Server URI";
        else if (!privateKeyValid)
            saveButtonText.text = "Invalid Private Key";
        else if (!contractAddressValid)
            saveButtonText.text = "Invalid Contract Address";
        else if (!assetIssuerAddressValid)
            saveButtonText.text = "Invalid Asset Issuer Address";
        else if (!assetCodeValid)
            saveButtonText.text = "Invalid Asset Code";
        else
            saveButtonText.text = "Save";
    }

    void SetFieldValid(TMP_InputField field, bool valid)
    {
        field.image.color = valid ? validColor : invalidColor;
    }

    public void SaveNetworkContext()
    {
        NetworkContext.SigningMethod signingMethod;
        MuxedAccount account;
        Func<string, string, Task<Result<string>>> signer;

        if (walletConnected)
        {
            signingMethod = NetworkContext.SigningMethod.UnityWallet;
            account = currentContext.userAccount;
            signer = currentContext.unityWalletSigner;
        }
        else
        {
            signingMethod = NetworkContext.SigningMethod.PrivateKey;
            account = MuxedAccount.FromSecretSeed(privateKeyInputField.text);
            signer = null;
        }

        NetworkContext newNetworkContext = new NetworkContext(
            true, signingMethod, account, true,
            serverUriInputField.text,
            contractAddressInputField.text,
            assetIssuerAddressInputField.text,
            assetCodeInputField.text,
            1000,
            30,
            signer);
        GameManager.Instance.SetNetworkContext(newNetworkContext);
    }

}
