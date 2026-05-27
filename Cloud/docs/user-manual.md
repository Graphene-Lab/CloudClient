# Cloud – User Manual

## Introduction

**Cloud** is a private cloud client that synchronises your files with your own cloud server using military-grade encryption. Your data never passes through third-party servers; all communication is exclusively between your machine and your private cloud.

---

## First Launch

1. Start the application (administrator/root required).
2. The web interface opens automatically in your default browser at `http://localhost:5000` (or the port configured in `appsettings.json`).
3. On first launch, a **passphrase** (12 or 24 words) is generated and displayed. **Write it down and store it securely** — this is the only way to recover your account.
4. A **QR code** is shown. Open the cloud server's pairing screen and scan it (or ask your administrator to scan it on the server side) to establish the encrypted connection.

---

## Main Panels

### Home / Status
Shows the current synchronisation status, number of pending files, and connection state.

### Backup
Configure the backup destination (requires a separate physical drive). Toggle daily backup and versioning. View the backup history.

### Security
Display your public key, regenerate credentials (⚠ this resets your pairing), and manage access PINs.

### Antivirus
View results of antivirus scans performed during synchronisation.

### Diagnostics
View sync logs, connection events, and error reports.

### Library
Browse the files in your cloud directory.

### Monitoring
Real-time file transfer progress and sync activity.

### Redundancy
Configure remote mirror paths (pen drive on router, NAS, etc.) for an additional copy of your data.

### Signature
Sign a document using your private key. Share the signature file along with the document for third-party verification.

### Signature Validation
Verify a document signature produced by any Cloud user.

### QR
Display your own QR code for pairing with another client or server.

### Info
Software version, license, and system information.

---

## Synchronisation

Files placed in the configured cloud directory (`CloudPath` in `appsettings.json`, default `~/Cloud`) are automatically synchronised to the cloud server as soon as they are modified. The status icon indicates:

| Icon | Meaning |
|---|---|
| Default (grey) | Not connected |
| Spinning (blue) | Synchronisation in progress |
| Check (green) | All files synchronised |
| Warning (orange) | Error or attention needed |

---

## Backup

### Requirements
A **second physical drive** is required for backup. Backing up to the same drive as the source does not protect against hardware failure.

### Daily Backup
A full snapshot of your cloud directory is taken once per day. Hard links ensure that unchanged files take up no additional space.

### Versioning
Every time a file is saved, a versioned snapshot is created automatically. To restore an earlier version:
1. Open the **Backup** panel.
2. Navigate to the date/time of the version you want.
3. Copy the file back to your cloud directory.

---

## Digital Signature

1. Open the **Signature** panel.
2. Select the file you want to sign.
3. Click **Sign**. A `.sig` file is created alongside the original.
4. Share both the original file and the `.sig` file.

To verify:
1. Open the **Signature Validation** panel.
2. Select the original file and the `.sig` file.
3. The signer's public key and timestamp are displayed.

---

## Multiple Cloud Connections

To connect to a second cloud server, install the application in a different directory and configure a different `Port` and `CloudPath` in `appsettings.json`.

---

## Themes

Click the theme selector (top-right of the UI) to switch between:
- **Compact** – minimal padding, suitable for small screens.
- **Dark** – dark background.
- **Modern** – contemporary light design.
- **Professional** – business-oriented style.

---

## Troubleshooting

| Problem | Solution |
|---|---|
| Application does not start | Ensure you are running as administrator/root |
| "Already running" message | Another instance is open; check the system tray |
| Cannot connect to server | Check network connectivity and firewall rules |
| Files not syncing | Check the Diagnostics panel for errors |
| Forgot passphrase | Unrecoverable — the passphrase is your only credential |
