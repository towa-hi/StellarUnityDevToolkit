using System;
using System.Threading.Tasks;
using Stellar;

namespace StellarSDK
{
    [System.Serializable]
    public struct NetworkContext
    {
        public enum SigningMethod
        {
            PrivateKey,
            UnityWallet,
        }

        public bool online;
        public SigningMethod signingMethod;
        public MuxedAccount userAccount;
        public bool isTestnet;
        public string contractAddress;

        public string serverUri;
        public string assetIssuerAddress;
        public string assetCode;

        public int pollRateMs;
        public int maxAttempts;

        [NonSerialized]
        public Func<string, string, Task<Result<string>>> unityWalletSigner;

        public NetworkContext(bool inOnline, SigningMethod inSigningMethod, MuxedAccount inUserAccount, bool inIsTestnet, string inServerUri, string inContractAddress, string inAssetIssuerAddress, string inAssetCode, int inPollRateMs, int inMaxAttempts, Func<string, string, Task<Result<string>>> inUnityWalletSigner = null)
        {
            online = inOnline;
            signingMethod = inSigningMethod;
            userAccount = inUserAccount;
            isTestnet = inIsTestnet;
            serverUri = inServerUri;
            contractAddress = inContractAddress;
            assetIssuerAddress = inAssetIssuerAddress;
            assetCode = inAssetCode;
            pollRateMs = inPollRateMs;
            maxAttempts = inMaxAttempts;
            unityWalletSigner = inUnityWalletSigner;
        }
    }
}
