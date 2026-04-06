using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StellarSDK;
using Stellar;
using System;

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
        bool valid = Stellar.Utilities.StrKey.IsValidEd25519SecretSeed(privateKeyInputField.text);
        publicAddressInputField.text = valid ? MuxedAccount.FromSecretSeed(privateKeyInputField.text).AccountId : "INVALID PRIVATE KEY";
        UpdateSaveButton();
    }

    public void PopulateNetworkContext(NetworkContext networkContext)
    {
        Debug.Log("Populating network context: " + networkContext.serverUri);
        serverUriInputField.text = networkContext.serverUri;
        privateKeyInputField.text = networkContext.userAccount.SecretSeed;
        publicAddressInputField.text = networkContext.userAccount.AccountId;
        contractAddressInputField.text = networkContext.contractAddress;
        assetIssuerAddressInputField.text = networkContext.assetIssuerAddress;
        assetCodeInputField.text = networkContext.assetCode;
        assetsHeldText.text = "0";

        originalServerUri = serverUriInputField.text;
        originalPrivateKey = privateKeyInputField.text;
        originalContractAddress = contractAddressInputField.text;
        originalAssetIssuerAddress = assetIssuerAddressInputField.text;
        originalAssetCode = assetCodeInputField.text;

        OnPrivateKeyChanged();
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
        bool privateKeyValid = Stellar.Utilities.StrKey.IsValidEd25519SecretSeed(privateKeyInputField.text);
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
        NetworkContext newNetworkContext = new NetworkContext(
            true, false, MuxedAccount.FromSecretSeed(privateKeyInputField.text), true,
            serverUriInputField.text,
            contractAddressInputField.text,
            assetIssuerAddressInputField.text,
            assetCodeInputField.text,
            1000,
            30);
        GameManager.Instance.SetNetworkContext(newNetworkContext);
    }

}
