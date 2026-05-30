using UnityEngine;
using System;
using System.Threading.Tasks;
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
    public GameController gameController;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        if (clientTask == null)
        {
            clientTask = new StellarClientTask();
        }

        clientTask.OnStepStarted += HandleClientStepStarted;
        clientTask.OnStepEnded += HandleClientStepEnded;
        clientTask.OnBusyChanged += HandleClientBusyChanged;
        if (communicationDiagram != null)
        {
            communicationDiagram.SetTask(clientTask);
        }
    }

    void OnDisable()
    {
        if (clientTask == null)
        {
            return;
        }

        clientTask.OnStepStarted -= HandleClientStepStarted;
        clientTask.OnStepEnded -= HandleClientStepEnded;
        clientTask.OnBusyChanged -= HandleClientBusyChanged;
    }

    async void Start()
    {
        try
        {
            await InitializeDefaultNetworkContextAsync();
        }
        catch (Exception exception)
        {
            Debug.LogError($"GameManager.Start failed: {exception.Message}");
        }
    }

    void HandleClientStepStarted(string step)
    {
        Debug.Log($"[StellarClient] Started: {step}");
    }

    void HandleClientStepEnded(string step)
    {
        Debug.Log($"[StellarClient] Ended: {step}");
    }

    void HandleClientBusyChanged(bool busy)
    {
        Debug.Log($"[StellarClient] Busy: {busy}");
    }

    async Task InitializeDefaultNetworkContextAsync()
    {
        if (defaultSettings == null)
        {
            Debug.LogError("GameManager: defaultSettings is missing.");
            return;
        }

        await Task.Yield();
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

    public void RunTests()
    {
        _ = RunTestsAsync();
    }

    async Task RunTestsAsync()
    {
        try
        {
            await RpcTests.RunAllAsync(context, clientTask);
        }
        catch (Exception exception)
        {
            Debug.LogError($"RunTests failed: {exception.Message}");
        }
    }

    public void CreateTestnetAccount()
    {
        _ = CreateTestnetAccountAsync();
    }

    async Task CreateTestnetAccountAsync()
    {
        try
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
        catch (Exception exception)
        {
            Debug.LogError($"CreateTestnetAccount failed: {exception.Message}");
        }
    }

    public void ConnectWallet()
    {
        _ = ConnectWalletAsync();
    }

    async Task ConnectWalletAsync()
    {
        try
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
        catch (Exception exception)
        {
            Debug.LogError($"ConnectWallet failed: {exception.Message}");
        }
    }

    public void SetNetworkContext(NetworkContext networkContext)
    {
        context = networkContext;
        if (networkContextWindow != null)
        {
            networkContextWindow.PopulateNetworkContext(context);
        }
    }

    static async Task<Result<string>> SignWithUnityWallet(string unsignedEnvelope, string networkPassphrase)
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
        if (gameController != null)
        {
            gameController.StartNewGame();
            return;
        }

        // Fallback keeps older scene setups functional.
        board.InitializeBoard(GameUtility.GetBoardSize());
    }
}
