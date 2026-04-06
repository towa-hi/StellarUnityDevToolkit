using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace StellarSDK
{
    public class WalletManager : MonoBehaviour
    {
        [DllImport("__Internal")]
        static extern void JSCheckWallet();

        [DllImport("__Internal")]
        static extern void JSGetFreighterAddress(string isTestnet);

        [DllImport("__Internal")]
        static extern void JSGetNetworkDetails();

        [DllImport("__Internal")]
        static extern void JSSignTransaction(string unsignedTransactionEnvelope, string networkPassphrase);

        const int JsSignTransactionUserRejectedCode = -9;

        public static string address;
        public static NetworkDetails networkDetails;

        public static bool webGL;

        public static WalletManager instance;

        public static bool IsWalletBusy { get; private set; }
        public static event Action<bool> OnWalletBusyChanged;

        static void SetWalletBusy(bool busy)
        {
            if (IsWalletBusy == busy) return;
            IsWalletBusy = busy;
            try
            {
                OnWalletBusyChanged?.Invoke(IsWalletBusy);
            }
            catch (Exception e)
            {
                Debug.LogError($"OnWalletBusyChanged handler threw: {e}");
            }
        }

        static TaskCompletionSource<JSResponse> checkWalletTaskSource;
        static TaskCompletionSource<JSResponse> getAddressTaskSource;
        static TaskCompletionSource<JSResponse> getNetworkDetailsTaskSource;
        static TaskCompletionSource<JSResponse> signTransactionTaskSource;

        static JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
        {
            ContractResolver = (IContractResolver)new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        };

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
#if UNITY_WEBGL
            webGL = true;
#endif
        }

        public struct WalletConnection
        {
            public string address;
            public NetworkDetails networkDetails;
        }

        public static async Task<Result<WalletConnection>> ConnectWallet(bool isTestnet)
        {
            SetWalletBusy(true);
            Result<bool> check = await CheckWallet();
            address = null;
            networkDetails = null;
            if (check.IsError)
            {
                Debug.LogWarning("Wallet could not be found");
                SetWalletBusy(false);
                return Result<WalletConnection>.Err(check);
            }
            Result<string> addrRes = await GetAddress(isTestnet);
            if (addrRes.IsError)
            {
                Debug.LogWarning("Address not found");
                SetWalletBusy(false);
                return Result<WalletConnection>.Err(addrRes);
            }
            address = addrRes.Value;
            Result<string> ndRes = await GetNetworkDetails();
            if (ndRes.IsError)
            {
                Debug.LogWarning("Network details not found");
                SetWalletBusy(false);
                return Result<WalletConnection>.Err(ndRes);
            }
            string networkDetailsJson = ndRes.Value;
            try
            {
                var errorObj = JsonConvert.DeserializeAnonymousType(networkDetailsJson, new { error = "" }, jsonSettings);
                if (!string.IsNullOrEmpty(errorObj?.error))
                {
                    Debug.LogError($"Network details error: {errorObj.error}");
                    return Result<WalletConnection>.Err(StatusCode.WALLET_NETWORK_DETAILS_ERROR, errorObj.error);
                }
                NetworkDetails networkDetailsObj = JsonConvert.DeserializeObject<NetworkDetails>(networkDetailsJson, jsonSettings);
                if (networkDetailsObj == null)
                {
                    Debug.LogError("Invalid network details format");
                    return Result<WalletConnection>.Err(StatusCode.WALLET_PARSING_ERROR, "Invalid wallet network details format");
                }
                Debug.Log($"Connected to network: {networkDetailsObj.network}");
                networkDetails = networkDetailsObj;
                var ok = Result<WalletConnection>.Ok(new WalletConnection { address = address, networkDetails = networkDetailsObj });
                SetWalletBusy(false);
                return ok;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to parse network details: {ex.Message}");
                SetWalletBusy(false);
                return Result<WalletConnection>.Err(StatusCode.WALLET_PARSING_ERROR, ex.Message);
            }
        }

        public static Task<bool> CanFindWalletAsync()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return CheckWalletAvailabilityInternal();
#else
            return Task.FromResult(false);
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        static async Task<bool> CheckWalletAvailabilityInternal()
        {
            try
            {
                Result<bool> check = await CheckWallet();
                return check.IsOk;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"WalletManager.CanFindWalletAsync failed: {ex}");
                return false;
            }
        }
