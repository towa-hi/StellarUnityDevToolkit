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
    public TestWindow testWindow;

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
        testWindow?.PopulateDefaultFields(refreshOwnerFromContext: true);
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

    public void TryGetSEP50AssetBalance(string assetContractAddress, string ownerAddress = null)
    {
        _ = TryGetSEP50AssetBalanceAsync(assetContractAddress, ownerAddress);
    }

    public string GetContextContractId()
    {
        return context.contractAddress ?? string.Empty;
    }

    public string GetDefaultSep50AssetContractAddress()
    {
        if (defaultSettings != null && !string.IsNullOrWhiteSpace(defaultSettings.sep50AssetContractAddress))
        {
            return defaultSettings.sep50AssetContractAddress;
        }

        return GetContextContractId();
    }

    public string GetContextAssetOwnerId()
    {
        return context.userAccount != null ? context.userAccount.AccountId : string.Empty;
    }

    public async Task<Result<SorobanInvocationMeta>> MintSEP50AssetAsync(string assetContractAddress, string ownerAddress = null)
    {
        string normalizedAssetContractAddress = string.IsNullOrWhiteSpace(assetContractAddress) ? null : assetContractAddress.Trim();
        if (normalizedAssetContractAddress == null)
        {
            return Result<SorobanInvocationMeta>.Err(StatusCode.OTHER_ERROR, "MintSEP50Asset: asset contract address is required.");
        }
        string normalizedOwnerOverride = string.IsNullOrWhiteSpace(ownerAddress) ? null : ownerAddress.Trim();
        if (normalizedOwnerOverride != null && !Stellar.Utilities.StrKey.IsValidEd25519PublicKey(normalizedOwnerOverride))
        {
            return Result<SorobanInvocationMeta>.Err(StatusCode.OTHER_ERROR, $"MintSEP50Asset: invalid owner address override: {normalizedOwnerOverride}");
        }
        if (!Stellar.Utilities.StrKey.IsValidContractId(normalizedAssetContractAddress))
        {
            return Result<SorobanInvocationMeta>.Err(StatusCode.OTHER_ERROR, $"MintSEP50Asset: invalid asset contract address: {normalizedAssetContractAddress}");
        }
        NetworkContext overwrittenContext = context;
        overwrittenContext.contractAddress = normalizedAssetContractAddress;
        return await StellarClient.InvokeSEP50AssetMint(overwrittenContext, normalizedOwnerOverride, clientTask);
    }

    public async Task<Result<int>> GetSEP50AssetBalanceAsync(string assetContractAddress, string ownerAddress = null)
    {
        string normalizedAssetContractAddress = string.IsNullOrWhiteSpace(assetContractAddress) ? null : assetContractAddress.Trim();
        if (normalizedAssetContractAddress == null)
        {
            return Result<int>.Err(StatusCode.OTHER_ERROR, "GetSEP50AssetBalance: asset contract address is required.");
        }
        string normalizedOwnerOverride = string.IsNullOrWhiteSpace(ownerAddress) ? null : ownerAddress.Trim();
        if (normalizedOwnerOverride != null && !Stellar.Utilities.StrKey.IsValidEd25519PublicKey(normalizedOwnerOverride))
        {
            return Result<int>.Err(StatusCode.OTHER_ERROR, $"GetSEP50AssetBalance: invalid owner address override: {normalizedOwnerOverride}");
        }
        if (!Stellar.Utilities.StrKey.IsValidContractId(normalizedAssetContractAddress))
        {
            return Result<int>.Err(StatusCode.OTHER_ERROR, $"GetSEP50AssetBalance: invalid asset contract address: {normalizedAssetContractAddress}");
        }
        NetworkContext overwrittenContext = context;
        overwrittenContext.contractAddress = normalizedAssetContractAddress;
        return await StellarClient.SimSEP50AssetBalance(overwrittenContext, normalizedOwnerOverride, clientTask);
    }

    async Task TryGetSEP50AssetBalanceAsync(string assetContractAddress, string ownerAddress)
    {
        try
        {
            string normalizedAssetContractAddress = string.IsNullOrWhiteSpace(assetContractAddress) ? null : assetContractAddress.Trim();
            string normalizedOwnerOverride = string.IsNullOrWhiteSpace(ownerAddress) ? null : ownerAddress.Trim();
            Result<int> result = await GetSEP50AssetBalanceAsync(normalizedAssetContractAddress, normalizedOwnerOverride);

            if (result.IsOk)
            {
                string ownerForLog = normalizedOwnerOverride ?? context.userAccount.AccountId;
                Debug.Log($"GetSEP50AssetBalance: owner={ownerForLog}, contract={normalizedAssetContractAddress}, balance={result.Value}");
            }
            else
            {
                Debug.LogError($"GetSEP50AssetBalance failed: {result.Message}");
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"GetSEP50AssetBalance failed: {exception.Message}");
        }
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
