using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AssetModal : MonoBehaviour
{
    public GameObject root;
    public GameObject assetCardPrefab;
    public List<AssetCard> assetCards = new List<AssetCard>();
    public GameObject window;
    public Button closeButton;

    public bool isOpen;

    string assetContractAddress;

    void Awake()
    {
        assetCards = new List<AssetCard>();
        SetOpen(false);
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    public void SetOpen(bool open)
    {
        isOpen = open;
        if (window != null)
        {
            window.SetActive(open);
        }
        if (!open)
        {
            GameManager.Instance?.CloseSendModal();
        }
    }

    public void SetAssets(Dictionary<int, string> assetMap, string ownerId, string contractAddress)
    {
        assetContractAddress = contractAddress;
        foreach (AssetCard assetCard in assetCards)
        {
            Destroy(assetCard.gameObject);
        }
        assetCards.Clear();

        if (assetCardPrefab == null || root == null)
        {
            Debug.LogError("AssetModal: assetCardPrefab or root is not assigned.");
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
                assetCard.sendButton.onClick.AddListener(() => OpenSendModal(assetCard));
            }
            else
            {
                Debug.LogError("AssetModal: AssetCard prefab is missing a sendButton reference.");
            }
            assetCards.Add(assetCard);
        }
    }

    void Close()
    {
        SetOpen(false);
    }

    void OpenSendModal(AssetCard assetCard)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("AssetModal: GameManager.Instance is missing.");
            return;
        }

        GameManager.Instance.OpenSendModal(assetCard, assetContractAddress);
    }
}
