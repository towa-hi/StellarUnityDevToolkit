using Stellar;

namespace StellarSDK
{
    [System.Serializable]
    public struct NetworkContext
    {
        public bool online;
        public bool isWallet;
        public MuxedAccount userAccount;
        public bool isTestnet;
        public string contractAddress;

        public string serverUri;
        public string assetIssuerAddress;
        public string assetCode;

        public int pollRateMs;
        public int maxAttempts;

        public NetworkContext(bool inOnline, bool inIsWallet, MuxedAccount inUserAccount, bool inIsTestnet, string inServerUri, string inContractAddress, string inAssetIssuerAddress, string inAssetCode, int inPollRateMs, int inMaxAttempts)
        {
            online = inOnline;
            isWallet = inIsWallet;
            userAccount = inUserAccount;
            isTestnet = inIsTestnet;
            serverUri = inServerUri;
            contractAddress = inContractAddress;
            assetIssuerAddress = inAssetIssuerAddress;
            assetCode = inAssetCode;
            pollRateMs = inPollRateMs;
            maxAttempts = inMaxAttempts;
        }
    }
}
