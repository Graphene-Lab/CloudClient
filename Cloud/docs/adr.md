# Cloud – Architecture Decision Records

## ADR-001: Blazor Server over Blazor WebAssembly

**Date**: 2024  
**Status**: Accepted

### Context
The cloud client management UI runs on a local machine and must interact with OS-level features (file system, system clock, service management). Blazor WebAssembly is sandboxed and cannot call native OS APIs.

### Decision
Use **Blazor Server**, where the component logic runs on the server (local process in this case) and only the rendered HTML diff is sent to the browser via SignalR.

### Consequences
- **Positive**: full access to OS APIs and the CloudClient object model.
- **Positive**: no download of the .NET runtime to the browser.
- **Negative**: requires an active SignalR connection; not suitable as a standalone SPA.

---

## ADR-002: Administrator / Root Requirement

**Date**: 2024  
**Status**: Accepted

### Context
The application needs to: (1) synchronise the system clock, (2) create hard links for backup, (3) access all file-system paths.

### Decision
Require and enforce **administrator/root** privileges at startup. Exit with code 1 if not elevated.

### Consequences
- **Positive**: reliable access to all required OS features.
- **Negative**: users must explicitly run the application as admin; standard double-click launch may fail.

---

## ADR-003: Single-Instance Guard

**Date**: 2024  
**Status**: Accepted

### Context
Running two instances of the cloud client against the same cloud path would cause file conflicts and duplicate sync operations.

### Decision
A single-instance lock (a named mutex held for the whole life of the process) plus a quick
check of whether the control panel is already being served on the fixed UI port. If another
Cloud Client is already running, the new launch does not start a second web server: it opens
the already-running control panel in the browser and exits cleanly.

### Consequences
- **Positive**: data consistency guaranteed (only one instance touches the cloud path).
- **Positive**: clean user experience - a second launch re-uses the first instance's UI
  instead of colliding on the fixed port and stopping with "The port N is busy!" (issue #7).
- **Negative**: multi-cloud scenarios require separate installation paths (by design).

---

## ADR-004: Multi-Theme CSS Architecture

**Date**: 2024  
**Status**: Accepted

### Context
The application targets different user preferences and deployment scenarios (light/dark, compact for small screens, professional for business).

### Decision
Provide four discrete CSS theme files (`compact.css`, `dark.css`, `modern.css`, `professional.css`) loaded by `ThemeService` at runtime. Theme selection is persisted.

### Consequences
- **Positive**: no runtime CSS-in-JS; themes are plain CSS; easy to add new themes.
- **Negative**: common styles must be maintained in each file or factored into a shared base.

---

## ADR-005: PWA Support

**Date**: 2024  
**Status**: Accepted

### Context
Users should be able to install the web UI as a desktop app for quick access without opening a browser.

### Decision
Add `manifest.webmanifest` and `service-worker.js` to enable **Progressive Web App** installation.

### Consequences
- **Positive**: native-like installation experience on all platforms.
- **Negative**: service worker caching must be carefully managed to avoid serving stale UI after updates.
