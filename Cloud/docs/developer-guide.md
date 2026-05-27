# Cloud – Developer Guide

## Prerequisites

- .NET 9 SDK
- Administrator / root privileges (required at runtime)
- A compatible private cloud server (or a network path for backup-only mode)

## Repository Layout

```
Cloud/
├── Components/
│   ├── Layout/          # MainLayout, NavMenu
│   └── Pages/           # One .razor per panel (Backup, Security, Diagnostics, …)
├── Panels/              # C# logic for Applications and Share panels
├── Services/            # ThemeService
├── wwwroot/
│   ├── themes/          # CSS theme files
│   └── lib/bootstrap/
├── Api.cs               # Encrypted REST API surface
├── BackupManager.cs     # Backup orchestration
├── Program.cs           # App host bootstrap
├── Static.cs            # Application-wide singletons
├── Util.cs              # Utility helpers
└── VirtualDisk.cs       # Virtual disk management
```

## Running Locally

```powershell
cd Cloud
dotnet run
```

The application must be run as **Administrator** (Windows) or **root** (Linux/macOS).

On first launch the app generates a passphrase / key-pair and displays a QR code that the cloud server must scan to establish the encrypted pairing.

## Adding a New Panel

1. Create `Components/Pages/MyPanel.razor` with `@page "/mypanel"`.
2. Add the navigation entry in `Components/Layout/NavMenu.razor`.
3. Inject `Static.Client` (the `CloudClient.Client` instance) for cloud operations.

## Themes

CSS theme files live in `wwwroot/themes/`. `ThemeService` (injected as a scoped service) stores the current theme and raises a change notification consumed by `MainLayout.razor`.

To add a new theme, drop a `.css` file in `wwwroot/themes/` and register it in `ThemeService`.

## API Endpoints

The encrypted REST API is exposed via `ApiMiddleware` (from the `UISupportBlazor` package). Commands are declared as public static methods in `Api.cs` and are automatically discoverable by the middleware.

POST to `/api` with a JSON body:
```json
{ "command": "MethodName", "args": [ ... ] }
```

Responses are JSON-encoded return values.

## Upgrade / Update

The `AppSync` library provides self-update functionality. Place the new build artefacts in the configured repository URL and call `AppSync.Update.Run()` from the UI.

## Logging and Error Handling

- Unhandled exceptions are caught by `CloudSync.Util.UnhandledException` and written to a crash log.
- The application exits with code 1 if not running as admin or if another instance is already running.
- Use `CloudSync.Util.RecordError` to log non-fatal errors.
