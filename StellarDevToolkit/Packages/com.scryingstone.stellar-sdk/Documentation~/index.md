# Stellar Development Toolkit for Unity — Documentation

The Stellar Development Toolkit for Unity lets you talk to the Stellar network and Soroban smart
contracts from C# in Unity: XDR serialization, a Soroban JSON-RPC client,
transaction building, contract invocation, and Ed25519 signing.

## Installation

Add via UPM (Window > Package Manager > Add package from git URL):

```
https://github.com/Scrying-Stone/stellar-unity-packages.git?path=/com.scryingstone.stellar-sdk
```

Or import from the Unity Asset Store. The Newtonsoft.Json dependency
(`com.unity.nuget.newtonsoft-json`) is resolved automatically.

## Core concepts

- `NetworkContext` — a serializable struct describing the connection: RPC
  server URI, testnet flag, the active `MuxedAccount`, signing method, and
  polling settings. Passed to every `StellarClient` call.
- `StellarClient` — static entry point for all RPC operations
  (`GetHealthAsync`, `GetLatestLedgerAsync`, `GetNetworkAsync`,
  `GetFeeStatsAsync`, `SimulateContractFunction`, `CallContractFunction`, etc.).
  Calls return `Result<T>` (check `IsOk` / `Message`).
- `MuxedAccount` — Ed25519 keypair: create with `Random()`, import via
  `FromSecretSeed(...)` / `FromBIP39Seed(...)`, and `Sign(...)` data. Local
  signing is opt-in via `NetworkContext.SigningMethod.PrivateKey`.
- `SCVal` + `SCUtility` — Soroban host value types and helpers for converting
  between native C# values and XDR `SCVal`.

## Signing

Two signing paths are supported:

- `SigningMethod.PrivateKey` — the toolkit signs locally with the account's
  secret.
- `SigningMethod.UnityWallet` — signing is delegated to an external signer.

## WebGL / IL2CPP

The package ships a `link.xml` that preserves the RPC DTO types and
Newtonsoft.Json members that managed-code stripping would otherwise remove.
No extra configuration is required.

## Sample

A complete, runnable example (RPC calls, XDR round-trip tests, and a demo game)
is in the sample project:
https://github.com/Scrying-Stone/stellar-unity-sample
