using UnityEngine;
using Stellar;
using StellarSDK;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    NetworkContext context;

    public NetworkContextWindow networkContextWindow;

    public static GameManager Instance { get; private set; }

    public DefaultSettings defaultSettings;
    public CommunicationDiagram communicationDiagram;

    StellarClientTask clientTask;

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
            true, false, account, true,
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
            SetNetworkContext(context);
        }
        else
        {
            Debug.LogError($"CreateTestnetAccount: error: {result.Message}");
        }
    }

    public async void ConnectWallet()
    {

    }

    public void SetNetworkContext(NetworkContext networkContext)
    {
        context = networkContext;
        networkContextWindow.PopulateNetworkContext(context);
    }
}
