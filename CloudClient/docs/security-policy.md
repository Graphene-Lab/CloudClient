# CloudClient – Security Policy

## End-to-End Encryption

All communication between CloudClient and CloudServer is end-to-end encrypted via the `EncryptedMessaging` layer. The client generates a secp256k1 key pair on first run; the corresponding public key is exchanged with the server during the pairing process (QR code scan or manual credential entry).

## Credential Storage

Login credentials (`LoginCredential`) are stored via `SecureStorage`, which encrypts data at rest using a device-derived key. Credentials are never stored in plaintext.

## QR Code Pairing Security

- The QR code encodes a public key and connection parameters.
- Scanning must occur in a physically controlled environment to prevent "evil QR" attacks (malicious QR code substitution).
- `QrCodeDetector.cs` uses the device camera; users should verify the QR source is trusted.

## Platform Tray Icon State

The system tray icon reflects the current sync state:

| Icon | State |
|---|---|
| `CloudIconDefault` | Idle / not connected |
| `CloudIconSynchronize` | Synchronising |
| `CloudIconSynchronized` | Sync complete |
| `CloudIconWarning` | Error / attention needed |

Users should treat the `Warning` icon as a prompt to investigate connectivity or credential issues.

## macOS and Windows Support

`MacOSSupport.cs` and `WindowsSupport.cs` provide OS-specific integration (tray icon, file system events). Both implementations follow the principle of least privilege: no elevated permissions are requested beyond what is needed for file access.

## Reporting Vulnerabilities

Open a private GitHub Security Advisory in the repository. Do not disclose vulnerabilities publicly before a fix is available.
