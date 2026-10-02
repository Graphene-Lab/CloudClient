# Installation and Update Architecture

This document explains how Cloud Client is distributed, installed, and updated. It is a
public reference. It contains no secrets and no private information. For the deep update
internals see `docs-dev/AUTO-UPDATE.md`. For the release automation see
`docs-dev/RELEASE-PIPELINE.md`.

## 1. Distribution channels

Cloud Client has **one** distribution channel: **GitHub Releases**.
Every release of this repository carries a fixed set of assets:

| Asset | What it is |
|---|---|
| `CloudClient-<version>.msi` | Windows per-machine installer (WiX v5) |
| `cloudclient-win-x64.tar.gz` | Self-contained archive, Windows x64 |
| `cloudclient-linux-x64.tar.gz` | Self-contained archive, Linux x64 |
| `cloudclient-linux-arm64.tar.gz` | Self-contained archive, Linux ARM64 |
| `cloudclient-osx-x64.tar.gz` | Self-contained archive, macOS x64 |
| `cloudclient-osx-arm64.tar.gz` | Self-contained archive, macOS ARM64 |
| `portable.zip` | Framework-dependent build, used by the self-update |
| `portable-manifest.json` | Update manifest for the portable channel (version + SHA-256) |

The archives and the MSI are the product. The `portable.zip` + manifest pair is the
self-update payload. All of them come from the same source build.

## 2. Microsoft Store — not used (verification note)

**Cloud Client is not published on the Microsoft Store.** This was verified against the
release pipeline and the Store account.

The Microsoft Store product id `9P61PN50Q957` is **"Graphene Agent Bridge"**, which is a
different application (AgentBridge). AgentBridge is packaged as **MSIX** and is submitted
to the Store as the application itself, not as an installer. The Store re-signs that MSIX
package during ingestion.

The point that is easy to confuse: the **MSI installer for Cloud Client lives on GitHub
Releases, not on the Store.** There is no Cloud Client entry on the Store at all, so there
is no "installer on the store" for Cloud Client.

Why Cloud Client cannot go to the Store:

- The app needs **administrator privileges** to run. It uses them to synchronize the system
  clock, create hard links for backups and file versioning, create and mount the optional
  encrypted virtual disk, and register the background autostart service.
- Store apps run inside an **AppContainer** at the user's normal, non-elevated privileges.
  The `runFullTrust` capability does **not** grant administrator rights, and the Store has
  no mechanism for clock synchronization, virtual-disk mounting, or service registration.
- A Store-packaged build would fail the `IsAdmin()` check at startup and exit.

So the honest channel for Cloud Client is a per-machine MSI on GitHub Releases, with the
administrator requirement disclosed up front (see section 6).

## 3. Installation modes

### 3.1 Windows MSI (per-machine)

The file `CloudClient-<version>.msi` is built by the `store-msi` job in the release
pipeline, using WiX v5 (`tools/installer/New-CloudClientInstaller.ps1`) against the
win-x64 self-contained publish.

What the installer does:

- Installs the application under `C:\Program Files\Graphene Lab\Cloud Client`.
- Creates a Start Menu shortcut and a Desktop shortcut that launch `Cloud.exe`.
- Registers the background service for automatic start.
- Ships the AGPL-3.0 `LICENSE.md` and the `NOTICE.txt` disclosure inside the install folder.
- Requests administrator elevation through Windows UAC.

The MSI is **per-machine** (`Scope=perMachine`). It is unsigned unless a signing
certificate is configured. The project is AGPL-3.0, so it is eligible for the free
SignPath open-source signing program. When unsigned, Windows SmartScreen may show a warning;
the source is public and auditable.

### 3.2 Archives with install scripts (portable)

Each platform archive is a self-contained single-file build. It needs no .NET runtime
installed, because the runtime is bundled inside the archive.

Install steps:

1. Download the archive for your platform.
2. Extract it to the folder where the app should live.
3. Run `install.sh` (Linux/macOS) or `install.bat` (Windows). The script sets up the
   autostart entry and starts the app.

This mode is portable: the app runs from the folder you extracted it to.

### 3.3 PWA (Progressive Web App)

