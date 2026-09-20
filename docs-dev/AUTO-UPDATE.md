# CloudClient — automatic update system (contributor / verification guide)

Technical reference for anyone who wants to **test**, **verify**, or **improve** the
automatic update. The update logic itself lives in the separate library
[`GitHubAppSync`](https://github.com/Graphene-Lab/GitHubAppSync); this document covers
how CloudClient wires it, how the release assets are produced, and how to check the whole
thing end-to-end.

Replaced: the old `AppSync` updater that pulled from a private HTTP directory server
(`update.tc0.it:5050`). See the GitHubAppSync README for *why* (anti-downgrade, SHA-256,
no DNS pinning). This doc assumes that "why" and focuses on the CloudClient specifics.

---

## 1. What runs

At app startup `Util.MonitorUpdates()` starts a timer (first check after 1 h, then every
25 h). The "Check for updates" button calls `Util.UpdateApplication()` on demand. Both
delegate to `GitHubAppSync.Update`, which runs this pipeline:

```
manifest → version compare → download → SHA-256 verify → extract → diff → apply → restart
```

1. **Manifest** — fetch `<channel>-manifest.json` from the release channel (small; read first).
2. **Version compare** — if the manifest version is **not newer** than the running
   `Assembly.GetEntryAssembly().Version`, stop with `AlreadyUpToDate`. This is the
   anti-downgrade guard: an older/equal build is never applied.
3. **Download** — fetch the zip asset named in the manifest to a temp file.
4. **SHA-256 verify** — hash the downloaded zip and compare with `manifest.sha256`.
   Mismatch → `IntegrityFailure`, **nothing is written**. The check is in the pipeline,
   not the transport, so a custom source cannot skip it.
5. **Extract** — unpack the zip to a temp dir.
6. **Diff** — compare extracted files against the install dir (SHA-256 per file).
7. **Apply** — copy changed files over the install dir (existing files are parked to temp
   first). `appsettings.json` is never overwritten (local config). Pruning of removed
   files is **off** by default (`PruneRemovedFiles=false`).
8. **Restart** — only when the app reports it is safe (`Static.CanUpdate`). On Windows the
   exe is relaunched; on Linux/macOS the process exits so the supervisor restarts it.

Result status: `Updated`, `AlreadyUpToDate`, `NoManifest`, `IntegrityFailure`, `Error`.

---

## 2. CloudClient wiring (`Cloud/Util.cs`)

```csharp
private const string UpdateRepository = "Graphene-Lab/CloudClient";

private static string UpdateChannel =>
    File.Exists(Path.Combine(AppContext.BaseDirectory, "Cloud.dll"))
        ? "portable"
        : RuntimeInformation.RuntimeIdentifier;

static public string UpdateApplication() =>
    Update.CheckAndUpdate(UpdateRepository, Static.CanUpdate, UpdateChannel).ToString();

static public void MonitorUpdates() =>
    Update.MonitoringUpdates(UpdateRepository, Static.CanUpdate, UpdateChannel);
```

**Channel selection** is the one non-obvious piece:

- If `Cloud.dll` sits next to the executable → **framework-dependent** build → channel
  `portable`. This is the build the website ships and the one that self-updates.
- Otherwise → **self-contained single-file** build → channel = the RID
  (`win-x64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`).

The updater reads the asset pair for that channel:
`https://github.com/Graphene-Lab/CloudClient/releases/latest/download/<channel>.zip`
and `…/<channel>-manifest.json`.

`Static.CanUpdate` is the gate that decides when a restart is safe (no backup running, etc.).
Keep that contract correct — a wrong `true` restarts the app at a bad moment.

---

## 3. Release asset convention

Each release carries, **per channel**, a zip + a manifest. The zip stores its files at the
**archive root** (no wrapping folder), because the client extracts and compares relative
paths against the install directory.

```
portable.zip              portable-manifest.json
```

Manifest shape (camelCase, read case-insensitively):

```json
{
  "channel": "portable",
  "version": "1.26.09.17",
  "asset": "portable.zip",
  "sha256": "<lowercase hex of the zip>",
  "generatedAtUtc": "2026-09-17T…Z"
}
```

The assets are produced in CI by `release.yml` → job `portable`:

1. `dotnet publish Cloud/Cloud.csproj -c Release -o ./portable` (framework-dependent, no RID).
2. `pwsh -File GitHubAppSync/tools/New-UpdateAsset.ps1 -PublishDir ./portable -Channel portable -Version <ver> -OutDir ./portable-assets`
3. The `release` job attaches `artifacts/portable-update/*` to the GitHub release.

The helper is taken from the **GitHubAppSync repo** (checked out into the run) so the asset
format stays identical to what the client parser reads — one source of truth.

---

## 4. How to test locally (no network)

The pipeline is exercised by a verifier that runs against a local **fake** `IUpdateSource`,
so it needs no GitHub and no real release. From the GitHubAppSync repo:

```
dotnet run --project tests/Verify
```

It asserts: no-manifest, older build **not** applied (anti-downgrade), equal build not
applied, newer build applied with the correct file delta, tampered payload rejected with
**nothing written**, local `appsettings.json` preserved across an update, pruning on/off,
and the JSON wire-format contract between the CI helper and the client parser. Exits
non-zero on any failure.

Run this whenever you change `GitHubAppSync` (the library) — it is the regression net for
the update logic.

---

## 5. How to verify a real release end-to-end

After a release (e.g. `v1.26.09.17`) is produced, confirm the assets are coherent before
pointing anything at them:

```bash
# 1. The release carries the portable pair
gh release view v1.26.09.17 --repo Graphene-Lab/CloudClient \
  --json tagName,isPrerelease,assets --jq '{tag:.tagName,prerelease:.isPrerelease,assets:[.assets[].name]}'
# expect: portable.zip + portable-manifest.json present, prerelease=false

# 2. The stable redirect resolves (this is what clients hit)
curl -fsSL -o portable-manifest.json \
  https://github.com/Graphene-Lab/CloudClient/releases/latest/download/portable-manifest.json
curl -fsSL -o portable.zip \
  https://github.com/Graphene-Lab/CloudClient/releases/latest/download/portable.zip

# 3. The zip's SHA-256 matches the manifest (Linux/macOS)
sha256sum portable.zip
# (Windows)  certutil -hashfile portable.zip SHA256
# compare against the "sha256" field in portable-manifest.json — must be identical, lowercase

# 4. The manifest version equals the release tag (minus the leading v)
#    tag v1.26.09.17  ->  manifest.version 1.26.09.17
```

If the SHA-256 does not match, the client will report `IntegrityFailure` and refuse to
update — treat that as a broken release, do not ship it.

**Live client test:** install the current portable build, then (with a newer release
published) click "Check for updates" and confirm it reports `Updated <old> -> <new>` and
restarts only when `CanUpdate` is true. To force the path without waiting, call
`Util.UpdateApplication()` directly.

---

## 6. Known gap: self-contained installs do not self-update yet

Only the **portable** (framework-dependent) channel has assets today. The self-contained
single-file RID archives (`cloudclient-win-x64.tar.gz`, the MSI, etc.) select their RID
channel by the logic in §2, but `release.yml` does **not** yet publish `win-x64.zip` /
`win-x64-manifest.json` — so a self-contained install gets `NoManifest` and never
self-updates.

This is partly intentional: a running single-file self-contained app has its own executable
locked, so replacing it in place is fragile. The portable framework-dependent build is the
designed self-update path.

**To add a per-RID self-update channel** (if you decide to), in `release.yml` add a step to
the build (or a per-RID job) that runs the helper against the self-contained publish and
attaches the pair:

```bash
pwsh -File GitHubAppSync/tools/New-UpdateAsset.ps1 \
  -PublishDir ./publish -Channel ${{ matrix.rid }} \
  -Version '${{ needs.check-version.outputs.version }}' -OutDir ./rid-assets
# then attach rid-assets/* in the release job
```

Before doing this, verify the FileApplier can replace a running single-file exe on each OS
(park-then-copy may fail while the exe is locked). Test the restart path per platform.

---

## 7. Invariants to preserve when improving

These are load-bearing; breaking them re-introduces the bugs the rewrite removed:

- **Never apply an older or equal version.** The version compare is the anti-downgrade
  guard. Do not replace it with a "files differ" check.
- **SHA-256 is verified in the pipeline**, after the download returns — not inside the
  transport. A custom `IUpdateSource` must not be trusted to hash for you.
- **Read through `releases/latest/download`**, not the REST API. The stable redirect does
  not consume the 60 req/h/IP unauthenticated API limit; a fleet behind NAT would exhaust
  it. Do not switch the manifest/asset fetch to `api.github.com`.
- **No DNS pinning.** Do not rewrite the host to a resolved IP — it breaks TLS and is
  wrong behind a CDN.
- **Zip entries at archive root.** If the wrapping folder changes, the relative-path diff
  breaks and every file looks changed (or missing).
- **`appsettings.json` is excluded** from overwrite. Keep local config out of the payload.
- **Restart only on `CanUpdate`.** Never restart over a running backup/sync.

---

## 8. Release-flow gotchas (CloudClient side)

- **GitHubAppSync must be on NuGet before a CloudClient release runs.** The CI `portable`
  job restores `GitHubAppSync 1.*` (the sibling repo is not checked out on the runner). If
  the library changed, tag/push GitHubAppSync (`v*` → `publish.yml`) and confirm the new
  version is on nuget.org before the CloudClient gate-off push.
- **The tag-exists guard.** `check-version` skips the release if `v$VERSION` already exists,
  so a docs push with the gate still off will **not** create a duplicate release. The
  gate-restore commit carries `[skip ci]` so it creates no run at all.
- **Version normalization.** `1.26.09.17` (4-section, from `1.$(ReleaseDate)`) normalizes
  to `1.26.9.17` in NuGet. The MSI folds to a 3-section file version
  (`1.26.917`) that is **not** comparable to the 4-section release version — always
  compare on the manifest `version`, never on the MSI file version.
- **`ReleaseDate` pin.** The gate-off commit pins `ReleaseDate` to the intended local push
  date so the UTC runner cannot derive the previous day and reuse a version.
- **Channel releases must be FULL releases (not pre-release, not draft).** The updater reads
  via `releases/latest`, and GitHub's `releases/latest` returns only the newest
  **non-draft, non-prerelease** release. If a release carrying the channel assets is marked
  "pre-release", the redirect skips it (resolves to the previous stable, or 404s) and the
  client silently reports `NoManifest` — it never sees the new build. CloudClient's
  `release.yml` creates full releases (`prerelease=false`), so this holds; do not mark a
  channel release as pre-release. (Supporting pre-release channels would require reading the
  REST API, which reintroduces the rate-limit the stable redirect avoids.)

---

## 9. Where things live

| Concern | Location |
|---|---|
| Update logic (library) | `GitHubAppSync` repo: `Update.cs`, `FileApplier.cs`, `FileStructure.cs`, `GitHubReleaseSource.cs` |
| CloudClient wiring | `Cloud/Util.cs` (`UpdateChannel`, `UpdateApplication`, `MonitorUpdates`) |
| "Check for updates" UI | `Cloud/Components/Pages/Utility.razor` |
| Asset helper | `GitHubAppSync/tools/New-UpdateAsset.ps1` |
| Asset verifier | `GitHubAppSync/tests/Verify` (`dotnet run --project tests/Verify`) |
| Release pipeline | `.github/workflows/release.yml` (jobs: `check-version`, `build`, `store-msi`, `portable`, `release`) |
| Release/version gate | `Cloud/Cloud.csproj` (`IsPrerelease`, `ReleaseDate`) |
