# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-06-25

### Added
- Initial release of the Stellar Unity Wallet package.
- Freighter browser-wallet integration for Unity WebGL via a `.jslib` bridge.
- `WalletManager` API: detect the wallet, connect and fetch the address and
  network details, and sign transaction envelopes.
- Async results via `WalletResult<T>` with explicit `WalletStatusCode` values,
  including user-cancellation handling.
