# Solution Architecture Overview

## The Trustless Private Cloud Ecosystem

This solution implements a **complete, end-to-end trustless private cloud system** — from low-level cryptography and transport to a Blazor web UI — organized as a layered set of independently reusable .NET libraries.

## Dependency Graph

```
Cloud (Blazor Server web app)
	│
	├── CloudClient  (cloud client logic + OS tray icon)
	│       └── CloudBox  (symmetric cloud instance manager)
	│               └── CloudSync  (file sync engine)
	│                       └── EncryptedMessaging  (identity + AES-256 messaging)
	│                               └── CommunicationChannel  (TCP transport)
	│                                       └── FullDuplexStreamSupport  (stream mux)
	│
	├── UISupportBlazor  (auto-generated Blazor front-end)
	│       └── UISupportGeneric  (assembly analyzer / UI model)
	│
	├── AntiGitLibrary  (orchestration: mirroring + backup)
	│       ├── DataRedundancy  (real-time mirroring + merge)
	│       └── BackupLibrary   (hard-link-based backup + versioning)
	│
	├── SecureStorage  (AES-256 encrypted key/value + object store)
	├── SystemExtra    (cross-platform OS utilities)
	└── AppSync        (automatic application updates)
```

## Layer Descriptions

### Application Layer
- **Cloud**: Blazor Server web UI. Provides browser-based management panels (sync, backup, security, diagnostics, digital signature, QR pairing, …).
- **CloudClient**: embeds the cloud client logic and OS tray integration.

### Cloud Layer
- **CloudBox**: symmetric client/server cloud instance manager; Bitcoin-based identity; dual transport (TCP binary + encrypted REST).
- **CloudSync**: high-performance binary-protocol file synchronization; hash-based diff; spooler with auto-retry; role management; 2FA.

### Messaging & Transport Layer
- **EncryptedMessaging**: per-message ephemeral AES-256-GCM encryption; ECDSA-521 digital signatures; group multicast; no server-side account.
- **CommunicationChannel**: transport-agnostic reliable channel; TCP default; keep-alive; auto-reconnect; anti-duplicate.
- **FullDuplexStreamSupport**: stream multiplexer — multiple logical channels over one physical stream.

### UI Automation Layer
- **UISupportBlazor**: Blazor components + `ApiMiddleware`; deterministic front-end generation from back-end assemblies.
- **UISupportGeneric**: platform-neutral assembly reflector; produces the UI model consumed by any renderer.

### Data & OS Layer
- **SecureStorage**: AES-256 encrypted persistent storage (values, objects, blobs); hardware HSM support.
- **SystemExtra**: cross-platform OS utilities (service install, user management, disk info, kiosk mode).
- **AppSync**: delta-based automatic application updates.

### Anti-Git Layer
- **AntiGitLibrary**: orchestration bridge between the UI and the two underlying subsystems.
- **DataRedundancy**: real-time file mirroring and intelligent multi-user merge over a shared network path.
- **BackupLibrary**: hard-link-based daily backup and per-file versioning.

## Security Model (Global)

All projects share a common security philosophy:

1. **Trustless**: security is guaranteed by mathematics, not by trusting a third party.
2. **Bitcoin-derived cryptography**: BIP39 passphrase → ECDSA key-pair is used throughout for identity and key derivation.
3. **Client-side-only encryption**: the server never receives or stores plaintext.
4. **Open source**: all code is publicly auditable; the published binaries match the source.
5. **Zero accounts**: identity = key-pair. No password databases, no registration servers.

## Target Frameworks

| Project | Framework |
|---|---|
| Cloud | .NET 9 |
| CloudClient | .NET 9 |
| CloudBox, CloudSync | .NET Standard 2.1 |
| EncryptedMessaging, CommunicationChannel | .NET Standard 2.1 |
| FullDuplexStreamSupport | .NET Standard 2.1 |
| UISupportBlazor, UISupportGeneric | .NET Standard 2.1 |
| SecureStorage, SystemExtra, AppSync | .NET Standard 2.1 |
| AntiGitLibrary, DataRedundancy, BackupLibrary | .NET Standard 2.1 |
