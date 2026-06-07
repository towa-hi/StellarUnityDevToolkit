using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AssetCard : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public Image image;
    public Button sendButton;

    public int tokenId;

    public void SetName(string name)
    {
        nameText.text = name;
    }

}
