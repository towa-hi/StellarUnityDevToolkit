using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Stellar;
using Stellar.RPC;
using Stellar.Utilities;
using UnityEngine;
using UnityEngine.Networking;

namespace StellarSDK
{
    public static class StellarClient
    {
        public static bool EnableLogging;

        static void DebugLog(string message)
        {
            if (EnableLogging)
            {
                Debug.Log(message);
            }
        }

        static readonly byte[] exceededLimitPatternLower = Encoding.ASCII.GetBytes("exceeded_limit");
        static readonly byte[] exceededLimitPatternUpper = Encoding.ASCII.GetBytes("EXCEEDED_LIMIT");
        static readonly byte[] outOfFuelPattern = Encoding.ASCII.GetBytes("OutOfFuel");
        static readonly byte[] operationInstructionsExceedsPattern = Encoding.ASCII.GetBytes("operation instructions exceeds amount specified");

        static readonly JsonSerializerSettings jsonSettings = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = true,
                    OverrideSpecifiedNames = false,
                }
            },
            NullValueHandling = NullValueHandling.Ignore,
        };

        public static void WarmUpJsonSerializer()
        {
            try
            {
                var dummyRequest = new JsonRpcRequest
                {
                    JsonRpc = "2.0",
                    Method = "getLedgerEntries",
                    Params = new GetLedgerEntriesParams
                    {
                        Keys = new[] { "dummy" }
                    },
                    Id = 1
                };

                long warmupStart = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string warmupJson = JsonConvert.SerializeObject(dummyRequest, jsonSettings);
                long warmupEnd = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                DebugLog($"JSON.NET warmup completed in {warmupEnd - warmupStart}ms");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"JSON.NET warmup failed: {e.Message}");
            }
        }

        public static SCVal.ScvAddress AccountStringToScvAddress(string accountAddress)
        {
            return new SCVal.ScvAddress()
            {
                address = new SCAddress.ScAddressTypeAccount()
                {
                    accountId = new AccountID(new PublicKey.PublicKeyTypeEd25519()
                    {
                        ed25519 = StrKey.DecodeStellarAccountId(accountAddress),
                    }),
                },
            };
        }

        public static SCVal.ScvAddress ContractStringToScvAddress(string contractAddress)
        {
            return new SCVal.ScvAddress()
            {
                address = new SCAddress.ScAddressTypeContract()
                {
                    contractId = new Hash(StrKey.DecodeContractId(contractAddress)),
                },
            };
        }
        
        public static async Task<Result<(SimulateTransactionResult, SendTransactionResult, GetTransactionResult)>> CallContractFunction(NetworkContext context, string functionName, SCVal[] args, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "CallContractFunction");
            var simResult = await SimulateContractFunction(context, functionName, args, task);
            if (simResult.IsError)
            {
                return Result<(SimulateTransactionResult, SendTransactionResult, GetTransactionResult)>.Err(simResult);
            }
            (Transaction transaction, SimulateTransactionResult sim) = simResult.Value;
            if (sim is not { Error: null })
            {
                StatusCode code = HasContractError(sim) ? StatusCode.CONTRACT_ERROR : StatusCode.SIMULATION_FAILED;
                return Result<(SimulateTransactionResult, SendTransactionResult, GetTransactionResult)>.Err(code, (sim, null, null), $"CallContractFunction {functionName} failed because the simulation result was not successful");
            }
            long minResourceFee = long.Parse(sim.MinResourceFee) * 2;
            sim.MinResourceFee = minResourceFee.ToString();
            Transaction assembledTransaction = sim.ApplyTo(transaction);
            Result<string> signResult = await SignAndEncodeTransaction(context, assembledTransaction, task);
            if (signResult.IsError)
            {
                if (signResult.Code == StatusCode.WALLET_SIGNING_CANCELLED)
                {
                    string cancellationMessage = string.IsNullOrWhiteSpace(signResult.Message)
                        ? $"CallContractFunction {functionName} cancelled by user"
                        : signResult.Message;
                    return Result<(SimulateTransactionResult, SendTransactionResult, GetTransactionResult)>.Err(StatusCode.WALLET_SIGNING_CANCELLED, (sim, null, null), cancellationMessage);
                }

                string signFailureMessage = string.IsNullOrWhiteSpace(signResult.Message)
                    ? $"CallContractFunction {functionName} failed because failed to sign"
                    : signResult.Message;
                return Result<(SimulateTransactionResult, SendTransactionResult, GetTransactionResult)>.Err(StatusCode.WALLET_ERROR, (sim, null, null), signFailureMessage);
            }
            var sendResult = await SendTransactionAsync(context, new SendTransactionParams()
            {
                Transaction = signResult.Value,
            }, task);
            if (sendResult.IsError)
            {
                return Result<(SimulateTransactionResult, SendTransactionResult, GetTransactionResult)>.Err(sendResult);
            }

            SendTransactionResult send = sendResult.Value;
            if (send is not { ErrorResult: null })
            {
                return Result<(SimulateTransactionResult, SendTransactionResult, GetTransactionResult)>.Err(StatusCode.TRANSACTION_SEND_FAILED, (sim, send, null), $"CallContractFunction {functionName} failed because the transaction sending result was not successful");
            }
            var getResult = await WaitForGetTransactionResult(context, send.Hash, context.pollRateMs, task);
            GetTransactionResult get = getResult.Value;
            return Result<(SimulateTransactionResult, SendTransactionResult, GetTransactionResult)>.Ok((sim, send, get));
        }

        public static async Task<Result<(Transaction, SimulateTransactionResult)>> SimulateContractFunction(NetworkContext context, string functionName, SCVal[] args, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "SimulateContractFunction");
            var accountEntryResult = await ReqAccountEntry(context, task);
            if (accountEntryResult.IsError)
            {
                return Result<(Transaction, SimulateTransactionResult)>.Err(accountEntryResult);
            }
            AccountEntry accountEntry = accountEntryResult.Value;
            Transaction invokeContractTransaction = BuildInvokeContractTransaction(context, accountEntry, functionName, args, true);
            var result = await SimulateTransactionAsync(context, new SimulateTransactionParams()
            {
                Transaction = EncodeTransaction(invokeContractTransaction),
                ResourceConfig = new(),
            }, task);
            if (result.IsError)
            {
                return Result<(Transaction, SimulateTransactionResult)>.Err(result);
            }
            SimulateTransactionResult simulateTransactionResult = result.Value;
            if (simulateTransactionResult.Error != null)
            {
                StatusCode code = HasContractError(simulateTransactionResult) ? StatusCode.CONTRACT_ERROR : StatusCode.SIMULATION_FAILED;
                return Result<(Transaction, SimulateTransactionResult)>.Err(code, (invokeContractTransaction, simulateTransactionResult), $"SimulateContractFunction {functionName} failed because the simulation result was not successful");
            }
            return Result<(Transaction, SimulateTransactionResult)>.Ok((invokeContractTransaction, simulateTransactionResult));
        }

        public static async Task<Result<AccountEntry>> ReqAccountEntry(NetworkContext context, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "ReqAccountEntry");
            string encodedKey = EncodedAccountKey(context);
            var result = await GetLedgerEntriesAsync(context, new GetLedgerEntriesParams()
            {
                Keys = new[] { encodedKey },
            }, task);
            if (result.IsError)
            {
                return Result<AccountEntry>.Err(result);
            }
            GetLedgerEntriesResult getLedgerEntriesResult = result.Value;
            if (getLedgerEntriesResult.Entries.Count != 1)
            {
                return Result<AccountEntry>.Err(StatusCode.ENTRY_NOT_FOUND, $"ReqAccountEntry on {context.userAccount.XdrPublicKey} failed because there was not exactly one entry");
            }
            LedgerEntry.dataUnion.Account entry = getLedgerEntriesResult.Entries.First().LedgerEntryData as LedgerEntry.dataUnion.Account;
            return Result<AccountEntry>.Ok(entry?.account);
        }

        public static async Task<Result<string>> GetSEP50AssetBalance(NetworkContext context, string assetContractAddress, string assetOwnerAddress, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "GetSEP50AssetBalance");
            SCVal.ScvAddress assetOwnerAddressScv = AccountStringToScvAddress(assetOwnerAddress);
            SCVal.ScvAddress assetContractAddressScv = ContractStringToScvAddress(assetContractAddress);
            var result = await SimulateContractFunction(context, "balance", new SCVal[] {
                assetOwnerAddressScv,
                assetContractAddressScv,
            } ), task);
            if (result.IsError)
            {
                return Result<string>.Err(result);
            }
            
        }

        public static async Task<Result<LedgerEntry.dataUnion.Trustline>> GetAssets(NetworkContext context, string accountIdOverride = null, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "GetAssets");
            string encodedKey = EncodedTrustlineKey(context, accountIdOverride);
            var result = await GetLedgerEntriesAsync(context, new GetLedgerEntriesParams()
            {
                Keys = new[] { encodedKey },
            }, task);
            if (result.IsError)
            {
                return Result<LedgerEntry.dataUnion.Trustline>.Err(result);
            }
            GetLedgerEntriesResult getLedgerEntriesResult = result.Value;
            if (getLedgerEntriesResult.Entries.Count == 0)
            {
                return Result<LedgerEntry.dataUnion.Trustline>.Err(StatusCode.ENTRY_NOT_FOUND, "GetAssets: no trustline entries found");
            }
            LedgerEntry.dataUnion.Trustline entry = getLedgerEntriesResult.Entries.First().LedgerEntryData as LedgerEntry.dataUnion.Trustline;
            return Result<LedgerEntry.dataUnion.Trustline>.Ok(entry);
        }

        static Transaction BuildInvokeContractTransaction(NetworkContext context, AccountEntry accountEntry, string functionName, SCVal[] args, bool increment)
        {
            List<Operation> operations = new();
            Operation operation = new()
            {
                sourceAccount = context.userAccount,
                body = new Operation.bodyUnion.InvokeHostFunction()
                {
                    invokeHostFunctionOp = new InvokeHostFunctionOp()
                    {
                        auth = Array.Empty<SorobanAuthorizationEntry>(),
                        hostFunction = new HostFunction.HostFunctionTypeInvokeContract()
                        {
                            invokeContract = new InvokeContractArgs()
                            {
                                contractAddress = new SCAddress.ScAddressTypeContract()
                                {
                                    contractId = new Hash(StrKey.DecodeContractId(context.contractAddress)),
                                } ,
                                functionName = new SCSymbol(functionName),
                                args = args,
                            },
                        },
                    },
                },
            };
            if (increment)
            {
                accountEntry.seqNum.Increment();
            }
            operations.Add(operation);
            return new Transaction()
            {
                sourceAccount = operation.sourceAccount,
                fee = 100000,
                memo = new Memo.MemoNone(),
                seqNum = accountEntry.seqNum,
                cond = new Preconditions.PrecondNone(),
                ext = new Transaction.extUnion.case_0(),
                operations = operations.ToArray(),
            };
        }

        static string EncodeTransaction(Transaction transaction)
        {
            TransactionEnvelope.EnvelopeTypeTx envelope = new()
            {
                v1 = new TransactionV1Envelope()
                {
                    tx = transaction,
                    signatures = Array.Empty<DecoratedSignature>(),
                },
            };
            return TransactionEnvelopeXdr.EncodeToBase64(envelope);
        }

        static async Task<Result<string>> SignAndEncodeTransaction(NetworkContext context, Transaction transaction, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "SignAndEncodeTransaction");
            if (context.signingMethod == NetworkContext.SigningMethod.UnityWallet)
            {
                if (context.unityWalletSigner == null)
                {
                    return Result<string>.Err(StatusCode.WALLET_NOT_AVAILABLE, "Unity wallet signing is selected, but no wallet signer is registered.");
                }

                Result<string> signTransactionRes = await context.unityWalletSigner(EncodeTransaction(transaction), Network.Current.NetworkPassphrase);
                if (signTransactionRes.IsError)
                {
                    return Result<string>.Err(signTransactionRes);
                }
                return Result<string>.Ok(signTransactionRes.Value);
            }

            if (context.signingMethod == NetworkContext.SigningMethod.PrivateKey)
            {
                DecoratedSignature signature = transaction.Sign(context.userAccount);
                TransactionEnvelope.EnvelopeTypeTx envelope = new()
                {
                    v1 = new TransactionV1Envelope()
                    {
                        tx = transaction,
                        signatures = new[] { signature },
                    },
                };
                return Result<string>.Ok(TransactionEnvelopeXdr.EncodeToBase64(envelope));
            }

            return Result<string>.Err(StatusCode.WALLET_ERROR, $"Unsupported signing method: {context.signingMethod}");
        }

        static async Task<Result<GetTransactionResult>> WaitForGetTransactionResult(NetworkContext context, string txHash, int delayMS, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "WaitForGetTransactionResult");
            int attempts = 0;
            await AsyncDelay.Delay(delayMS);
            while (attempts < context.maxAttempts)
            {
                attempts++;
                var result = await GetTransactionAsync(context, new GetTransactionParams()
                {
                    Hash = txHash,
                }, task);
                if (result.IsError)
                {
                    return Result<GetTransactionResult>.Err(result);
                }
                GetTransactionResult completion = result.Value;
                switch (completion.Status)
                {
                    case GetTransactionResult_Status.FAILED:
                        DebugLog("WaitForTransaction: FAILED");
                        string failureMessage = completion.ResultMetaXdr;
                        return Result<GetTransactionResult>.Err(StatusCode.TRANSACTION_FAILED, completion, failureMessage);
                    case GetTransactionResult_Status.NOT_FOUND:
                        await AsyncDelay.Delay(delayMS);
                        continue;
                    case GetTransactionResult_Status.SUCCESS:
                        DebugLog("WaitForTransaction: SUCCESS");
                        return Result<GetTransactionResult>.Ok(completion);
                }
            }
            DebugLog("WaitForTransaction: timed out");
            return Result<GetTransactionResult>.Err(StatusCode.TRANSACTION_TIMEOUT);
        }

        public static bool IsExceededLimitError(GetTransactionResult getResult)
        {
            if (getResult == null)
            {
                return false;
            }
            if (TryDecodeBase64(getResult.ResultMetaXdr, out byte[] data))
            {
                if (ByteArrayContainsSequence(data, exceededLimitPatternLower) ||
                    ByteArrayContainsSequence(data, exceededLimitPatternUpper) ||
                    ByteArrayContainsSequence(data, outOfFuelPattern) ||
                    ByteArrayContainsSequence(data, operationInstructionsExceedsPattern))
                {
                    DebugLog("IsExceededLimitError: matched exceeded-limit pattern in ResultMetaXdr");
                    return true;
                }
            }
            else
            {
                DebugLog("IsExceededLimitError: ResultMetaXdr missing or failed to decode");
            }

            if (getResult.DiagnosticEventsXdr != null)
            {
                foreach (string diagnosticEventString in getResult.DiagnosticEventsXdr)
                {
                    using MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(diagnosticEventString));
                    DiagnosticEvent diagnosticEvent = DiagnosticEventXdr.Decode(new XdrReader(memoryStream));
                    if (diagnosticEvent._event.body is ContractEvent.bodyUnion.case_0 v0)
                    {
                        foreach (SCVal topic in v0.v0.topics)
                        {
                            if (topic is SCVal.ScvError { error: SCError.SceBudget })
                            {
                                DebugLog("IsExceededLimitError: matched exceeded-limit pattern in DiagnosticEvent");
                                return true;
                            }
                        }
                    }
                }
            }
            else
            {
                DebugLog("IsExceededLimitError: DiagnosticEventsXdr was null");
            }

            return false;
        }

        static bool TryDecodeBase64(string base64, out byte[] data)
        {
            if (string.IsNullOrEmpty(base64))
            {
                data = null;
                return false;
            }

            try
            {
                data = Convert.FromBase64String(base64);
                return true;
            }
            catch (FormatException)
            {
                data = null;
                return false;
            }
        }

        static bool ByteArrayContainsSequence(byte[] haystack, byte[] needle)
        {
            if (haystack == null || needle == null || needle.Length == 0 || haystack.Length < needle.Length)
            {
                return false;
            }

            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                int j = 0;
                for (; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        break;
                    }
                }

                if (j == needle.Length)
                {
                    return true;
                }
            }

            return false;
        }

        static async Task<Result<SimulateTransactionResult>> SimulateTransactionAsync(NetworkContext context, SimulateTransactionParams parameters = null, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "SimulateTransactionAsync");
            var result = await SendJsonRequest<SimulateTransactionResult>(context, new()
            {
                JsonRpc = "2.0",
                Method = "simulateTransaction",
                Params = parameters,
                Id = 1,
            });
            if (result.IsError)
            {
                return Result<SimulateTransactionResult>.Err(result);
            }
            SimulateTransactionResult transactionResult = result.Value;
            return Result<SimulateTransactionResult>.Ok(transactionResult);
        }

        static async Task<Result<SendTransactionResult>> SendTransactionAsync(NetworkContext context, SendTransactionParams parameters = null, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "SendTransactionAsync");
            var result = await SendJsonRequest<SendTransactionResult>(context, new()
            {
                JsonRpc = "2.0",
                Method = "sendTransaction",
                Params = parameters,
                Id = 1,
            });
            if (result.IsError)
            {
                return Result<SendTransactionResult>.Err(result);
            }
            SendTransactionResult transactionResult = result.Value;
            return Result<SendTransactionResult>.Ok(transactionResult);
        }

        public static async Task<Result<GetEventsResult>> GetEventsAsync(NetworkContext context, GetEventsParams parameters, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "GetEventsAsync");
            var result = await SendJsonRequest<GetEventsResult>(context, new()
            {
                JsonRpc = "2.0",
                Method = "getEvents",
                Params = parameters,
                Id = 1,
            });
            if (result.IsError)
            {
                return Result<GetEventsResult>.Err(result);
            }
            return Result<GetEventsResult>.Ok(result.Value);
        }

        public static async Task<Result<GetLedgerEntriesResult>> GetLedgerEntriesAsync(NetworkContext context, GetLedgerEntriesParams parameters = null, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "GetLedgerEntriesAsync");
            var result = await SendJsonRequest<GetLedgerEntriesResult>(context, new()
            {
                JsonRpc = "2.0",
                Method = "getLedgerEntries",
                Params = parameters,
                Id = 1
            });
            if (result.IsError)
            {
                return Result<GetLedgerEntriesResult>.Err(result);
            }
            return Result<GetLedgerEntriesResult>.Ok(result.Value);
        }

        static async Task<Result<GetTransactionResult>> GetTransactionAsync(NetworkContext context, GetTransactionParams parameters = null, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "GetTransactionAsync");
            var result = await SendJsonRequest<GetTransactionResult>(context, new()
            {
                JsonRpc = "2.0",
                Method = "getTransaction",
                Params = parameters,
                Id = 1
            });
            if (result.IsError)
            {
                return Result<GetTransactionResult>.Err(result);
            }
            return Result<GetTransactionResult>.Ok(result.Value);
        }

        public static async Task<Result<GetTransactionsResult>> GetTransactionsAsync(NetworkContext context, GetTransactionsParams parameters, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "GetTransactionsAsync");
            var result = await SendJsonRequest<GetTransactionsResult>(context, new()
            {
                JsonRpc = "2.0",
                Method = "getTransactions",
                Params = parameters,
                Id = 1,
            });
            if (result.IsError)
            {
                return Result<GetTransactionsResult>.Err(result);
            }
            return Result<GetTransactionsResult>.Ok(result.Value);
        }

        public static async Task<Result<GetHealthResult>> GetHealthAsync(NetworkContext context, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "GetHealthAsync");
            var result = await SendJsonRequest<GetHealthResult>(context, new()
            {
                JsonRpc = "2.0",
                Method = "getHealth",
                Id = 1,
            });
            if (result.IsError)
            {
                return Result<GetHealthResult>.Err(result);
            }
            return Result<GetHealthResult>.Ok(result.Value);
        }

        public static async Task<Result<GetLatestLedgerResult>> GetLatestLedgerAsync(NetworkContext context, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "GetLatestLedgerAsync");
            var result = await SendJsonRequest<GetLatestLedgerResult>(context, new()
            {
                JsonRpc = "2.0",
                Method = "getLatestLedger",
                Id = 1,
            });
            if (result.IsError)
            {
                return Result<GetLatestLedgerResult>.Err(result);
            }
            return Result<GetLatestLedgerResult>.Ok(result.Value);
        }

        public static async Task<Result<GetNetworkResult>> GetNetworkAsync(NetworkContext context, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "GetNetworkAsync");
            var result = await SendJsonRequest<GetNetworkResult>(context, new()
            {
                JsonRpc = "2.0",
                Method = "getNetwork",
                Id = 1,
            });
            if (result.IsError)
            {
                return Result<GetNetworkResult>.Err(result);
            }
            return Result<GetNetworkResult>.Ok(result.Value);
        }

        public static async Task<Result<GetVersionInfoResult>> GetVersionInfoAsync(NetworkContext context, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "GetVersionInfoAsync");
            var result = await SendJsonRequest<GetVersionInfoResult>(context, new()
            {
                JsonRpc = "2.0",
                Method = "getVersionInfo",
                Id = 1,
            });
            if (result.IsError)
            {
                return Result<GetVersionInfoResult>.Err(result);
            }
            return Result<GetVersionInfoResult>.Ok(result.Value);
        }

        public static async Task<Result<GetFeeStatsResult>> GetFeeStatsAsync(NetworkContext context, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "GetFeeStatsAsync");
            var result = await SendJsonRequest<GetFeeStatsResult>(context, new()
            {
                JsonRpc = "2.0",
                Method = "getFeeStats",
                Id = 1,
            });
            if (result.IsError)
            {
                return Result<GetFeeStatsResult>.Err(result);
            }
            return Result<GetFeeStatsResult>.Ok(result.Value);
        }

        static async Task<Result<T>> SendJsonRequest<T>(NetworkContext context, JsonRpcRequest request)
        {
            string json = JsonConvert.SerializeObject(request, jsonSettings);
            UnityWebRequest unityWebRequest = new(context.serverUri, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            unityWebRequest.SetRequestHeader("Content-Type", "application/json");
            DebugLog($"SendJsonRequest: request: {json}");
            await unityWebRequest.SendWebRequest();
            if (unityWebRequest.result == UnityWebRequest.Result.ConnectionError || unityWebRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                return Result<T>.Err(StatusCode.NETWORK_ERROR, $"SendJsonRequest: error: {unityWebRequest.error}");
            }
            DebugLog($"SendJsonRequest: response: {unityWebRequest.downloadHandler.text}");
            string responseText = unityWebRequest.downloadHandler.text;

            T result = JsonConvert.DeserializeObject<JsonRpcResponse<T>>(responseText, jsonSettings).Result;
            if (result == null)
            {
                return Result<T>.Err(StatusCode.DESERIALIZATION_ERROR, "SendJsonRequest: error: JSON deserialization failed");
            }
            return Result<T>.Ok(result);
        }

        static bool HasContractError(SimulateTransactionResult simulate)
        {
            if (simulate == null || simulate.DiagnosticEvents == null) return false;
            foreach (var diag in simulate.DiagnosticEvents.Where(d => !d.inSuccessfulContractCall))
            {
                if (diag._event?.body is ContractEvent.bodyUnion.case_0 body)
                {
                    foreach (SCVal topic in body.v0.topics)
                    {
                        if (topic is SCVal.ScvError { error: SCError.SceContract })
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static LedgerKey MakeLedgerKey(NetworkContext context, string sym, object key, ContractDataDurability durability)
        {
            SCVal scKey = SCUtility.NativeToSCVal(key);
            SCVal.ScvVec enumKey = new()
            {
                vec = new SCVec(new[]
                {
                    new SCVal.ScvSymbol
                    {
                        sym = sym,
                    },
                    scKey,
                }),
            };
            return new LedgerKey.ContractData
            {
                contractData = new LedgerKey.contractDataStruct
                {
                    contract = new SCAddress.ScAddressTypeContract
                    {
                        contractId = new Hash(StrKey.DecodeContractId(context.contractAddress)),
                    },
                    key = enumKey,
                    durability = durability,
                },
            };
        }

        static string EncodedAccountKey(NetworkContext context)
        {
            return LedgerKeyXdr.EncodeToBase64(new LedgerKey.Account()
            {
                account = new LedgerKey.accountStruct()
                {
                    accountID = context.userAccount.XdrPublicKey,
                },
            });
        }

        static string EncodedTrustlineKey(NetworkContext context, string accountIdOverride = null)
        {
            string code = context.assetCode;
            string issuerAccountId = context.assetIssuerAddress;
            AccountID issuerAccount = MuxedAccount.FromAccountId(issuerAccountId).XdrPublicKey;
            AccountID accountId = string.IsNullOrEmpty(accountIdOverride)
                ? context.userAccount.XdrPublicKey
                : MuxedAccount.FromAccountId(accountIdOverride).XdrPublicKey;
            byte[] codeBytes = Encoding.ASCII.GetBytes(code);
            return LedgerKeyXdr.EncodeToBase64(new LedgerKey.Trustline
            {
                trustLine = new LedgerKey.trustLineStruct
                {
                    accountID = accountId,
                    asset = new TrustLineAsset.AssetTypeCreditAlphanum4
                    {
                        alphaNum4 = new AlphaNum4
                        {
                            assetCode = new AssetCode4
                            {
                                InnerValue = codeBytes,
                            },
                            issuer = issuerAccount,
                        },
                    },
                },
            });
        }

        public static async Task<Result<MuxedAccount>> CreateAccount(StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "CreateAccount");
            MuxedAccount newAccount = MuxedAccount.Random();
            string friendbotUrl = $"https://friendbot.stellar.org?addr={newAccount.AccountId}";
            UnityWebRequest unityWebRequest = new(friendbotUrl, "GET")
            {
                downloadHandler = new DownloadHandlerBuffer(),
            };
            unityWebRequest.SetRequestHeader("Content-Type", "application/json");
            await unityWebRequest.SendWebRequest();
            if (unityWebRequest.result == UnityWebRequest.Result.ConnectionError || unityWebRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                return Result<MuxedAccount>.Err(StatusCode.NETWORK_ERROR, $"CreateAccount: error: {unityWebRequest.error}");
            }
            return Result<MuxedAccount>.Ok(newAccount);
        }
    }
}