#endif

        public static void DisconnectWallet()
        {
            address = null;
            networkDetails = null;
        }

        static async Task<Result<bool>> CheckWallet()
        {
            if (checkWalletTaskSource != null && !checkWalletTaskSource.Task.IsCompleted)
            {
                throw new Exception("CheckWallet() is already in progress");
            }
            checkWalletTaskSource = new TaskCompletionSource<JSResponse>();
            JSCheckWallet();
            JSResponse checkWalletRes = await checkWalletTaskSource.Task;
            checkWalletTaskSource = null;
            if (checkWalletRes.code != 1)
            {
                Debug.Log("CheckWallet() failed with code " + checkWalletRes.code);
                return Result<bool>.Err(StatusCode.WALLET_NOT_AVAILABLE, "Wallet not available");
            }
            Debug.Log("CheckWallet() completed");
            return Result<bool>.Ok(true);
        }

        static async Task<Result<string>> GetAddress(bool isTestnet)
        {
            if (getAddressTaskSource != null && !getAddressTaskSource.Task.IsCompleted)
            {
                throw new Exception("GetAddressFromFreighter() is already in progress");
            }
            getAddressTaskSource = new TaskCompletionSource<JSResponse>();
            JSGetFreighterAddress(isTestnet ? "true" : "false");
            JSResponse getAddressRes = await getAddressTaskSource.Task;
            getAddressTaskSource = null;
            if (getAddressRes.code == -2)
            {
                Debug.Log("GetAddress() failed with code " + getAddressRes.code + " data " + getAddressRes.data);
                return Result<string>.Err(StatusCode.WALLET_ADDRESS_MISSING, getAddressRes.data);
            }
            else if (getAddressRes.code != 1)
            {
                Debug.Log("GetAddress() failed with code " + getAddressRes.code + " data " + getAddressRes.data);
                return Result<string>.Err(StatusCode.WALLET_ADDRESS_MISSING, getAddressRes.data);
            }
            Debug.Log($"GetAddress() completed with data {getAddressRes.data}");
            return Result<string>.Ok(getAddressRes.data);
        }

        static async Task<Result<string>> GetNetworkDetails()
        {
            if (getNetworkDetailsTaskSource != null && !getNetworkDetailsTaskSource.Task.IsCompleted)
            {
                throw new Exception("GetNetworkDetails() is already in progress");
            }
            getNetworkDetailsTaskSource = new TaskCompletionSource<JSResponse>();
            JSGetNetworkDetails();
            JSResponse getNetworkDetailsRes = await getNetworkDetailsTaskSource.Task;
            getNetworkDetailsTaskSource = null;
            if (getNetworkDetailsRes.code != 1)
            {
                Debug.Log("GetNetworkDetails() failed with code " + getNetworkDetailsRes.code);
                return Result<string>.Err(StatusCode.WALLET_NETWORK_DETAILS_ERROR, "Wallet network details error");
            }
            Debug.Log($"GetNetworkDetails() completed with data {getNetworkDetailsRes.data}");
            return Result<string>.Ok(getNetworkDetailsRes.data);
        }

        public static async Task<Result<string>> SignTransaction(string unsignedTransactionEnvelope, string networkPassphrase)
        {
            if (signTransactionTaskSource != null && !signTransactionTaskSource.Task.IsCompleted)
            {
                throw new Exception("SignTransaction() is already in progress");
            }
            SetWalletBusy(true);
            signTransactionTaskSource = new TaskCompletionSource<JSResponse>();
            JSSignTransaction(unsignedTransactionEnvelope, networkPassphrase);
            JSResponse signTransactionRes = await signTransactionTaskSource.Task;
            signTransactionTaskSource = null;

            Result<string> result;
            if (signTransactionRes.code == JsSignTransactionUserRejectedCode)
            {
                Debug.Log("SignTransaction() cancelled by user");
                string cancellationMessage = ExtractFreighterErrorMessage(signTransactionRes.data);
                if (string.IsNullOrWhiteSpace(cancellationMessage))
                {
                    cancellationMessage = "User cancelled signing request.";
                }
                result = Result<string>.Err(StatusCode.WALLET_SIGNING_CANCELLED, cancellationMessage);
            }
            else if (signTransactionRes.code != 1)
            {
                Debug.Log("SignTransaction() failed with code " + signTransactionRes.code);
                string failureDetails = ExtractFreighterErrorMessage(signTransactionRes.data);
                string errorMessage = string.IsNullOrWhiteSpace(failureDetails)
                    ? "failed to sign transaction"
                    : $"failed to sign transaction {failureDetails}";
                result = Result<string>.Err(StatusCode.WALLET_SIGNING_ERROR, errorMessage);
            }
            else
            {
                Debug.Log($"SignTransaction() completed with data {signTransactionRes.data}");
                result = Result<string>.Ok(signTransactionRes.data);
            }

            SetWalletBusy(false);
            return result;
        }

        public void StellarResponse(string json)
        {
            try
            {
                JSResponse response = JsonUtility.FromJson<JSResponse>(json);
                if (response.code == -666)
                {
                    throw new Exception($"StellarResponse() got unspecified error: {response}");
                }
                TaskCompletionSource<JSResponse> task = response.function switch
                {
                    "_JSCheckWallet" => checkWalletTaskSource,
                    "_JSGetFreighterAddress" => getAddressTaskSource,
                    "_JSGetNetworkDetails" => getNetworkDetailsTaskSource,
                    "_JSSignTransaction" => signTransactionTaskSource,
                    _ => throw new Exception($"StellarResponse() function not found {response}"),
                };
                if (task == null)
                {
                    throw new Exception($"StellarResponse() task was null: {response}");
                }
                task.SetResult(response);
            }
            catch (Exception e)
            {
                Debug.Log($"StellarResponse() unspecified error {e}");
                throw;
            }
        }

        static string ExtractFreighterErrorMessage(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData))
            {
                return null;
            }

            string trimmed = rawData.Trim();
            if (string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "undefined", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            try
            {
                var anon = JsonConvert.DeserializeAnonymousType(trimmed, new { message = string.Empty });
                if (!string.IsNullOrWhiteSpace(anon?.message))
                {
                    return anon.message;
                }
            }
            catch (JsonException)
            {
                // Fall back to the raw data
            }

            return trimmed;
        }
    }

    public class NetworkDetails
    {
        public string network { get; set; }
        public string networkUrl { get; set; }
        public string networkPassphrase { get; set; }
        public string sorobanRpcUrl { get; set; }
    }

    [Serializable]
    public class JSResponse
    {
        public string function;
        public int code;
        public string data;
    }
}
