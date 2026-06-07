using Stellar.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SendModal : MonoBehaviour
{
    public Image image;
    public TextMeshProUGUI nameText;
    public TMP_InputField destinationAddressInputField;
    public Button sendButton;
    public Button closeButton;
    public GameObject window;

    public bool isOpen;

    int tokenId;
    string assetContractAddress;

    void Awake()
    {
        SetOpen(false);
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
        if (destinationAddressInputField != null)
        {
            destinationAddressInputField.onValueChanged.AddListener(OnDestinationAddressChanged);
        }
        UpdateSendButtonState();
    }

    void OnDestroy()
    {
        if (destinationAddressInputField != null)
        {
            destinationAddressInputField.onValueChanged.RemoveListener(OnDestinationAddressChanged);
        }
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (window != null)
        {
            window.SetActive(open);
        }
    }

    public void SetAsset(AssetCard assetCard, string contractAddress)
    {
        tokenId = assetCard.tokenId;
        assetContractAddress = contractAddress;
        if (image != null && assetCard.image != null)
        {
            image.sprite = assetCard.image.sprite;
        }
        if (nameText != null && assetCard.nameText != null)
        {
            nameText.text = assetCard.nameText.text;
        }
        if (destinationAddressInputField != null)
        {
            destinationAddressInputField.text = string.Empty;
        }
        UpdateSendButtonState();
    }

    void OnDestinationAddressChanged(string _)
    {
        UpdateSendButtonState();
    }

    void UpdateSendButtonState()
    {
        if (sendButton == null)
        {
            return;
        }

        string destinationAddress = destinationAddressInputField != null
            ? destinationAddressInputField.text
            : string.Empty;
        bool isValidDestination = !string.IsNullOrWhiteSpace(destinationAddress)
            && StrKey.IsValidEd25519PublicKey(destinationAddress.Trim());
        sendButton.interactable = isValidDestination;
    }

    void OnSendButtonClicked()
    {
        if (destinationAddressInputField == null)
        {
            return;
        }

        string destinationAddress = destinationAddressInputField.text.Trim();
        if (!StrKey.IsValidEd25519PublicKey(destinationAddress))
        {
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("SendModal: GameManager.Instance is missing.");
            return;
        }

        GameManager.Instance.SendAsset(tokenId, destinationAddress, assetContractAddress);
        SetOpen(false);
    }

    void Close()
    {
        SetOpen(false);
    }
}
