# StellarUnityDevToolkit

**StellarUnityDevToolkit** is a set of tools that allows Unity developers to interface with the Stellar smart contract platform (formerly known as Soroban) through RPC. The toolkit is intended primarily for Unity video game applications, so some asset management functionality is currently out of scope. However, the toolset does support the use and authentication of tokens and NFT assets on the Stellar network. WebGL builds include a browser wallet bridge for Freighter that handles transaction signing and account management.

Distributed as a Unity Package Manager package (`com.scryingstone.stellar-sdk`), targeting Unity 2022.3+.

## Features

- **Soroban RPC client** -- async methods for all standard Stellar RPC endpoints (`getHealth`, `getLatestLedger`, `getNetwork`, `getVersionInfo`, `getFeeStats`, `getLedgerEntries`, `getTransactions`, `getEvents`, `simulateTransaction`, `sendTransaction`, `getTransaction`)
- **Smart contract invocation** -- `CallContractFunction` handles the full simulate-sign-send-poll flow; `SimulateContractFunction` performs a dry run without submitting
- **Full XDR type coverage** -- ~400 generated C# types matching the Stellar XDR schema, plus `XdrReader`/`XdrWriter` for binary serialization
- **SCVal serialization** -- `SCUtility` converts between C# types (primitives, arrays, dictionaries, structs, `Vector2Int`, enums) and Soroban `SCVal`; custom converters can be added via `Register<T>()`
- **WebGL wallet bridge** -- `WalletManager` communicates with the Freighter browser extension through a `.jslib` plugin for wallet detection, address retrieval, network details, and transaction signing
- **Desktop key signing** -- outside of WebGL, transactions are signed directly with the `MuxedAccount` keypair
- **Token and asset queries** -- `GetAssets` reads trustline entries for Stellar assets
- **Testnet account creation** -- `CreateAccount` funds a new keypair through Stellar Friendbot
- **Structured error handling** -- `Result<T>` carries one of 19 `StatusCode` values covering network, RPC, wallet, simulation, and transaction errors
- **Async task observability** -- `StellarClientTask` tracks nested async steps and fires events (`OnStepStarted`, `OnStepEnded`, `OnBusyChanged`), useful for loading UIs

## Key Classes

### `NetworkContext`

Serializable struct holding connection and account configuration: RPC server URI, user account (`MuxedAccount`), contract address, asset issuer/code, testnet flag, and polling settings (`pollRateMs`, `maxAttempts`). Passed as the first argument to every `StellarClient` method.

### `StellarClient`

Static class and the primary API surface. Public methods:

- `CallContractFunction` -- full contract call flow (simulate, sign, send, poll)
- `SimulateContractFunction` -- dry-run a contract invocation
- `ReqAccountEntry` -- fetch the caller's account entry from the ledger
- `GetAssets` -- query trustline entries
- `GetHealthAsync`, `GetLatestLedgerAsync`, `GetNetworkAsync`, `GetVersionInfoAsync`, `GetFeeStatsAsync`, `GetLedgerEntriesAsync`, `GetTransactionsAsync`, `GetEventsAsync` -- individual RPC endpoints
- `CreateAccount` -- testnet Friendbot funding
- `MakeLedgerKey` -- build a `LedgerKey` for contract data lookups

All methods use `UnityWebRequest`, accept a `NetworkContext`, and return `Result<T>`.

### `WalletManager`

MonoBehaviour singleton that connects Unity WebGL builds to the Freighter browser wallet via `DllImport("__Internal")` calls into a `.jslib` plugin.

- `ConnectWallet` -- detects wallet, retrieves address and network details
- `SignTransaction` -- requests Freighter to sign a transaction envelope
- `DisconnectWallet` -- clears cached address and network state
- `IsWalletBusy` / `OnWalletBusyChanged` -- tracks in-flight wallet operations

On non-WebGL platforms, `StellarClient` bypasses `WalletManager` and signs with the account keypair directly.

### `SCUtility`

Static class for converting between C# types and Soroban `SCVal`.

- `NativeToSCVal(object)` -- C# to `SCVal` (supports `uint`, `int`, `ulong`, `string`, `bool`, `byte[]`, `Vector2Int`, enums, arrays, dictionaries, and `IScvMapCompatable` types)
- `SCValToNative<T>(SCVal)` -- `SCVal` to typed C# value (same type coverage, plus nullable unwrapping and struct reflection)
- `Register<T>(toScVal, fromScVal)` -- add custom converters for additional types
- `FieldToSCMapEntry` -- convert a named field to an `SCMapEntry` (handles nullable wrapping)
- `HashEqual(SCVal, SCVal)` -- compare two `SCVal` instances by XDR encoding

### `RpcTests`

Runtime test suite invoked via `RunAllAsync(NetworkContext, StellarClientTask)`. Covers:

- **RPC endpoint tests** -- `getHealth`, `getLatestLedger`, `getNetwork`, `getVersionInfo`, `getFeeStats`, `getLedgerEntries`, `getTransactions`, `getEvents`, `simulateTransaction`, and a full `CallContractFunction` round-trip
- **Serialization round-trip tests** -- primitives (`u32`, `i32`, `u64`, `bool`, `string`, `bytes`), arithmetic, vecs, maps, structs (`Player`), nested structs (`Inventory`), multi-return values, and edge cases (zero, empty, min/max)

Returns a `Results` struct with pass/fail counts. Logs `[PASS]`/`[FAIL]` per test via `Debug.Log`.
