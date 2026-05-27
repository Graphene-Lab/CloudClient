# CloudClient – System Overview

## What is CloudClient?

**CloudClient** is a .NET class library that implements the cloud client logic for the trustless private cloud system. It extends `CloudBox.CloudBox` with:

- OS-specific features: system tray icon on Windows and macOS with real-time sync status.
- QR code detection for initial server pairing.
- Server command handling and local sync status callbacks.

The library is consumed by the `Cloud` Blazor application, which provides the web UI shell, but it can be embedded in any .NET host (console, desktop, service).

## Architecture

```
CloudClient.Client
	│
	├── extends CloudBox.CloudBox
	│        └── uses CloudSync (sync engine)
	│                 └── uses EncryptedMessaging + CommunicationChannel (transport)
	│
	├── WindowsSupport  – Windows system tray icon
	├── MacOSSupport    – macOS status-bar icon
	└── QrCodeDetector  – camera-based QR pairing
```

## Sync Status Icons

The tray icon transitions between four states:

| State | Meaning |
|---|---|
| `Default` | Idle / not connected |
| `Synchronize` | Sync in progress |
| `Synchronized` | All files up-to-date |
| `Warning` | Error or pending attention |

## Platform Support

| Platform | System Tray | QR Detection |
|---|---|---|
| Windows | ✅ | ✅ |
| macOS | ✅ | ✅ |
| Linux | ❌ (headless) | ❌ |

## Dependencies

- `CloudBox` / `CloudSync` – core sync stack
- `EncryptedMessaging` – encrypted, signed messaging
- `NBitcoin` – BIP39 passphrase / key derivation
- `SecureStorage` – secure local key storage
