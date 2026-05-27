# CloudClient – Architecture Decision Records

## ADR-001: Extend CloudBox Rather Than Wrap It

**Date**: 2024  
**Status**: Accepted

### Context
The cloud client application needs all the functionality of `CloudBox` plus OS-specific features (tray icon, QR detection). Two approaches were considered: composition (wrap `CloudBox` as a field) or inheritance (extend `CloudBox`).

### Decision
`CloudClient.Client` **extends** `CloudBox.CloudBox` via inheritance.

### Consequences
- **Positive**: full access to protected members of `CloudBox` without additional API surface.
- **Positive**: `Client` can be passed anywhere a `CloudBox` is expected.
- **Negative**: inheriting from a library class creates a tighter coupling; changes to `CloudBox` protected API require updates to `Client`.

---

## ADR-002: OS Feature Opt-In via `EnableOSFeatures`

**Date**: 2024  
**Status**: Accepted

### Context
Some hosts (e.g., a headless Linux service) do not have a graphical desktop. Installing a tray icon on a headless system would throw an exception.

### Decision
OS-specific features (tray icon on Windows, status-bar icon on macOS) are **not** activated in the constructor. The host must explicitly call `client.EnableOSFeatures(openUI)`.

### Consequences
- **Positive**: `Client` can be instantiated in headless/server scenarios without errors.
- **Positive**: the `openUI` callback allows the host to control how the UI is opened from the tray icon.
- **Negative**: callers that want the tray icon must remember to call `EnableOSFeatures`.

---

## ADR-003: QR Detection Disabled After First Pairing

**Date**: 2024  
**Status**: Accepted

### Context
After the initial server pairing, continuously scanning for QR codes wastes CPU and camera resources.

### Decision
Set `QrCodeDetector.DisallowDetectQrCode = true` in the `Client` constructor. The application's UI can re-enable it when the user explicitly initiates a new pairing.

### Consequences
- **Positive**: no background camera access after pairing.
- **Positive**: reduces permission warnings on platforms that alert on camera usage.
- **Negative**: re-pairing requires an explicit UI action.
