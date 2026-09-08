using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StellarSDK;

public class MarketWindow : MonoBehaviour
{
    public Button backButton;
    public TextMeshProUGUI assetAddressText;
    public TextMeshProUGUI statusText;
    public GameObject root;
    public GameObject assetCardPrefab;

    readonly List<AssetCard> assetCards = new List<AssetCard>();

    void Start()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    void OnBackClicked()
    {
        GameManager.Instance?.ShowMainWindow();
    }

    public async void Refresh()
    {
        ClearCards();
        SetStatus("Loading...");

        GameManager manager = GameManager.Instance;
        if (manager == null)
        {
            SetAssetAddress(string.Empty);
            SetStatus("Error: GameManager is missing.");
            return;
        }

        string assetAddress = manager.GetDefaultSep50AssetContractAddress();
        SetAssetAddress(assetAddress);

        // async void: an escaping exception would leave the window stuck on "Loading...".
        try
        {
            Result<(Dictionary<int, string> ownerMap, string contractAddress)> result =
                await manager.FetchSEP50AssetOwnerMapAsync(assetAddress);
            if (result.IsError)
            {
                SetStatus($"Error: {result.Message}");
                return;
            }

            (Dictionary<int, string> ownerMap, string contractAddress) = result.Value;
            SetAssetAddress(contractAddress);
            PopulateOwnedAssets(ownerMap, manager.GetContextAssetOwnerId());
            SetStatus(assetCards.Count == 0 ? "No owned assets." : string.Empty);
        }
        catch (Exception exception)
        {
            Debug.LogError($"MarketWindow.Refresh failed: {exception.Message}");
            SetStatus($"Error: {exception.Message}");
        }
    }

    void PopulateOwnedAssets(Dictionary<int, string> assetMap, string ownerId)
    {
        if (assetCardPrefab == null || root == null)
        {
            Debug.LogError("MarketWindow: assetCardPrefab or root is not assigned.");
            return;
        }

        foreach (KeyValuePair<int, string> asset in assetMap)
        {
            if (asset.Value != ownerId)
            {
                continue;
            }

            AssetCard assetCard = Instantiate(assetCardPrefab, root.transform).GetComponent<AssetCard>();
            assetCard.tokenId = asset.Key;
            assetCard.SetName("Token ID: " + asset.Key);
            if (assetCard.sendButton != null)
            {
                // The card's name label lives on the send button, so disable the Button
                // rather than the object: the token id stays readable and nothing is clickable.
                assetCard.sendButton.enabled = false;
            }

            assetCards.Add(assetCard);
        }
    }

    void ClearCards()
    {
        foreach (AssetCard assetCard in assetCards)
        {
            if (assetCard != null)
            {
                Destroy(assetCard.gameObject);
            }
        }

        assetCards.Clear();
    }

    void SetAssetAddress(string address)
    {
        if (assetAddressText != null)
        {
            assetAddressText.text = address ?? string.Empty;
        }
    }

    void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message ?? string.Empty;
        }
    }
}
