using System;
using UnityEngine;
using UnityEngine.UI;

public class TransactionTracker : MonoBehaviour
{
    public GameObject transactionTrackerEntryPrefab;
    public Transform Content;
    public ScrollRect scrollRect;

    public Button testButton;

    void Start()
    {
        testButton.onClick.AddListener(AddTestEntry);
    }

    void AddTestEntry()
    {
        AddEntry(
            DateTime.Now.ToString("HH:mm:ss"),
            "mint",
            "CABPWPXDLIHKI6WIHDH4H45RTSZOMFIWOCG666ANQYELUBYP5OUG2G5U",
            "https://stellar.expert/explorer/testnet/contract/CABPWPXDLIHKI6WIHDH4H45RTSZOMFIWOCG666ANQYELUBYP5OUG2G5U",
            "https://stellar.expert/explorer/testnet/tx/b699dbd3da162d05985853e3273b9a86d793df14cbadcda279b86c4fc77b230b",
            35000,
            "b699dbd3da162d05985853e3273b9a86d793df14cbadcda279b86c4fc77b230b");
    }

    public void AddEntry(string time, string operationLabel, string contractAddress, string contractUrl, string transactionUrl, uint estimatedFeeStroops, string hash)
    {
        GameObject go = Instantiate(transactionTrackerEntryPrefab, Content);
        go.GetComponent<TransactionTrackerEntry>().SetData(time, operationLabel, contractAddress, contractUrl, transactionUrl, estimatedFeeStroops, hash);

        ScrollToBottom();
    }

    void ScrollToBottom()
    {
        // Force the layout to account for the new entry before scrolling,
        // otherwise we'd scroll against the old content height.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Content);

        scrollRect.verticalNormalizedPosition = 0f;
    }
}
