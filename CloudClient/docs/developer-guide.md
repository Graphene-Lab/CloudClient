# CloudClient – Developer Guide

## Integration

Add a reference to `CloudClient` and instantiate `Client`:

```csharp
using CloudClient;

var client = new Client(cloudPath: "/home/user/Cloud", syncIsEnabled: true);

// Enable OS tray icon and pass a callback to open the UI
client.EnableOSFeatures(openUI: () => { /* open browser or window */ });
```

Call `client.Quit()` on application exit (Windows) to clean up the tray icon.

## Constructor Parameters

| Parameter | Type | Description |
|---|---|---|
| `cloudPath` | `string?` | Local directory for the cloud. `null` = platform default |
| `syncIsEnabled` | `bool` | `false` suspends sync (useful when the virtual disk is unmounted) |

## Reacting to Sync Status Changes

Override or subscribe to `OnLocalSyncStatusChanges` to react to status transitions:

```csharp
// The Client class internally handles this and updates the tray icon.
// To add custom logic, subclass Client and override the method.
```

## Server Commands

Incoming commands from the cloud server are dispatched via `OnServerCommand`. Add handlers in `ClientServerCommands.cs`.

## QR Code Pairing

The initial pairing with a cloud server is done by scanning the server's QR code. `QrCodeDetector` handles camera access and decoding. After the first successful pairing the credentials are persisted by `SecureStorage` and QR detection is disabled (`DisallowDetectQrCode = true`).

## Building

```powershell
dotnet build CloudClient/CloudClient.csproj
```

The project targets **.NET 9**.