Cloud Client runs a local web server (Kestrel) and serves a Blazor web UI. That UI can be
installed as a **PWA** from the browser. The PWA is a shortcut to the local web UI; it is
not a separate distribution. The native install (MSI or archive) is still what runs the
server and the privileged operations.

## 4. Update modes

### 4.1 The updater

Automatic updates use the library **`GitHubAppSync`**. The update pipeline is:

```
manifest -> version compare -> download -> SHA-256 verify -> extract -> diff -> apply -> restart
```

1. **Manifest** — read `<channel>-manifest.json` from the release channel.
2. **Version compare** — if the manifest version is not newer than the running version,
   stop. This is the anti-downgrade guard.
3. **Download** — fetch the zip named in the manifest.
4. **SHA-256 verify** — hash the downloaded zip and compare with the manifest. A mismatch
   stops the update and writes nothing.
5. **Extract** — unpack to a temp folder.
6. **Diff** — compare the extracted files with the install folder, file by file.
7. **Apply** — copy the changed files over the install folder. `appsettings.json` is never
   overwritten, so local configuration survives.
8. **Restart** — only when the app reports it is safe to restart.

The update reads through the stable redirect
`https://github.com/Graphene-Lab/CloudClient/releases/latest/download/...`. It does not
use the GitHub REST API, so a fleet of clients behind one NAT address does not exhaust the
API rate limit.

### 4.2 Channel selection

The running build picks its update channel by how it was built:

- If `Cloud.dll` sits next to the executable, the build is **framework-dependent** and the
  channel is `portable`.
- Otherwise the build is **self-contained single-file** and the channel is the RID
  (`win-x64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`).

This is decided in `Cloud/Util.cs` (`UpdateChannel`).

### 4.3 What self-updates today

- The **portable** (framework-dependent) build self-updates. It has the `portable.zip` +
  `portable-manifest.json` assets on every release.
- The **self-contained** builds (the RID archives and the MSI) select their RID channel, but
  the release pipeline does not yet publish a per-RID zip + manifest. So a self-contained
  install reports `NoManifest` and does not self-update. To update those, install the new
  MSI or the new archive.

This split is deliberate. A running single-file self-contained app keeps its own executable
locked, so replacing it in place is fragile. The portable build is the designed self-update
path.

### 4.4 The safe-restart gate

The updater restarts the app only when `Static.CanUpdate()` returns true. That gate is true
only when the client is connected, the client can restart, and no backup is running. This
prevents an update from restarting the app in the middle of a backup or sync.

### 4.5 Manual check

The web UI has a "Check for updates" action that calls the update on demand, instead of
waiting for the timer. The timer runs a first check one hour after start, then periodically.

## 5. Update integrity and safety

These properties are load-bearing for the update system:

- An older or equal version is never applied (anti-downgrade).
- SHA-256 is verified in the pipeline after the download, not inside the transport.
- The download uses the stable `releases/latest` redirect, not the REST API.
- No DNS pinning.
- Zip entries are stored at the archive root, so the relative-path diff works.
- `appsettings.json` is excluded from overwrite.
- The app restarts only when `CanUpdate` is true.

## 6. Administrator requirement and disclosure

Cloud Client requires administrator privileges. This is disclosed in three places so the
user is never surprised:

- The MSI requests UAC elevation at install time.
- The application manifest requests `requireAdministrator`.
- `tools/installer/NOTICE.txt` (shipped inside the install folder) explains, in plain
  words, why admin is needed and states the zero-knowledge privacy model: the app does not
  use, view, or transfer user data; files are encrypted before they leave the machine; the
  key stays on the client side.

## 7. Summary

| Mode | Channel | Self-updates | Needs admin |
|---|---|---|---|
| Windows MSI | GitHub Releases | No (install new MSI) | Yes |
| Self-contained archive (per RID) | GitHub Releases | No (NoManifest yet) | Yes |
| Portable (framework-dependent) | GitHub Releases | Yes (portable.zip) | Yes |
| PWA | Browser, over the local server | Follows the native install | Yes (the server does) |
| Microsoft Store | Not used | — | — |

Cloud Client is distributed only through GitHub Releases. It is not on the Microsoft Store,
because the administrator requirement is not compatible with the Store sandbox. The Store
product `9P61PN50Q957` is a different application (AgentBridge), packaged as MSIX.
