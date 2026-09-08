using UnityEngine;
using Stellar;
using Stellar.RPC;
using StellarSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StellarSDK
{
    public static class RpcTests
    {
        public struct Results
        {
            public int Passed;
            public int Failed;
        }

        class Counter
        {
            public int Passed;
            public int Failed;
        }

        public static async Task<Results> RunAllAsync(NetworkContext context, StellarClientTask task = null)
        {
            var c = new Counter();

            Debug.Log("========== RPC TESTS ==========");

            // --- getHealth ---
            {
                Debug.Log("[TEST] getHealth...");
                Result<GetHealthResult> result = await StellarClient.GetHealthAsync(context, task);
                if (result.IsOk && result.Value.Status == "healthy")
                {
                    Debug.Log($"[PASS] getHealth: status={result.Value.Status}, latestLedger={result.Value.LatestLedger}");
                    c.Passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] getHealth: {result.Message}");
                    c.Failed++;
                }
            }

            // --- getLatestLedger ---
            {
                Debug.Log("[TEST] getLatestLedger...");
                Result<GetLatestLedgerResult> result = await StellarClient.GetLatestLedgerAsync(context, task);
                if (result.IsOk && result.Value.Sequence > 0)
                {
                    Debug.Log($"[PASS] getLatestLedger: sequence={result.Value.Sequence}, protocol={result.Value.ProtocolVersion}");
                    c.Passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] getLatestLedger: {result.Message}");
                    c.Failed++;
                }
            }

            // --- getNetwork ---
            {
                Debug.Log("[TEST] getNetwork...");
                Result<GetNetworkResult> result = await StellarClient.GetNetworkAsync(context, task);
                if (result.IsOk && !string.IsNullOrEmpty(result.Value.Passphrase))
                {
                    Debug.Log($"[PASS] getNetwork: passphrase={result.Value.Passphrase}, protocol={result.Value.ProtocolVersion}");
                    c.Passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] getNetwork: {result.Message}");
                    c.Failed++;
                }
            }

            // --- getVersionInfo ---
            {
                Debug.Log("[TEST] getVersionInfo...");
                Result<GetVersionInfoResult> result = await StellarClient.GetVersionInfoAsync(context, task);
                if (result.IsOk && !string.IsNullOrEmpty(result.Value.Version))
                {
                    Debug.Log($"[PASS] getVersionInfo: version={result.Value.Version}, protocol={result.Value.ProtocolVersion}");
                    c.Passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] getVersionInfo: {result.Message}");
                    c.Failed++;
                }
            }

            // --- getFeeStats ---
            {
                Debug.Log("[TEST] getFeeStats...");
                Result<GetFeeStatsResult> result = await StellarClient.GetFeeStatsAsync(context, task);
                if (result.IsOk && result.Value.SorobanInclusionFee != null)
                {
                    Debug.Log($"[PASS] getFeeStats: sorobanP50={result.Value.SorobanInclusionFee.P50}, classicP50={result.Value.InclusionFee.P50}");
                    c.Passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] getFeeStats: {result.Message}");
                    c.Failed++;
                }
            }

            // --- getLedgerEntries (account) ---
            {
                Debug.Log("[TEST] getLedgerEntries (account)...");
                Result<AccountEntry> result = await StellarClient.ReqAccountEntry(context, task);
                if (result.IsOk && result.Value != null)
                {
                    Debug.Log($"[PASS] getLedgerEntries: account found, seqNum={result.Value.seqNum.InnerValue}");
                    c.Passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] getLedgerEntries: {result.Message}");
                    c.Failed++;
                }
            }

            // --- getTransactions ---
            {
                Debug.Log("[TEST] getTransactions...");
                Result<GetLatestLedgerResult> ledgerResult = await StellarClient.GetLatestLedgerAsync(context, task);
                if (ledgerResult.IsOk)
                {
                    long startLedger = ledgerResult.Value.Sequence - 100;
                    Result<GetTransactionsResult> result = await StellarClient.GetTransactionsAsync(context, new GetTransactionsParams
                    {
                        StartLedger = startLedger,
                        Pagination = new Pagination { Limit = 5 },
                    }, task);
                    if (result.IsOk && result.Value.Transactions != null)
                    {
                        Debug.Log($"[PASS] getTransactions: got {result.Value.Transactions.Count} txns from ledger {startLedger}");
                        c.Passed++;
                    }
                    else
                    {
                        Debug.LogError($"[FAIL] getTransactions: {result.Message}");
                        c.Failed++;
                    }
                }
                else
                {
                    Debug.LogError($"[FAIL] getTransactions: couldn't get latest ledger first: {ledgerResult.Message}");
                    c.Failed++;
                }
            }

            // --- getEvents ---
            {
                Debug.Log("[TEST] getEvents...");
                Result<GetLatestLedgerResult> ledgerResult = await StellarClient.GetLatestLedgerAsync(context, task);
                if (ledgerResult.IsOk)
                {
                    long startLedger = ledgerResult.Value.Sequence - 100;
                    Result<GetEventsResult> result = await StellarClient.GetEventsAsync(context, new GetEventsParams
                    {
                        StartLedger = startLedger,
                        Pagination = new Pagination { Limit = 5 },
                    }, task);
                    if (result.IsOk && result.Value.Events != null)
                    {
                        Debug.Log($"[PASS] getEvents: got {result.Value.Events.Count} events from ledger {startLedger}");
                        foreach (Stellar.RPC.Events ev in result.Value.Events)
                        {
                            Debug.Log($"Event: {ev.Type}, {ev.Ledger}, {ev.LedgerClosedAt}, {ev.ContractId}, {ev.Id}, {ev.PagingToken}, {ev.InSuccessfulContractCall}, {ev.Topic}, {ev.Value}, {ev.TxHash}");
                        }
                        c.Passed++;
                    }
                    else
                    {
                        Debug.LogError($"[FAIL] getEvents: {result.Message}");
                        c.Failed++;
                    }
                }
                else
                {
                    Debug.LogError($"[FAIL] getEvents: couldn't get latest ledger first: {ledgerResult.Message}");
                    c.Failed++;
                }
            }

            // --- simulateTransaction ---
            {
                Debug.Log("[TEST] simulateTransaction...");
                SCVal[] args = { new SCVal.ScvString { str = new SCString("World") } };
                Result<(Transaction, SimulateTransactionResult)> result = await StellarClient.SimulateContractFunction(context, "hello", args, false, task);
                if (result.IsOk && result.Value.Item2.Error == null)
                {
                    SimulateTransactionResult sim = result.Value.Item2;
                    SCVal scResult = sim.Results?.Count > 0 ? sim.Results.First().Result : null;
                    Debug.Log($"[PASS] simulateTransaction: minFee={sim.MinResourceFee}, hasResult={scResult != null}");
                    c.Passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] simulateTransaction: {result.Message}");
                    c.Failed++;
                }
            }

            // --- full round-trip: simulate + send + getTransaction ---
            {
                Debug.Log("[TEST] CallContractFunction (simulate → send → getTransaction)...");
                SCVal[] args = { new SCVal.ScvString { str = new SCString("World") } };
                Result<(SimulateTransactionResult, SendTransactionResult, GetTransactionResult)> result = await StellarClient.CallContractFunction(context, "hello", args, task);
                if (result.IsOk)
                {
                    (SimulateTransactionResult sim, SendTransactionResult send, GetTransactionResult get) = result.Value;
                    Debug.Log($"[PASS] CallContractFunction: txHash={send.Hash}, status={get.Status}");
                    c.Passed++;
                }
                else
                {
                    Debug.LogError($"[FAIL] CallContractFunction: {result.Message}");
                    c.Failed++;
                }
            }

            Debug.Log("========== SERIALIZATION TESTS ==========");
            await RunSerializationTests(context, c, task);

            Debug.Log("========== GAME LOG TESTS ==========");
            await RunGameLogTests(context, c, task);

            return new Results { Passed = c.Passed, Failed = c.Failed };
        }

        static async Task RunSerializationTests(NetworkContext context, Counter c, StellarClientTask task)
        {
            // --- Primitive round-trips ---

            Check("echo_u32(42)", Eq<uint>(await Sim(context, "echo_u32", task, U32(42)), 42), c);
            Check("echo_i32(-7)", Eq<int>(await Sim(context, "echo_i32", task, I32(-7)), -7), c);
            Check("echo_u64(1234567890123)", Eq<ulong>(await Sim(context, "echo_u64", task, U64(1234567890123)), 1234567890123), c);
            Check("echo_bool(true)", Eq<bool>(await Sim(context, "echo_bool", task, Bool(true)), true), c);
            Check("echo_bool(false)", Eq<bool>(await Sim(context, "echo_bool", task, Bool(false)), false), c);
            Check("echo_string(\"stellar\")", Eq<string>(await Sim(context, "echo_string", task, Str("stellar")), "stellar"), c);
            Check("echo_bytes", XdrEq(await Sim(context, "echo_bytes", task, SBytes(0xDE, 0xAD, 0xBE, 0xEF)), SBytes(0xDE, 0xAD, 0xBE, 0xEF)), c);

            // --- Arithmetic ---

            Check("add_u32(10,20)=30", Eq<uint>(await Sim(context, "add_u32", task, U32(10), U32(20)), 30), c);
            Check("add_i32(-10,20)=10", Eq<int>(await Sim(context, "add_i32", task, I32(-10), I32(20)), 10), c);
            Check("negate_i32(42)=-42", Eq<int>(await Sim(context, "negate_i32", task, I32(42)), -42), c);

            // --- Vec ---

            SCVal vecInput = Vec(U32(1), U32(2), U32(3));
            Check("echo_vec([1,2,3])", XdrEq(await Sim(context, "echo_vec", task, vecInput), vecInput), c);
            Check("sum_vec([10,20,30])=60", Eq<uint>(await Sim(context, "sum_vec", task, Vec(U32(10), U32(20), U32(30))), 60), c);
            Check("make_vec(7,8,9)", XdrEq(await Sim(context, "make_vec", task, U32(7), U32(8), U32(9)), Vec(U32(7), U32(8), U32(9))), c);

            // --- Map ---

            SCVal mapInput = SMap(
                Entry(Str("hp"), U32(100)),
                Entry(Str("mp"), U32(50))
            );
            Check("echo_map({hp:100,mp:50})", XdrEq(await Sim(context, "echo_map", task, mapInput), mapInput), c);
            Check("map_get(gold)=999", Eq<uint>(await Sim(context, "map_get", task, SMap(Entry(Str("gold"), U32(999))), Str("gold")), 999), c);
            Check("map_get(missing)=0", Eq<uint>(await Sim(context, "map_get", task, SMap(Entry(Str("gold"), U32(999))), Str("silver")), 0), c);

            SCVal makeMapExpected = SMap(Entry(Str("a"), U32(1)), Entry(Str("b"), U32(2)));
            Check("make_map", XdrEq(await Sim(context, "make_map", task, Vec(Str("a"), Str("b")), Vec(U32(1), U32(2))), makeMapExpected), c);

            // --- Struct (Inventory, sorted alphabetically: items, quantities) ---

            SCVal invItems = Vec(Str("shield"), Str("sword"));
            SCVal invQuantities = SMap(Entry(Str("shield"), U32(2)), Entry(Str("sword"), U32(1)));
            SCVal inventory = SMap(
                Entry(Sym("items"), invItems),
                Entry(Sym("quantities"), invQuantities)
            );
            Check("echo_inventory", XdrEq(await Sim(context, "echo_inventory", task, inventory), inventory), c);

            SCVal makeInvExpected = SMap(
                Entry(Sym("items"), Vec(Str("potion"), Str("scroll"))),
                Entry(Sym("quantities"), SMap(Entry(Str("potion"), U32(1)), Entry(Str("scroll"), U32(2))))
            );
            Check("make_inventory", XdrEq(await Sim(context, "make_inventory", task, Vec(Str("potion"), Str("scroll"))), makeInvExpected), c);

            // --- Bytes ---

            Check("bytes_len(5)", Eq<uint>(await Sim(context, "bytes_len", task, SBytes(1, 2, 3, 4, 5)), 5), c);
            Check("concat_bytes", XdrEq(await Sim(context, "concat_bytes", task, SBytes(1, 2), SBytes(3, 4)), SBytes(1, 2, 3, 4)), c);

            // --- Edge cases ---

            Check("echo_u32_zero", Eq<uint>(await Sim(context, "echo_u32_zero", task), 0), c);
            Check("echo_empty_vec", XdrEq(await Sim(context, "echo_empty_vec", task), Vec()), c);
            Check("echo_empty_string", Eq<string>(await Sim(context, "echo_empty_string", task), ""), c);
            Check("echo_true", Eq<bool>(await Sim(context, "echo_true", task), true), c);
            Check("echo_false", Eq<bool>(await Sim(context, "echo_false", task), false), c);
            Check("echo_max_u32", Eq<uint>(await Sim(context, "echo_max_u32", task), uint.MaxValue), c);
            Check("echo_min_i32", Eq<int>(await Sim(context, "echo_min_i32", task), int.MinValue), c);
            Check("echo_max_u64", Eq<ulong>(await Sim(context, "echo_max_u64", task), ulong.MaxValue), c);
        }

        // Field names must match the contract's GameLog symbol keys so
        // SCValToNative can map them by reflection.
