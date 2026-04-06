using UnityEngine;
using System.Collections.Generic;
using StellarSDK;

public class CommunicationDiagram : MonoBehaviour
{
    public GameObject diagramPointPrefab;
    public List<RowContainer> rowContainers;
    StellarClientTask task;
    public int currentRow = 0;

    static readonly Dictionary<string, (Column from, Column to)> stepMapping = new()
    {
        { "GetLedgerEntriesAsync", (Column.Client, Column.Stellar) },
        { "SimulateTransactionAsync", (Column.Client, Column.Stellar) },
        { "SendTransactionAsync", (Column.Client, Column.Stellar) },
        { "GetTransactionAsync", (Column.Client, Column.Stellar) },
        { "GetEventsAsync", (Column.Client, Column.Stellar) },
        { "GetTransactionsAsync", (Column.Client, Column.Stellar) },
        { "GetHealthAsync", (Column.Client, Column.Stellar) },
        { "GetLatestLedgerAsync", (Column.Client, Column.Stellar) },
        { "GetNetworkAsync", (Column.Client, Column.Stellar) },
        { "GetVersionInfoAsync", (Column.Client, Column.Stellar) },
        { "GetFeeStatsAsync", (Column.Client, Column.Stellar) },
        { "CreateAccount", (Column.Client, Column.Stellar) },
        { "SignAndEncodeTransaction", (Column.Client, Column.Wallet) },
    };

    public void ClearDiagram()
    {
        foreach (RowContainer rowContainer in rowContainers)
        {
            rowContainer.ClearRow();
        }
        currentRow = 0;
    }

    void Start()
    {
        gameObject.SetActive(false);
    }

    public void SetTask(StellarClientTask task)
    {
        this.task = task;
        task.OnStepStarted += OnStepStarted;
        task.OnStepEnded += OnStepEnded;
        task.OnBusyChanged += OnBusyChanged;
    }

    void OnBusyChanged(bool busy)
    {
        if (busy)
        {
            ClearDiagram();
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    bool TryAdvanceRow(string step, Column from, Column to)
    {
        if (currentRow >= rowContainers.Count) return false;
        rowContainers[currentRow].SetRow(from, to, step);
        currentRow++;
        return true;
    }

    void OnStepStarted(string step)
    {
        if (!stepMapping.TryGetValue(step, out var mapping)) return;
        TryAdvanceRow(step, mapping.from, mapping.to);
    }

    void OnStepEnded(string step)
    {
        if (!stepMapping.TryGetValue(step, out var mapping)) return;
        TryAdvanceRow(step, mapping.to, mapping.from);
    }
}

public enum Column
{
    Client,
    Wallet,
    Stellar
}