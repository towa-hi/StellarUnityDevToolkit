# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-06-25

### Added
- Initial release of the Stellar Unity SDK.
- XDR serialization and deserialization for Stellar and Soroban types.
- JSON-RPC client for Soroban RPC (getHealth, getLatestLedger, getNetwork,
  getVersionInfo, getFeeStats, getLedgerEntries, getTransactions, getEvents,
  simulateTransaction, sendTransaction, getTransaction).
- Transaction building and contract invocation helpers.
- Ed25519 keypair handling and transaction signing (`MuxedAccount`).
- WebGL/IL2CPP support via bundled `link.xml` stripping rules.