#pragma warning disable 0649 // assigned by reflection in SCValToNative
        struct PackedMovesEntry
        {
            public ulong[] packed;
        }

        struct GameLogEntry
        {
            public uint final_score;
            public PackedMovesEntry packed_moves;
            public ulong seed;
            public ulong submitted_ledger_seq;
        }
#pragma warning restore 0649

        static async Task RunGameLogTests(NetworkContext context, Counter c, StellarClientTask task)
        {
            // Testnet state survives between runs and the contract rejects a seed
            // it already stored, so each run needs a seed it has never seen.
            ulong seed = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            const uint FinalScore = 4321;
            SCVal player = StellarClient.AccountStringToScvAddress(context.userAccount.AccountId);
            SCVal[] submitArgs = { player, GameLogVal(FinalScore, seed) };

            // --- register_game_log (a write, so it needs a real signed tx) ---

            Debug.Log($"[TEST] register_game_log(seed={seed})...");
            Result<(SimulateTransactionResult, SendTransactionResult, GetTransactionResult)> submit =
                await StellarClient.CallContractFunction(context, "register_game_log", submitArgs, task);
            if (submit.IsError)
            {
                Debug.LogError($"[FAIL] register_game_log: {submit.Message}");
                c.Failed++;
                return;
            }

            Debug.Log($"[PASS] register_game_log: txHash={submit.Value.Item2.Hash}, status={submit.Value.Item3.Status}");
            c.Passed++;

            // --- get_game_logs returns what was just written ---

            GameLogEntry[] logs = await GetGameLogs(context, task, player);
            int index = IndexOfSeed(logs, seed);
            Check($"get_game_logs contains seed={seed}", index >= 0, c);
            if (index >= 0)
            {
                Check("final_score round-trips", logs[index].final_score == FinalScore, c);
                // The client sent 0; a non-zero value means the contract stamped it.
                Check("submitted_ledger_seq stamped by contract", logs[index].submitted_ledger_seq > 0, c);
            }

            // --- a repeated seed is rejected, even with a different score ---

            SCVal[] duplicateArgs = { player, GameLogVal(FinalScore + 1, seed) };
            Result<(SimulateTransactionResult, SendTransactionResult, GetTransactionResult)> duplicate =
                await StellarClient.CallContractFunction(context, "register_game_log", duplicateArgs, task);
            Check("register_game_log rejects duplicate seed", duplicate.IsError, c);

            // --- an address that never submitted has no logs ---

            SCVal stranger = StellarClient.AccountStringToScvAddress(MuxedAccount.Random().AccountId);
            GameLogEntry[] strangerLogs = await GetGameLogs(context, task, stranger);
            Check("get_game_logs is empty for an unknown address", strangerLogs != null && strangerLogs.Length == 0, c);
        }

        static async Task<GameLogEntry[]> GetGameLogs(NetworkContext context, StellarClientTask task, SCVal address)
        {
            SCVal result = await Sim(context, "get_game_logs", task, address);
            if (result == null)
            {
                return null;
            }

            try
            {
                return SCUtility.SCValToNative<GameLogEntry[]>(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"  Failed decoding get_game_logs result: {ex.Message}");
                return null;
            }
        }

        static int IndexOfSeed(GameLogEntry[] logs, ulong seed)
        {
            if (logs == null)
            {
                return -1;
            }

            for (int i = 0; i < logs.Length; i++)
            {
                if (logs[i].seed == seed)
                {
                    return i;
                }
            }

            return -1;
        }

        static async Task<SCVal> Sim(NetworkContext context, string fn, StellarClientTask task, params SCVal[] args)
        {
            var result = await StellarClient.SimulateContractFunction(context, fn, args, false, task);
            if (result.IsOk && result.Value.Item2.Error == null)
                return result.Value.Item2.Results?.FirstOrDefault()?.Result;
            Debug.LogError($"  Simulation failed for '{fn}': {result.Message}");
            return null;
        }

        static void Check(string name, bool ok, Counter c)
        {
            if (ok) { Debug.Log($"[PASS] {name}"); c.Passed++; }
            else { Debug.LogError($"[FAIL] {name}"); c.Failed++; }
        }

        static bool Eq<T>(SCVal v, T expected)
        {
            if (v == null) return false;
            try { return EqualityComparer<T>.Default.Equals(SCUtility.SCValToNative<T>(v), expected); }
            catch { return false; }
        }

        static bool XdrEq(SCVal a, SCVal b)
        {
            if (a == null || b == null) return false;
            return SCUtility.HashEqual(a, b);
        }

        static SCVal U32(uint v) => new SCVal.ScvU32 { u32 = new uint32(v) };
        static SCVal I32(int v) => new SCVal.ScvI32 { i32 = new int32(v) };
        static SCVal U64(ulong v) => new SCVal.ScvU64 { u64 = new uint64(v) };
        static SCVal Bool(bool v) => new SCVal.ScvBool { b = v };
        static SCVal Str(string v) => new SCVal.ScvString { str = new SCString(v) };
        static SCVal Sym(string v) => new SCVal.ScvSymbol { sym = new SCSymbol(v) };
        static SCVal SBytes(params byte[] v) => new SCVal.ScvBytes { bytes = new SCBytes(v) };
        static SCVal Vec(params SCVal[] v) => new SCVal.ScvVec { vec = new SCVec(v) };
        static SCVal SMap(params SCMapEntry[] entries) => new SCVal.ScvMap { map = new SCMap(entries) };
        static SCMapEntry Entry(SCVal key, SCVal val) => new SCMapEntry { key = key, val = val };

        // GameLog, sorted alphabetically: final_score, packed_moves, seed, submitted_ledger_seq.
        // submitted_ledger_seq is whatever the contract stamps, so send 0.
        static SCVal GameLogVal(uint finalScore, ulong seed) =>
            SMap(
                Entry(Sym("final_score"), U32(finalScore)),
                Entry(Sym("packed_moves"), SMap(Entry(Sym("packed"), Vec()))),
                Entry(Sym("seed"), U64(seed)),
                Entry(Sym("submitted_ledger_seq"), U64(0))
            );
    }
}
