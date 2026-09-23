# CloudClient — release pipeline & Linux distribution plan (technical reference)

Status: 2026-09-09 (phases 1–3 + 6 applied; 4–5 external/product-gated). Scope mirrors the
AgentBridge system **minus the Microsoft Store**: automatic updates with the `IsPrerelease`
gate + date versioning, dependency NuGet packages for CI, platform archives on GitHub
Releases, Linux desktop channels, easy README install.

## Current state (verified 2026-09-09, post phases 1–3)

- Solution `CloudClient.sln` at repo root; two projects:
  - `Cloud/Cloud.csproj` — the app: **Blazor web + PWA** (`wwwroot/service-worker.js`),
    `Microsoft.NET.Sdk.Web`, `OutputType=WinExe`, **requires administrator/root**
    (exits otherwise), .NET **10**, self-contained single-file per-RID on release
    (previously framework-dependent; release.yml publishes `--self-contained true`),
    ships `install.bat` / `install.sh` / `uninstall.sh` / `INSTALL.txt`.
  - `CloudClient/CloudClient.csproj` — helper library (MonoMac.NetStandard, System.Drawing).
- Dependencies: **dual-reference** — local sibling ProjectReference wins in dev; published
  NuGet package (`1.*`) in CI. All published 2026-09-09 at 1.26.9.9 (closure:
  `cloudbox`, `cloudsynclibrary`, `encryptedmessaging`, `communicationchannel`,
  `securestorage`, `fullduplexstreamsupport`, `bytesextension`, `appsync`, `AntiGitLibrary`,
  `backuplibrary`, `dataredundancy`, plus the pre-existing `systemextra`,
  `uisupportblazor`).
- GitHub: public `Graphene-Lab/CloudClient`, branch `master`, **release.yml active**:
  gate `IsPrerelease` → (optional) NuGet wait → 5 per-RID archives → GitHub release +
  changelog + tag. First release **v1.26.09.09** produced and verified.
- App auto-update: rewired 2026-09-17 from the private `AppSync` server to **GitHub
  Releases** via the `GitHubAppSync` library (`Cloud/Util.cs`). See
  [AUTO-UPDATE.md](AUTO-UPDATE.md) for the full design / test / verification guide.
- Linux store channels: all account-gated externally (Snap/Ubuntu SSO approval pending,
  AUR registration suspended, Flathub closed to this family) → tracked in TODO-LOCAL.md;
  the primary Linux channel is the GitHub release archive + `install.sh`.

## Architecture to implement (AgentBridge model)

### 1. Versioning + release gate (DONE in phase 1)
Same as AgentBridge: `<IsPrerelease>` (default true) + date version `1.yy.MM.dd`
(`<ReleaseDate>` pin for the gate-off commit so CI UTC never derives the previous day).
A push to `master` with `IsPrerelease=false` triggers `release.yml` → tag `v1.yy.MM.dd`.

### 2. Dependency NuGet publishing (needed before CI can build)
Add to each dependency repo the AgentBridge "core repo" convention: date-versioned pack on
`v*` tag push (`publish.yml`), `NUGET_API_KEY` secret. Package ids to use (check free):
`cloudbox` (from CloudBox/CloudLibraries/CloudBox), `appsync`, `antigithub`
(AntiGithub library). `uisupportblazor` / `systemextra` already published.
Then switch the csprojs to the **dual-reference** pattern
(`ProjectReference Condition="Exists(...)"` + `PackageReference Version="1.*"`) so dev
builds use siblings and CI restores NuGet.

### 3. release.yml (GitHub Release + archives)
Mirror AgentBridge: check-version gate → wait for today's dependency packages →
publish per-RID self-contained? NOTE: Cloud is currently **framework-dependent** (uses
system dotnet + admin). Decide per channel:
- Keep framework-dependent for the classic install (system runtime) **or** switch to
  self-contained single-file archives like AgentBridge (recommended for the stores).
