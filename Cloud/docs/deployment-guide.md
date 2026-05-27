# Cloud – Deployment Guide

## Windows

1. Extract the published artefacts to `C:\Program Files\Cloud`.
2. Run `Cloud.exe` as Administrator. On first run you may be prompted to install the .NET runtime.
3. (Optional) To run as a Windows service, use `sc create Cloud binPath= "C:\Program Files\Cloud\Cloud.exe"` or the provided `install.bat`.

## Linux / macOS

```bash
chmod +x install.sh
sudo ./install.sh
```

The script installs the application and configures it as a systemd service (Linux) or launchd agent (macOS).

To uninstall:
```bash
sudo ./uninstall.sh
```

## Configuration (`appsettings.json`)

| Key | Description | Default |
|---|---|---|
| `CloudPath` | Local directory to synchronize | `~/Cloud` |
| `Port` | Web UI port | `5000` |
| `VirtualDisk` | Enable virtual disk feature | `false` |
| `BackupPath` | Destination directory for backups | *(none)* |

## Multi-Instance Deployment

To connect the same machine to multiple cloud servers:

1. Copy the installation folder to a separate path.
2. Edit `appsettings.json` in the copy: set a unique `Port` and a unique `CloudPath`.
3. Start each instance independently.

## PWA Installation

Navigate to the web UI URL in a compatible browser and click the browser's "Install" button to install Cloud as a Progressive Web App.

## Network Requirements

- Outbound TCP connection to the cloud server (default port configured on the server side).
- Inbound access is only needed for the local web UI (localhost by default).

## Administrator / Root Requirement

The application requires elevated privileges to:
- Synchronize the system clock.
- Create hard links during backup operations.
- Access all file-system paths.

## Updates

If `AppSync` is configured with a repository URL, the application will check for updates at startup and can apply them automatically or on demand from the UI.
