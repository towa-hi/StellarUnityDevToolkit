using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TransactionTrackerEntry : MonoBehaviour
{
    public TextMeshProUGUI transactionTimeText;
    public TextMeshProUGUI contractText;
    public TextMeshProUGUI transactionText;

    public Button openContractButton;
    public Button openLinkButton;

    string contractUrl;
    string transactionUrl;

    void Awake()
    {
        openContractButton.onClick.AddListener(() => LinkOpener.Open(contractUrl));
        openLinkButton.onClick.AddListener(() => LinkOpener.Open(transactionUrl));
    }

    public void SetData(string time, string operationLabel, string contractAddress, string contractUrl, string transactionUrl, uint estimatedFeeStroops, string hash)
    {
        this.contractUrl = contractUrl;
        this.transactionUrl = transactionUrl;

        transactionTimeText.text = time;
        transactionText.text = FormatTransactionText(operationLabel, estimatedFeeStroops, hash);
        contractText.text = string.IsNullOrEmpty(contractAddress)
            ? string.Empty
            : $"Contract: {ShortenAddress(contractAddress)}";

        // Not every transaction targets a contract (e.g. classic payments).
        openContractButton.gameObject.SetActive(contractUrl != null);
    }

    static string FormatTransactionText(string operationLabel, uint estimatedFeeStroops, string hash)
    {
        return $"Function: {operationLabel} Estimated Fee: {estimatedFeeStroops} stroops Hash: {ShortenHash(hash)}";
    }

    static string ShortenAddress(string address)
    {
        if (string.IsNullOrEmpty(address))
        {
            return string.Empty;
        }

        return address.Length <= 12 ? address : $"{address[..4]}…{address[^4..]}";
    }

    static string ShortenHash(string hash)
    {
        if (string.IsNullOrEmpty(hash))
        {
            return string.Empty;
        }

        return hash.Length <= 12 ? hash : $"{hash[..4]}…{hash[^4..]}";
    }
}
