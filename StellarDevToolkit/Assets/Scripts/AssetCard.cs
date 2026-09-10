using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AssetCard : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Image image;
    public Button sendButton;

    public int tokenId;

    public static string ScoreAssetDisplayName(int tokenId)
    {
        int points = tokenId % 1000;
        if (points == 50 || points == 100 || points == 500)
        {
            return points + " point (#" + tokenId + ")";
        }

        return "Token ID: " + tokenId;
    }

    public void SetName(string name)
    {
        nameText.text = name;
    }

}
