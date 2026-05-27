# Cloud – System Overview

## What is Cloud?

**Cloud** is a Blazor Server web application that acts as the graphical front-end for the trustless private cloud ecosystem. It provides a browser-based management console that runs locally on the user's machine and communicates exclusively with the user's own private cloud server, never relying on any third-party service.

The application is designed around a **military-grade security** model derived from Bitcoin cryptography: every account is represented by a cryptographic key-pair generated from a BIP39 passphrase. No user registration is required and no credentials are stored on any external server.

## Key Features

| Feature | Description |
|---|---|
| **File synchronization** | Real-time, encrypted sync between local storage and a private cloud server via the CloudSync/CloudBox stack |
| **Antivirus scanning** | Files are scanned for malware at sync time |
| **Daily backup + versioning** | Intelligent hard-link-based backup that stores only deltas; versioning triggers on every file modification |
| **Virtual disk** | Optional virtual disk support for transparent cloud access |
| **Digital signature** | Document signing and verification using the user's private key |
| **QR-code login** | Scan the server QR code to establish the encrypted pairing |
| **PWA support** | Installable as a Progressive Web App |
| **Multi-theme UI** | Compact, Dark, Modern, and Professional themes |
| **Multi-instance** | Multiple cloud instances can run on the same machine on different ports |

## Architecture at a Glance

```
Browser  ──HTTP/WS──►  Blazor Server (Cloud project)
							  │
					┌─────────┼──────────┐
					▼         ▼          ▼
			   CloudBox   BackupMgr   ApiMiddleware
					│
			   CloudSync ──encrypted TCP──► Cloud Server
					│
		  EncryptedMessaging / CommunicationChannel
```

The Blazor application hosts the `CloudClient.Client` object, which extends `CloudBox.CloudBox`. All file I/O and sync logic live in the underlying library stack; the Blazor layer only renders state.

## Technology Stack

- **.NET 9** / Blazor Server
- **Bootstrap 5** (CSS themes in `wwwroot/themes/`)
- **CloudBox / CloudSync** – sync and cloud management
- **EncryptedMessaging** – encrypted, signed communication
- **SecureStorage** – encrypted local key storage
- **AppSync** – automatic application updates
- **SystemExtra** – OS utilities (admin check, service management)

## Security Model

- All data is encrypted **client-side** before leaving the device (AES-256 + Bitcoin-derived keys).
- The server never receives plaintext data.
- Packets are digitally signed to prevent man-in-the-middle attacks.
- The application requires **administrator/root** privileges to synchronize the system clock and create hard links for backups.
- The passphrase is the sole credential; losing it means losing access (trustless design, no recovery via a third party).

## Supported Platforms

Windows, Linux, macOS (cross-platform .NET 9 runtime required).

## Configuration

Main settings are in `appsettings.json`. Key fields include cloud path, web interface port, virtual disk toggle, and backup path. The default configuration is suitable for most users.
