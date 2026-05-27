# Cloud – Security Policy

## Security Model

Cloud is built on a **trustless** security model derived from Bitcoin cryptography. The core principle is: *you do not need to trust us – the mathematics and the open source code prove it*.

### Key Points

- **Client-side encryption only.** All data is encrypted with AES-256 (key derived from a BIP39 passphrase) before it leaves the device. The cloud server stores only ciphertext.
- **No user accounts on a server.** Identity is a cryptographic key-pair. There is no password database to breach.
- **Digital signatures on every packet.** Every network packet is signed with the sender's private key, preventing man-in-the-middle attacks.
- **Zero-knowledge server.** The server acts as a router/buffer. It cannot decrypt the content it stores.
- **Passphrase = full account.** The 12/24-word BIP39 passphrase regenerates the key-pair deterministically. Guard it like a hardware wallet seed.

## Reporting a Vulnerability

If you discover a security vulnerability, please **do not** open a public GitHub issue.

1. Send a detailed report to the project maintainer via private channel (see repository contacts).
2. Include: affected component, reproduction steps, potential impact, and suggested mitigation.
3. Allow a reasonable time (90 days) for a fix before public disclosure.

## Supported Versions

Only the latest released version receives security fixes. Update promptly when new versions are available.

## Known Security Requirements

- The application **must** run with administrator/root privileges. Restricting this will break clock sync and hard-link backup.
- Keep the passphrase offline (paper or hardware wallet). Losing it means permanent loss of access.
- Run the cloud server in an isolated environment (dedicated VM or physical machine) to reduce the attack surface.

## Dependency Security

All dependencies are open source and auditable:
- **.NET runtime** – maintained by Microsoft.
- **NBitcoin** – battle-tested Bitcoin cryptography library.
- **EncryptedMessaging** – in-house, open source.
- **SecureStorage** – in-house, open source.

Run `dotnet list package --vulnerable` regularly to check for known CVEs in NuGet dependencies.
