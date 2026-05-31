using UnityEngine;

[CreateAssetMenu(fileName = "DefaultSettings", menuName = "Default Settings")]
public class DefaultSettings : ScriptableObject
{
    public string accountSecretSeed;
    public string contractAddress;
    public string testnetUri;
    public string mainnetUri;
    public string testnetAssetIssuerAddress;
    public string mainnetAssetIssuerAddress;
    public string testnetAssetCode;
    public string mainnetAssetCode;
    public string sep50AssetContractAddress;

}
