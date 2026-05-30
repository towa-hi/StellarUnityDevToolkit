using UnityEngine;
using Stellar;
using StellarSDK;
using StellarWallet;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    NetworkContext context;

    public NetworkContextWindow networkContextWindow;

    public static GameManager Instance { get; private set; }

    public DefaultSettings defaultSettings;
    public CommunicationDiagram communicationDiagram;

    StellarClientTask clientTask;

    public NetworkUI networkUI;
    public Board board;
    public BlockBlastGameController blockBlastGameController;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void OnEnable()
    {
        clientTask = new StellarClientTask();
        clientTask.OnStepStarted += step => Debug.Log($"[StellarClient] Started: {step}");
        clientTask.OnStepEnded += step => Debug.Log($"[StellarClient] Ended: {step}");
        clientTask.OnBusyChanged += busy => Debug.Log($"[StellarClient] Busy: {busy}");
        if (communicationDiagram != null)
            communicationDiagram.SetTask(clientTask);
    }

    async void Start()
    {
        Network.UseTestNetwork();
        MuxedAccount account = MuxedAccount.FromSecretSeed(defaultSettings.accountSecretSeed);
        context = new NetworkContext(
            true, NetworkContext.SigningMethod.PrivateKey, account, true,
            defaultSettings.testnetUri,
            defaultSettings.contractAddress,
            defaultSettings.testnetAssetIssuerAddress,
            defaultSettings.testnetAssetCode,
            1000,
            30);
        SetNetworkContext(context);
    }

    public async void RunTests()
    {
        RpcTests.Results results = await RpcTests.RunAllAsync(context, clientTask);
    }

    public async void CreateTestnetAccount()
    {
        Result<MuxedAccount> result = await StellarClient.CreateAccount(clientTask);
        if (result.IsOk)
        {
            context.userAccount = result.Value;
            context.signingMethod = NetworkContext.SigningMethod.PrivateKey;
            context.unityWalletSigner = null;
            SetNetworkContext(context);
        }
        else
        {
            Debug.LogError($"CreateTestnetAccount: error: {result.Message}");
        }
    }

    public async void ConnectWallet()
    {
        WalletResult<WalletManager.WalletConnection> result = await WalletManager.ConnectWallet(context.isTestnet);
        if (result.IsError)
        {
            Debug.LogError($"ConnectWallet: error: {result.Message}");
            return;
        }

        context.signingMethod = NetworkContext.SigningMethod.UnityWallet;
        context.unityWalletSigner = SignWithUnityWallet;
        context.userAccount = MuxedAccount.FromAccountId(result.Value.address);
        if (!string.IsNullOrWhiteSpace(result.Value.networkDetails?.sorobanRpcUrl))
        {
            context.serverUri = result.Value.networkDetails.sorobanRpcUrl;
        }

        SetNetworkContext(context);
    }

    public void SetNetworkContext(NetworkContext networkContext)
    {
        context = networkContext;
        networkContextWindow.PopulateNetworkContext(context);
    }

    static async System.Threading.Tasks.Task<Result<string>> SignWithUnityWallet(string unsignedEnvelope, string networkPassphrase)
    {
        WalletResult<string> result = await WalletManager.SignTransaction(unsignedEnvelope, networkPassphrase);
        if (result.IsOk)
        {
            return Result<string>.Ok(result.Value);
        }

        return Result<string>.Err(MapWalletStatusCode(result.Code), result.Message);
    }

    static StatusCode MapWalletStatusCode(WalletStatusCode code)
    {
        return code switch
        {
            WalletStatusCode.WalletNotAvailable => StatusCode.WALLET_NOT_AVAILABLE,
            WalletStatusCode.WalletAddressMissing => StatusCode.WALLET_ADDRESS_MISSING,
            WalletStatusCode.WalletNetworkDetailsError => StatusCode.WALLET_NETWORK_DETAILS_ERROR,
            WalletStatusCode.WalletParsingError => StatusCode.WALLET_PARSING_ERROR,
            WalletStatusCode.WalletSigningCancelled => StatusCode.WALLET_SIGNING_CANCELLED,
            WalletStatusCode.WalletSigningError => StatusCode.WALLET_SIGNING_ERROR,
            _ => StatusCode.WALLET_ERROR,
        };
    }

    public void StartGame()
    {
        networkUI.gameObject.SetActive(false);
        NewGame();
    }

    void NewGame()
    {
        Debug.Log("Starting new game");
        if (blockBlastGameController != null)
        {
            blockBlastGameController.StartNewGame();
            return;
        }

        // Fallback keeps older scene setups functional.
        board.InitializeBoard(new Vector2Int(BlockBlastConstants.BoardSize, BlockBlastConstants.BoardSize));
    }
}
