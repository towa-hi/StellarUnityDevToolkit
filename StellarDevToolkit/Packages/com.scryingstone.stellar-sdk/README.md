# Stellar Unity SDK

A Unity SDK for the [Stellar](https://stellar.org) network and
[Soroban](https://soroban.stellar.org) smart contracts. It provides XDR
serialization, a JSON-RPC client for Soroban RPC, transaction building, and
contract invocation — all from C# inside Unity, including WebGL/IL2CPP builds.

> This package is the XDR + RPC core. Browser wallet integration (Freighter)
> lives in a separate package, `com.scryingstone.stellar-wallet`, and is not
> required to use the SDK.

## Requirements

- Unity 6000.0 or newer
- [`com.unity.nuget.newtonsoft-json`](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html) 3.2.2 (installed automatically as a dependency)

## Installation

### Unity Package Manager (git URL)

In **Window > Package Manager > + > Add package from git URL**, paste:

```
https://github.com/Scrying-Stone/stellar-unity-packages.git?path=/com.scryingstone.stellar-sdk
```

To pin a version, append a tag, e.g. `#v0.1.0`.

### Unity Asset Store

Import "Stellar Unity SDK" from your Asset Store **My Assets**, then add it via
the Package Manager.

## Quick start

```csharp
using Stellar;
using StellarSDK;
using UnityEngine;

public class StellarExample : MonoBehaviour
{
    async void Start()
    {
        // A keypair (import a secret seed, or MuxedAccount.Random() for a new one)
        MuxedAccount account = MuxedAccount.Random();

        var context = new NetworkContext(
            inOnline: true,
            inSigningMethod: NetworkContext.SigningMethod.PrivateKey,
            inUserAccount: account,
            inIsTestnet: true,
            inServerUri: "https://soroban-testnet.stellar.org",
            inContractAddress: null,
            inAssetIssuerAddress: null,
            inAssetCode: null,
            inPollRateMs: 1000,
            inMaxAttempts: 30
        );

        Result<GetLatestLedgerResult> result =
            await StellarClient.GetLatestLedgerAsync(context);

        if (result.IsOk)
            Debug.Log($"Latest ledger: {result.Value.Sequence}");
        else
            Debug.LogError(result.Message);
    }
}
```

See the [sample project](https://github.com/Scrying-Stone/stellar-unity-sample)
for a complete, runnable example (RPC calls, serialization round-trips, and a
demo game) and the optional Freighter wallet integration.

## Features

- XDR serialization/deserialization for Stellar and Soroban types
- Soroban JSON-RPC client (health, ledgers, network, fees, ledger entries,
  transactions, events, simulate/send/get transaction)
- Transaction building and contract function invocation
- Ed25519 keypair handling and local transaction signing (`MuxedAccount`)
- WebGL/IL2CPP support (ships its own `link.xml` stripping rules)

## License

MIT. See [LICENSE.md](LICENSE.md). This package bundles third-party components
under their own licenses; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
