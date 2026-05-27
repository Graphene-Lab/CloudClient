# Cloud – Testing Strategy

## Overview

The Cloud Blazor application has dependencies on OS-level features (file system, clock, admin privileges) that make end-to-end automated testing challenging. The strategy therefore layers tests by isolation level.

---

## 1. Unit Tests – Business Logic

Test pure logic classes that have no OS or network dependencies:

- `BackupManager` calculation logic (diff computation, hard-link decisions).
- `Util` helpers.
- `ThemeService` state transitions.
- `VirtualDisk` path calculations.

```csharp
[Fact]
public void ThemeService_DefaultTheme_IsModern()
{
	var svc = new ThemeService();
	Assert.Equal("modern", svc.CurrentTheme);
}
```

---

## 2. Component Tests (bUnit)

Render individual Blazor pages with mocked services:

```csharp
[Fact]
public void DiagnosticsPage_RendersWithoutException()
{
	using var ctx = new TestContext();
	ctx.Services.AddSingleton<ICloudClient>(Mock.Of<ICloudClient>());
	var cut = ctx.RenderComponent<Diagnostics>();
	Assert.NotNull(cut.Markup);
}
```

---

## 3. Integration Tests

Use `WebApplicationFactory` with a mock `CloudClient.Client` to test the full request pipeline:

```csharp
[Fact]
public async Task LoginPage_Returns200()
{
	var response = await _client.GetAsync("/login");
	Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}
```

---

## 4. Manual Acceptance Tests

Some features require manual verification due to OS dependencies:

| Test | Criterion |
|---|---|
| First-launch passphrase generation | Passphrase is displayed and is 12/24 words |
| QR code pairing | Server connects after scanning |
| Tray icon sync status | Icon changes colour on sync state change |
| Backup hard links | `fsutil hardlink list` shows links on Windows |
| Admin enforcement | Application refuses to start without elevation |

---

## Test Coverage Goals

| Area | Target |
|---|---|
| Business logic (pure functions) | ≥ 85 % |
| Blazor page smoke rendering | All pages render without exception |
| API endpoints | ≥ 70 % |
| OS-dependent paths | Manual only |

---

## Running Tests

```powershell
dotnet test
```