Then create GitHub release with win-x64 / linux-x64(+arm64) / osx assets + changelog.
Auto-update: reuse the existing **AppSync** mechanism wired to the same release tags
(equivalent of AgentBridge AutoUpdate), gated by the same `IsPrerelease`.

### 4. Linux desktop channels (no MS Store)
- **Flatpak** self-published (AgentBridge-Linux pattern) + **Flathub** submission later.
- **Snap Store** (classic confinement request) + **AUR**.
- The app is **admin/root-required** and a **PWA** (installable from its web UI): this
  affects the sandbox/classic story — document clearly per channel (Snap classic is the
  natural fit for a root-level sync/backup tool).

### 5. README "Install & Download"
Add the AgentBridge-style badge block (platforms + latest release), keep the existing
`install.sh`/`install.bat` instructions, add the PWA note and the Linux store links as
they go live.

## Phase checklist
- [x] Phase 1 (repo-local): version gate + date versioning in `Cloud.csproj`; README
      "Install & Download" section; this plan doc.
- [x] Phase 2: dependency NuGet publishing + dual-reference switch in the csprojs.
      Published 2026-09-09 at 1.26.9.9 (transitive closure, bottom-up):
      `securestorage`, `fullduplexstreamsupport`, `communicationchannel`,
      `encryptedmessaging`, `cloudsynclibrary` (id `cloudsync` is squatted on nuget.org —
      see CloudLibraries commit), `cloudbox`, `backuplibrary`, `dataredundancy`,
      `AntiGitLibrary`, `appsync`. Every repo got its `publish.yml` (pack + push on a `v*`
      tag, NUGET_API_KEY secret) and the cross-repo csprojs were switched to the
      dual-reference pattern (local ProjectReference wins in dev, published package 1.* in
      CI). CloudClient's own csprojs dual-ref CloudBox / AppSync / AntiGitLibrary /
      SystemExtra / UISupportBlazor.
- [x] Phase 3: `release.yml` — gate IsPrerelease → (optional) NuGet wait for the dependency
      repos that carry today's tag → per-RID self-contained single-file publish (win-x64,
      linux-x64, linux-arm64, osx-x64, osx-arm64) → GitHub release + auto changelog + tag
      `v1.yy.MM.dd` pinned to the gate-off commit. E2E verified 2026-09-09: release
      `v1.26.09.09` with all five `cloudclient-<rid>.tar.gz` archives (the CloudClient.wpp
      zip hook is gated to Windows so Linux/macOS publishes are not affected).
- [x] Phase 4: in-app auto-update — rewired 2026-09-17 from the private `AppSync` HTTP
      server to **GitHub Releases** via the new general-purpose `GitHubAppSync` library
      (fork of AppSync: real version compare / anti-downgrade, SHA-256 verified in the
      pipeline, no DNS pinning, pluggable `IUpdateSource`). `Cloud/Util.cs` selects the
      channel (`portable` for the framework-dependent build, the RID otherwise); the
      `portable` job in `release.yml` produces `portable.zip` + `portable-manifest.json`.
      The portable publish targets `-r win-x64 --self-contained false` so the archive carries
      the Windows launcher `Cloud.exe` that `install.bat` runs (a no-RID publish baked a Linux
      ELF `Cloud` launcher and no `Cloud.exe`, breaking the Windows download — issue #9).
      First release through the new path: **v1.26.09.17**. Full design / test / verify
      guide: [AUTO-UPDATE.md](AUTO-UPDATE.md).
- [ ] Phase 5: Linux store channels — see TODO-LOCAL.md (local, gitignored). All store
      accounts are external gates: Snap classic needs the Ubuntu SSO approval (same account
      as AgentBridge, pending), AUR registration is suspended, Flathub rejects submissions
      for the AgentBridge family. Primary Linux channel = the GitHub release archives with
      `install.sh` (active and tested). README final done.
- [x] Phase 6: end-to-end green run — release `v1.26.09.09` produced by release.yml from a
      clean CI checkout (NuGet-only restore, no sibling repos), all five archives attached,
      prerelease gate restored afterwards with `[skip ci]`.
