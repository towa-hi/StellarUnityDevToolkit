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
using StellarSDK;
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
            var simResult = await SimulateContractFunction(context, functionName, args, false, task);
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

        public static async Task<Result<(Transaction, SimulateTransactionResult)>> SimulateContractFunction(NetworkContext context, string functionName, SCVal[] args, bool skipAccountEntry = false, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "SimulateContractFunction");
            AccountEntry accountEntry = new AccountEntry() {
                accountID = new AccountID(new PublicKey.PublicKeyTypeEd25519()
                {
                    ed25519 = StrKey.DecodeStellarAccountId(context.userAccount.AccountId),
                }),
                seqNum = new SequenceNumber(42069),
                balance = new int64(0),
                numSubEntries = 0,
                flags = 0,
                inflationDest = null    ,
                homeDomain = new string32(""),
                thresholds = new Thresholds(new byte[] { 0, 0, 0, 0 }),
                signers = new Signer[] { },
                ext = new AccountEntry.extUnion.case_0(),
            };
            if (!skipAccountEntry)
            {
                var accountEntryResult = await ReqAccountEntry(context, task);
                if (accountEntryResult.IsError)
                {
                    return Result<(Transaction, SimulateTransactionResult)>.Err(accountEntryResult);
                }
                accountEntry = accountEntryResult.Value;
            }
            Transaction invokeContractTransaction = BuildInvokeContractTransaction(context, accountEntry, functionName, args, true);
            string encodedTransaction = EncodeTransaction(invokeContractTransaction);
            Debug.Log($"SimulateContractFunction {functionName} tx XDR: {encodedTransaction}");
            var result = await SimulateTransactionAsync(context, new SimulateTransactionParams()
            {
                Transaction = encodedTransaction,
                ResourceConfig = new(),
            }, task);
            if (result.IsError)
            {
                return Result<(Transaction, SimulateTransactionResult)>.Err(result);
            }
            SimulateTransactionResult simulateTransactionResult = result.Value;
            if (simulateTransactionResult.Error != null)
            {
                Debug.LogError($"SimulateContractFunction {functionName} failed simulation: {simulateTransactionResult.Error}");
                StatusCode code = HasContractError(simulateTransactionResult) ? StatusCode.CONTRACT_ERROR : StatusCode.SIMULATION_FAILED;
                return Result<(Transaction, SimulateTransactionResult)>.Err(code, (invokeContractTransaction, simulateTransactionResult), $"SimulateContractFunction {functionName} failed: {simulateTransactionResult.Error}");
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

        public static async Task<Result<SorobanInvocationMeta>> InvokeSEP50AssetMint(NetworkContext context, string assetOwnerAddressOverride = null, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "InvokeSEP50AssetMint");
            SCVal.ScvAddress ownerAddress = !string.IsNullOrWhiteSpace(assetOwnerAddressOverride)
                ? AccountStringToScvAddress(assetOwnerAddressOverride)
                : AccountStringToScvAddress(context.userAccount.AccountId);
            var result = await CallContractFunction(context, "mint", new SCVal[] {
                ownerAddress,
            }, task);
            if (result.IsError)
            {
                return Result<SorobanInvocationMeta>.Err(result);
            }
            if (result.Value.Item3 is not GetTransactionResult getResult)
            {
                return Result<SorobanInvocationMeta>.Err(StatusCode.DESERIALIZATION_ERROR, "InvokeSEP50AssetMint failed because the transaction result is not a getTransaction response.");
            }
            return GetSorobanMeta(getResult);
        }

        public static async Task<Result<int>> SimSEP50AssetBalance(NetworkContext context, string assetOwnerAddressOverride = null, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "GetSEP50AssetBalance");
            SCVal.ScvAddress ownerAddress = !string.IsNullOrWhiteSpace(assetOwnerAddressOverride)
                ? AccountStringToScvAddress(assetOwnerAddressOverride)
                : AccountStringToScvAddress(context.userAccount.AccountId);
            var result = await SimulateContractFunction(context, "balance", new SCVal[] {
                ownerAddress,
            }, true, task);
            if (result.IsError)
            {
                Debug.LogError($"GetSEP50AssetBalance simulation failed: code={result.Code}, message={result.Message}");
                return Result<int>.Err(result);
            }
            SimulateTransactionResult simulation = result.Value.Item2;
            SCVal rawBalance = simulation.Results?.FirstOrDefault()?.Result;
            if (rawBalance == null)
            {
                return Result<int>.Err(StatusCode.DESERIALIZATION_ERROR, "GetSEP50AssetBalance failed because simulation returned no balance value.");
            }
            if (rawBalance is not SCVal.ScvU32 u32Balance)
            {
                return Result<int>.Err(StatusCode.DESERIALIZATION_ERROR, $"GetSEP50AssetBalance expected u32 balance, got {rawBalance.GetType().Name}.");
            }
            int parsedBalance = checked((int)u32Balance.u32.InnerValue);
            return Result<int>.Ok(parsedBalance);
        }

        public static async Task<Result<string>> SimSEP50AssetName(NetworkContext context, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "SimSEP50AssetName");
            var result = await SimulateContractFunction(context, "name", Array.Empty<SCVal>(), true, task);
            if (result.IsError)
            {
                Debug.LogError($"SimSEP50AssetName simulation failed: code={result.Code}, message={result.Message}");
                return Result<string>.Err(result);
            }
            SimulateTransactionResult simulation = result.Value.Item2;
            SCVal rawName = simulation.Results?.FirstOrDefault()?.Result;
            if (rawName == null)
            {
                return Result<string>.Err(StatusCode.DESERIALIZATION_ERROR, "SimSEP50AssetName failed because simulation returned no name value.");
            }
            if (rawName is not SCVal.ScvString stringName)
            {
                return Result<string>.Err(StatusCode.DESERIALIZATION_ERROR, $"SimSEP50AssetName expected string name, got {rawName.GetType().Name}.");
            }
            return Result<string>.Ok(stringName.str.InnerValue);
        }

        public static async Task<Result<string>> SimSEP50AssetSymbol(NetworkContext context, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "SimSEP50AssetSymbol");
            var result = await SimulateContractFunction(context, "symbol", Array.Empty<SCVal>(), true, task);
            if (result.IsError)
            {
                Debug.LogError($"SimSEP50AssetSymbol simulation failed: code={result.Code}, message={result.Message}");
                return Result<string>.Err(result);
            }
            SimulateTransactionResult simulation = result.Value.Item2;
            SCVal rawSymbol = simulation.Results?.FirstOrDefault()?.Result;
            if (rawSymbol == null)
            {
                return Result<string>.Err(StatusCode.DESERIALIZATION_ERROR, "SimSEP50AssetSymbol failed because simulation returned no symbol value.");
            }
            if (rawSymbol is not SCVal.ScvString stringSymbol)
            {
                return Result<string>.Err(StatusCode.DESERIALIZATION_ERROR, $"SimSEP50AssetSymbol expected string symbol, got {rawSymbol.GetType().Name}.");
            }
            return Result<string>.Ok(stringSymbol.str.InnerValue);
        }

        public static async Task<Result<SCAddress>> SimSEP50AssetOwner_Of(NetworkContext context, int tokenId, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "SimSEP50AssetOwner_Of");
            var result = await SimulateContractFunction(context, "owner_of", new SCVal[] {
                new SCVal.ScvU32 { u32 = new uint32(checked((uint)tokenId)) },
            }, true, task);
            if (result.IsError)
            {
                Debug.LogError($"SimSEP50AssetOwner_Of simulation failed: code={result.Code}, message={result.Message}");
                return Result<SCAddress>.Err(result);
            }
            SimulateTransactionResult simulation = result.Value.Item2;
            SCVal rawOwner = simulation.Results?.FirstOrDefault()?.Result;
            if (rawOwner == null)
            {
                return Result<SCAddress>.Err(StatusCode.DESERIALIZATION_ERROR, "SimSEP50AssetOwner_Of failed because simulation returned no owner value.");
            }
            if (rawOwner is not SCVal.ScvAddress addressOwner)
            {
                return Result<SCAddress>.Err(StatusCode.DESERIALIZATION_ERROR, $"SimSEP50AssetOwner_Of expected address owner, got {rawOwner.GetType().Name}.");
            }
            return Result<SCAddress>.Ok(addressOwner.address);
        }

        public static async Task<Result<string>> SimSEP50AssetToken_Uri(NetworkContext context, int tokenId, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "SimSEP50AssetToken_Uri");
            var result = await SimulateContractFunction(context, "token_uri", new SCVal[] {
                new SCVal.ScvU32 { u32 = new uint32(checked((uint)tokenId)) },
            }, true, task);
            if (result.IsError)
            {
                Debug.LogError($"SimSEP50AssetToken_Uri simulation failed: code={result.Code}, message={result.Message}");
                return Result<string>.Err(result);
            }
            SimulateTransactionResult simulation = result.Value.Item2;
            SCVal rawTokenUri = simulation.Results?.FirstOrDefault()?.Result;
            if (rawTokenUri == null)
            {
                return Result<string>.Err(StatusCode.DESERIALIZATION_ERROR, "SimSEP50AssetToken_Uri failed because simulation returned no token URI value.");
            }
            if (rawTokenUri is not SCVal.ScvString stringTokenUri)
            {
                return Result<string>.Err(StatusCode.DESERIALIZATION_ERROR, $"SimSEP50AssetToken_Uri expected string token URI, got {rawTokenUri.GetType().Name}.");
            }
            return Result<string>.Ok(stringTokenUri.str.InnerValue);
        }

        public static async Task<Result<int>> SimSEP50AssetTotal_Supply(NetworkContext context, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "SimSEP50AssetTotal_Supply");
            var result = await SimulateContractFunction(context, "total_supply", Array.Empty<SCVal>(), true, task);
            if (result.IsError)
            {
                Debug.LogError($"SimSEP50AssetTotal_Supply simulation failed: code={result.Code}, message={result.Message}");
                return Result<int>.Err(result);
            }
            SimulateTransactionResult simulation = result.Value.Item2;
            SCVal rawTotalSupply = simulation.Results?.FirstOrDefault()?.Result;
            if (rawTotalSupply == null)
            {
                return Result<int>.Err(StatusCode.DESERIALIZATION_ERROR, "SimSEP50AssetTotal_Supply failed because simulation returned no total supply value.");
            }
            if (rawTotalSupply is not SCVal.ScvU32 u32TotalSupply)
            {
                return Result<int>.Err(StatusCode.DESERIALIZATION_ERROR, $"SimSEP50AssetTotal_Supply expected u32 total supply, got {rawTotalSupply.GetType().Name}.");
            }
            int parsedTotalSupply = checked((int)u32TotalSupply.u32.InnerValue);
            return Result<int>.Ok(parsedTotalSupply);
        }

        public static async Task<Result<Dictionary<int, string>>> ReqSEP50AssetOwnerMap(NetworkContext context, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "ReqSEP50AssetOwnerMap");
            const int maxKeysPerRequest = 200;

            Result<int> totalSupplyResult = await SimSEP50AssetTotal_Supply(context, task);
            if (totalSupplyResult.IsError)
            {
                return Result<Dictionary<int, string>>.Err(totalSupplyResult);
            }

            int totalSupply = totalSupplyResult.Value;
            if (totalSupply == 0)
            {
                return Result<Dictionary<int, string>>.Ok(new Dictionary<int, string>());
            }

            var ownerMap = new Dictionary<int, string>(totalSupply);
            for (int batchStart = 0; batchStart < totalSupply; batchStart += maxKeysPerRequest)
            {
                int batchCount = Math.Min(maxKeysPerRequest, totalSupply - batchStart);
                var keys = new string[batchCount];
                var expectedTokenIds = new int[batchCount];
                for (int i = 0; i < batchCount; i++)
                {
                    int tokenId = batchStart + i;
                    expectedTokenIds[i] = tokenId;
                    keys[i] = EncodedOwnerLedgerKey(context, tokenId);
                }

                var ledgerResult = await GetLedgerEntriesAsync(context, new GetLedgerEntriesParams
                {
                    Keys = keys,
                }, task);
                if (ledgerResult.IsError)
                {
                    return Result<Dictionary<int, string>>.Err(ledgerResult);
                }

                GetLedgerEntriesResult ledgerEntries = ledgerResult.Value;
                if (ledgerEntries.Entries == null || ledgerEntries.Entries.Count != batchCount)
                {
                    return Result<Dictionary<int, string>>.Err(
                        StatusCode.ENTRY_NOT_FOUND,
                        $"ReqSEP50AssetOwnerMap: expected {batchCount} Owner entries, got {ledgerEntries.Entries?.Count ?? 0}.");
                }

                var foundTokenIds = new HashSet<int>();
                foreach (Entries entry in ledgerEntries.Entries)
                {
                    if (!TryParseOwnerLedgerEntry(entry, out int tokenId, out string ownerAddress))
                    {
                        return Result<Dictionary<int, string>>.Err(
                            StatusCode.DESERIALIZATION_ERROR,
                            "ReqSEP50AssetOwnerMap: failed to decode an Owner ledger entry.");
                    }

                    if (!foundTokenIds.Add(tokenId))
                    {
                        return Result<Dictionary<int, string>>.Err(
                            StatusCode.DESERIALIZATION_ERROR,
                            $"ReqSEP50AssetOwnerMap: duplicate Owner entry for token id {tokenId}.");
                    }

                    ownerMap[tokenId] = ownerAddress;
                }

                foreach (int tokenId in expectedTokenIds)
                {
                    if (!foundTokenIds.Contains(tokenId))
                    {
                        return Result<Dictionary<int, string>>.Err(
                            StatusCode.ENTRY_NOT_FOUND,
                            $"ReqSEP50AssetOwnerMap: missing Owner entry for token id {tokenId}.");
                    }
                }
            }

            return Result<Dictionary<int, string>>.Ok(ownerMap);
        }

        public static async Task<Result<bool>> InvokeSEP50AssetTransfer(NetworkContext context, int tokenId, string destinationAddress, StellarClientTask task = null)
        {
            using var _ = new StellarClientTask.Scope(task, "InvokeSEP50AssetTransfer");
            SCVal.ScvAddress fromAddress = AccountStringToScvAddress(context.userAccount.AccountId);
            SCVal.ScvAddress toAddress = AccountStringToScvAddress(destinationAddress);
            var result = await CallContractFunction(context, "transfer", new SCVal[] {
                fromAddress,
                toAddress,
                new SCVal.ScvU32 { u32 = new uint32(checked((uint)tokenId)) },
            }, task);
            if (result.IsError)
            {
                return Result<bool>.Err(result);
            }
            if (result.Value.Item3 is not GetTransactionResult getResult)
            {
                return Result<bool>.Err(StatusCode.DESERIALIZATION_ERROR, "InvokeSEP50AssetTransfer failed because the transaction result is not a getTransaction response.");
            }
            if (getResult.Status != GetTransactionResult_Status.SUCCESS)
            {
                return Result<bool>.Err(StatusCode.TRANSACTION_FAILED, "InvokeSEP50AssetTransfer failed because the transaction did not succeed.");
            }
            return Result<bool>.Ok(true);
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

        static string EncodedOwnerLedgerKey(NetworkContext context, int tokenId)
        {
            LedgerKey ledgerKey = MakeLedgerKey(context, "Owner", (uint)tokenId, ContractDataDurability.PERSISTENT);
            return LedgerKeyXdr.EncodeToBase64(ledgerKey);
        }

        static bool TryGetOwnerTokenIdFromContractKey(SCVal key, out int tokenId)
        {
            tokenId = 0;
            if (key is not SCVal.ScvVec ownerKeyVec)
            {
                return false;
            }

            SCVal[] parts = ownerKeyVec.vec.InnerValue;
            if (parts.Length != 2)
            {
                return false;
            }

            if (parts[0] is not SCVal.ScvSymbol ownerSymbol || ownerSymbol.sym.InnerValue != "Owner")
            {
                return false;
            }

            if (parts[1] is not SCVal.ScvU32 tokenIdVal)
            {
                return false;
            }

            tokenId = checked((int)tokenIdVal.u32.InnerValue);
            return true;
        }

        static bool TryParseOwnerLedgerEntry(Entries entry, out int tokenId, out string ownerAddress)
        {
            tokenId = 0;
            ownerAddress = null;

            if (entry.LedgerKey is not LedgerKey.ContractData contractLedgerKey)
            {
                return false;
            }

            if (!TryGetOwnerTokenIdFromContractKey(contractLedgerKey.contractData.key, out tokenId))
            {
                return false;
            }

            if (entry.LedgerEntryData is not LedgerEntry.dataUnion.ContractData contractEntry)
            {
                return false;
            }

            if (contractEntry.contractData.val is not SCVal.ScvAddress addressVal)
            {
                return false;
            }

            ownerAddress = ScAddressToString(addressVal.address);
            return ownerAddress != null;
        }

        static string ScAddressToString(SCAddress address)
        {
            switch (address)
            {
                case SCAddress.ScAddressTypeAccount accountAddress:
                    if (accountAddress.accountId.InnerValue is PublicKey.PublicKeyTypeEd25519 ed25519Key)
                    {
                        return StrKey.EncodeStellarAccountId(ed25519Key.ed25519.InnerValue);
                    }
                    return null;
                case SCAddress.ScAddressTypeContract contractAddress:
                    return StrKey.EncodeContractId(contractAddress.contractId.InnerValue);
                default:
                    return null;
            }
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

        public static Result<SorobanInvocationMeta> GetSorobanMeta(GetTransactionResult getResult)
        {
            if (getResult?.TransactionResultMeta is not TransactionMeta meta)
            {
                return Result<SorobanInvocationMeta>.Err(StatusCode.DESERIALIZATION_ERROR,
                    "GetSorobanMeta failed because transaction result meta is missing.");
            }
            return GetSorobanMeta(meta);
        }

        public static Result<SorobanInvocationMeta> GetSorobanMeta(TransactionMeta meta)
        {
            if (meta == null)
            {
                return Result<SorobanInvocationMeta>.Err(StatusCode.DESERIALIZATION_ERROR,
                    "GetSorobanMeta failed because transaction meta is missing.");
            }

            object sorobanMeta = null;
            if (meta is TransactionMeta.case_3 case3)
            {
                sorobanMeta = case3.v3?.sorobanMeta;
            }
            else if (meta is TransactionMeta.case_4 case4)
            {
                sorobanMeta = case4.v4?.sorobanMeta;
            }

            if (sorobanMeta == null)
            {
                return Result<SorobanInvocationMeta>.Err(StatusCode.DESERIALIZATION_ERROR,
                    $"GetSorobanMeta failed because meta has no Soroban block (discriminator {meta.Discriminator}).");
            }

            return Result<SorobanInvocationMeta>.Ok(new SorobanInvocationMeta(sorobanMeta));
        }

        public static Result<SCVal> GetSorobanReturnValue(SorobanInvocationMeta sorobanMeta)
        {
            SCVal returnValue = sorobanMeta.Meta switch
            {
                SorobanTransactionMeta m => m.returnValue,
                SorobanTransactionMetaV2 m => m.returnValue,
                _ => null,
            };

            if (returnValue == null)
            {
                return Result<SCVal>.Err(StatusCode.DESERIALIZATION_ERROR,
                    "GetSorobanReturnValue failed because the invocation returned no value.");
            }

            return Result<SCVal>.Ok(returnValue);
        }

        public static Result<SorobanFees> GetSorobanFees(SorobanInvocationMeta sorobanMeta)
        {
            if (sorobanMeta.Ext is not SorobanTransactionMetaExt.case_1 extV1 || extV1.v1 == null)
            {
                return Result<SorobanFees>.Err(StatusCode.DESERIALIZATION_ERROR,
                    "GetSorobanFees failed because Soroban meta ext v1 fee breakdown is missing.");
            }

            SorobanTransactionMetaExtV1 feeExt = extV1.v1;
            return Result<SorobanFees>.Ok(new SorobanFees(
                feeExt.totalNonRefundableResourceFeeCharged.InnerValue,
                feeExt.totalRefundableResourceFeeCharged.InnerValue,
                feeExt.rentFeeCharged.InnerValue));
        }

        /// <summary>
        /// Total fee charged for the transaction (stroops), including inclusion and resource fees.
        /// </summary>
        public static Result<long> GetTransactionFeeCharged(TransactionResult transactionResult)
        {
            if (transactionResult == null)
            {
                return Result<long>.Err(StatusCode.DESERIALIZATION_ERROR,
                    "GetTransactionFeeCharged failed because transaction result is missing.");
            }

            return Result<long>.Ok(transactionResult.feeCharged.InnerValue);
        }

        public static Result<int> GetU32ReturnValue(SCVal returnValue)
        {
            if (returnValue is not SCVal.ScvU32 u32Value)
            {
                return Result<int>.Err(StatusCode.DESERIALIZATION_ERROR,
                    $"GetU32ReturnValue expected u32, got {returnValue?.GetType().Name ?? "null"}.");
            }

            return Result<int>.Ok(checked((int)u32Value.u32.InnerValue));
        }
    }
}
