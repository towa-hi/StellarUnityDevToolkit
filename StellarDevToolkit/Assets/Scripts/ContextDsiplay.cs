using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StellarSDK;

public class ContextDsiplay : MonoBehaviour
{

    public TextMeshProUGUI displayButtonText;
    public Button displayButton;
    public TextMeshProUGUI messageText;

    public GameObject messagePanel;

    void Start()
    {
        displayButton.onClick.AddListener(ToggleMessagePanel);
        UpdateDisplayButtonText();

        GameManager manager = GameManager.Instance;
        if (manager == null)
        {
            return;
        }

        manager.OnTopLevelWindowChanged += HandleTopLevelWindowChanged;
        manager.OnNetworkContextChanged += HandleNetworkContextChanged;
        HandleTopLevelWindowChanged(manager.CurrentTopLevelWindow);
        HandleNetworkContextChanged(manager.CurrentNetworkContext);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTopLevelWindowChanged -= HandleTopLevelWindowChanged;
            GameManager.Instance.OnNetworkContextChanged -= HandleNetworkContextChanged;
        }
    }

    void HandleNetworkContextChanged(NetworkContext context)
    {
        messageText.text =
            $"online: {context.online.ToString().ToLowerInvariant()}\n" +
            $"signingMethod: {context.signingMethod}\n" +
            $"serverUri: {context.serverUri}\n" +
            $"contractAddress: {Truncate(context.contractAddress, 5)}\n" +
            $"assestIssuerAddress: {Truncate(context.assetIssuerAddress, 5)}";
    }

    static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value ?? string.Empty;
        }

        return value.Substring(0, maxLength);
    }

    void HandleTopLevelWindowChanged(GameManager.TopLevelWindow window)
    {
        gameObject.SetActive(window != GameManager.TopLevelWindow.Connection);
    }

    void ToggleMessagePanel()
    {
        messagePanel.SetActive(!messagePanel.activeSelf);
        UpdateDisplayButtonText();
    }

    void UpdateDisplayButtonText()
    {
        displayButtonText.text = messagePanel.activeSelf ? "CLOSE INFO" : "OPEN INFO";
    }
}
