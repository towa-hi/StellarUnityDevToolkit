using System;
using System.Text;
using System.Threading.Tasks;
using Stellar.RPC;
using StellarSDK;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal test gauntlet for the Stellar SDK package. Runs only read-only RPC
/// calls that need no account, signing, or wallet, and reports pass/fail per
/// endpoint to the UI and the console.
/// </summary>
public class StellarDemo : MonoBehaviour
{
    public Button runTestsButton;
    public Text resultsText;

    [Tooltip("Stellar RPC endpoint the tests run against.")]
    public string serverUri = "https://soroban-testnet.stellar.org";

    readonly StringBuilder output = new StringBuilder();
    int passed;
    int failed;
    bool running;

    void Start()
    {
        if (runTestsButton != null)
        {
            runTestsButton.onClick.AddListener(RunTests);
        }

        SetText("Press Run Tests to check the RPC endpoints.");
    }

    void OnDestroy()
    {
        if (runTestsButton != null)
        {
            runTestsButton.onClick.RemoveListener(RunTests);
        }
    }

    public void RunTests()
    {
        if (running)
        {
            return;
        }

        _ = RunTestsAsync();
    }

    async Task RunTestsAsync()
    {
        running = true;
        if (runTestsButton != null)
        {
            runTestsButton.interactable = false;
        }

        passed = 0;
        failed = 0;
        output.Clear();
        AppendLine($"RPC tests: {serverUri}");
        Debug.Log($"========== RPC TESTS ({serverUri}) ==========");

        try
        {
            // No account, contract, or signer: these endpoints are all public.
            NetworkContext context = new NetworkContext(
                true, NetworkContext.SigningMethod.PrivateKey, null, true,
                serverUri, null, null, null, 1000, 30);

            long latestLedger = 0;

            {
                Result<GetHealthResult> result = await StellarClient.GetHealthAsync(context);
                bool ok = result.IsOk && result.Value.Status == "healthy";
                Report("getHealth", ok, ok
                    ? $"status={result.Value.Status}"
                    : result.Message);
            }

            {
                Result<GetLatestLedgerResult> result = await StellarClient.GetLatestLedgerAsync(context);
                bool ok = result.IsOk && result.Value.Sequence > 0;
                if (ok)
                {
                    latestLedger = result.Value.Sequence;
                }
                Report("getLatestLedger", ok, ok
                    ? $"sequence={result.Value.Sequence}, protocol={result.Value.ProtocolVersion}"
                    : result.Message);
            }

            {
                Result<GetNetworkResult> result = await StellarClient.GetNetworkAsync(context);
                bool ok = result.IsOk && !string.IsNullOrEmpty(result.Value.Passphrase);
                Report("getNetwork", ok, ok
                    ? $"passphrase={result.Value.Passphrase}"
                    : result.Message);
            }

            {
                Result<GetVersionInfoResult> result = await StellarClient.GetVersionInfoAsync(context);
                bool ok = result.IsOk && !string.IsNullOrEmpty(result.Value.Version);
                Report("getVersionInfo", ok, ok
                    ? $"version={result.Value.Version}"
                    : result.Message);
            }

            {
                Result<GetFeeStatsResult> result = await StellarClient.GetFeeStatsAsync(context);
                bool ok = result.IsOk && result.Value.SorobanInclusionFee != null;
                Report("getFeeStats", ok, ok
                    ? $"sorobanP50={result.Value.SorobanInclusionFee.P50}, classicP50={result.Value.InclusionFee.P50}"
                    : result.Message);
            }

            if (latestLedger > 0)
            {
                long startLedger = Math.Max(1, latestLedger - 100);

                {
                    Result<GetLedgersResult> result = await StellarClient.GetLedgersAsync(context, new GetLedgersParams
                    {
                        StartLedger = startLedger,
                        Pagination = new Pagination { Limit = 5 },
                    });
                    bool ok = result.IsOk && result.Value.Ledgers != null;
                    Report("getLedgers", ok, ok
                        ? $"got {result.Value.Ledgers.Count} ledgers from {startLedger}"
                        : result.Message);
                }

                {
                    Result<GetTransactionsResult> result = await StellarClient.GetTransactionsAsync(context, new GetTransactionsParams
                    {
                        StartLedger = startLedger,
                        Pagination = new Pagination { Limit = 5 },
                    });
                    bool ok = result.IsOk && result.Value.Transactions != null;
                    Report("getTransactions", ok, ok
                        ? $"got {result.Value.Transactions.Count} transactions from {startLedger}"
                        : result.Message);
                }

                {
                    Result<GetEventsResult> result = await StellarClient.GetEventsAsync(context, new GetEventsParams
                    {
                        StartLedger = startLedger,
                        Pagination = new Pagination { Limit = 5 },
                    });
                    bool ok = result.IsOk && result.Value.Events != null;
                    Report("getEvents", ok, ok
                        ? $"got {result.Value.Events.Count} events from {startLedger}"
                        : result.Message);
                }
            }
            else
            {
                Report("getLedgers", false, "skipped: no latest ledger");
                Report("getTransactions", false, "skipped: no latest ledger");
                Report("getEvents", false, "skipped: no latest ledger");
            }

            string summary = $"Done: {passed} passed, {failed} failed.";
            AppendLine(summary);
            Debug.Log($"========== {summary} ==========");
        }
        catch (Exception exception)
        {
            AppendLine($"Tests aborted: {exception.Message}");
            Debug.LogError($"StellarDemo tests aborted: {exception}");
        }
        finally
        {
            running = false;
            if (runTestsButton != null)
            {
                runTestsButton.interactable = true;
            }
        }
    }

    void Report(string name, bool ok, string detail)
    {
        if (ok)
        {
            passed++;
            Debug.Log($"[PASS] {name}: {detail}");
        }
        else
        {
            failed++;
            Debug.LogError($"[FAIL] {name}: {detail}");
        }

        AppendLine($"{(ok ? "PASS" : "FAIL")}  {name}: {detail}");
    }

    void AppendLine(string line)
    {
        output.AppendLine(line);
        SetText(output.ToString());
    }

    void SetText(string text)
    {
        if (resultsText != null)
        {
            resultsText.text = text;
        }
    }
}
