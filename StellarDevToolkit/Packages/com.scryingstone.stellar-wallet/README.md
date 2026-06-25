# Stellar Unity Wallet

[Freighter](https://www.freighter.app/) browser-wallet integration for Unity
**WebGL**. It lets a WebGL build detect the Freighter extension, read the
connected account and network details, and request transaction signatures —
without your game ever handling the user's secret key.

> This package pairs with the [Stellar Unity SDK](https://github.com/Scrying-Stone/stellar-unity-packages/tree/main/com.scryingstone.stellar-sdk)
> but does **not** depend on it. It operates purely on transaction-envelope
> strings, so you can wire it to the SDK (or anything else) at the app layer.

## Requirements

- Unity 6000.0 or newer
- A **WebGL** build target (Freighter is a browser extension; the API returns
  an error outside WebGL player builds)
- [`com.unity.nuget.newtonsoft-json`](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.2/manual/index.html) 3.2.2 (installed automatically)

## Installation

In **Window > Package Manager > + > Add package from git URL**, paste:

```
https://github.com/Scrying-Stone/stellar-unity-packages.git?path=/com.scryingstone.stellar-wallet
```

To pin a version, append a tag, e.g. `#v0.1.0`.

## Usage

`WalletManager` is a static class; no GameObject is required.

```csharp
using StellarWallet;

// Connect (true = testnet)
WalletResult<WalletManager.WalletConnection> connection =
    await WalletManager.ConnectWallet(isTestnet: true);

if (connection.IsOk)
{
    string address = connection.Value.address;
    string passphrase = connection.Value.networkDetails.networkPassphrase;

    // Sign a base64 transaction envelope (XDR) produced elsewhere (e.g. the SDK)
    WalletResult<string> signed =
        await WalletManager.SignTransaction(unsignedEnvelopeXdr, passphrase);

    if (signed.IsOk)
        Debug.Log($"Signed envelope: {signed.Value}");
}
```

### Pairing with the Stellar Unity SDK

The SDK's `NetworkContext` accepts a `unityWalletSigner` delegate. Point it at
`WalletManager.SignTransaction` and set `SigningMethod.UnityWallet` to have the
SDK delegate signing to Freighter instead of signing locally. See the
[sample project](https://github.com/Scrying-Stone/stellar-unity-sample) for a
complete wiring.

## License

MIT. See [LICENSE.md](LICENSE.md).
