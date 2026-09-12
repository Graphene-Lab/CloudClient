<#
.SYNOPSIS
Builds the Windows MSI installer for the Cloud Client from a win-x64 publish payload.

.DESCRIPTION
Harvests every file under -PayloadDir into a per-machine MSI using the repo-local WiX v5
tool (tools/installer/dotnet-tools.json, `dotnet tool restore`). Installs to
"%ProgramFiles%\Graphene Lab\Cloud Client" mirroring the payload tree (Cloud.exe, wwwroot/,
Assets/, appsettings*.json), adds Start-menu + desktop shortcuts to Cloud.exe and the
standard uninstall entry.

Honesty notes (this is NOT a Microsoft Store package - the app needs admin, which the Store
sandbox forbids; see docs). The installer:
  * ships the AGPL-3.0 LICENSE.md and a NOTICE.txt that explains the admin requirement;
  * shows the AGPL license in the install wizard (WixUI_InstallDir) when -UseUi is on;
  * is code-signed only when a certificate is supplied (-SignPfx / -SignThumbprint / SIGN_*
    env vars). Without one it is built UNSIGNED and a warning is printed. The project is
    AGPL-3.0, so it is eligible for the free SignPath open-source signing program.

.PARAMETER PayloadDir
Absolute path of the win-x64 publish output (must contain Cloud.exe at its root).

.PARAMETER Version
Version for the product, e.g. 1.26.09.12 (MSI folds the 4th section into build: -> 1.26.912).

.PARAMETER OutDir
Folder for the produced .msi (default: <parent of PayloadDir>\installer-out).

.EXAMPLE
powershell -File tools\installer\New-CloudClientInstaller.ps1 -PayloadDir .\publish -Version 1.26.09.12
#>
param(
    [Parameter(Mandatory)][string]$PayloadDir,
    [Parameter(Mandatory)][string]$Version,
    [string]$OutDir,
    [ValidateSet('low', 'high')][string]$Compression = 'low',
    [string]$UseUi = '1',
    # Code signing: PFX on disk, or a cert in LocalMachine\My by thumbprint, or SIGN_* env vars.
    [string]$SignPfx,
    [string]$SignPfxPassword,
    [string]$SignThumbprint,
    [string]$TimestampUrl
)

$ErrorActionPreference = 'Stop'
$withUi = ($UseUi -notin @('0', 'false', 'False', 'no', 'No'))
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent (Split-Path -Parent $root)   # tools/installer -> repo root
$exe = Join-Path $PayloadDir 'Cloud.exe'
if (-not (Test-Path $exe)) { throw "Cloud.exe not found at '$exe' - is this a win-x64 Cloud Client payload?" }
if (-not $OutDir) { $OutDir = Join-Path (Split-Path -Parent $PayloadDir) 'installer-out' }

# MSI ProductVersion has three sections; fold the date-based 4th into build (MM*100+DD).
$vp = $Version.TrimStart('v') -split '\.'
if ($vp.Count -ge 4 -and $vp[2] -match '^\d+$' -and $vp[3] -match '^\d+$') {
    $ver = '{0}.{1}.{2}' -f [int]$vp[0], [int]$vp[1], ([int]$vp[2] * 100 + [int]$vp[3])
} else {
    $ver = ($vp[0..([Math]::Min(2, $vp.Count - 1))]) -join '.'
}

$productName = 'Cloud Client'
$manufacturer = 'Graphene Lab'
$upgradeCode = '9c1f7a42-5b8e-4c3d-9a6f-2e1d0c4b3a57'   # stable across builds for MajorUpgrade
$appDescription = 'Trustless private cloud client'

# ── Code signing configuration (same contract as AgentBridge) ─────────────
if (-not $SignPfx) { $SignPfx = $env:SIGN_PFX }
if (-not $SignPfxPassword) { $SignPfxPassword = $env:SIGN_PFX_PASSWORD }
if (-not $SignThumbprint) { $SignThumbprint = $env:SIGN_THUMBPRINT }
if (-not $TimestampUrl) { $TimestampUrl = 'http://timestamp.digicert.com' }
if ($env:SIGN_TIMESTAMP_URL) { $TimestampUrl = $env:SIGN_TIMESTAMP_URL }

$signTool = $null
if ($SignPfx -or $SignThumbprint) {
    $signTool = (Get-Command signtool.exe -ErrorAction SilentlyContinue).Source
    if (-not $signTool) {
        $sdk = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending | Select-Object -First 1
        if (-not $sdk) { throw 'Signing requested but signtool.exe was not found (install the Windows SDK or add it to PATH).' }
        $signTool = $sdk.FullName
    }
    Write-Host "Signing with $signTool, timestamped by $TimestampUrl"
}

function Invoke-Sign([string]$Path) {
    $signArgs = @('sign', '/fd', 'sha256', '/tr', $TimestampUrl, '/td', 'sha256')
    if ($SignPfx) {
        $signArgs += @('/f', $SignPfx)
        if ($SignPfxPassword) { $signArgs += @('/p', $SignPfxPassword) }
    }
    else { $signArgs += @('/sha1', $SignThumbprint, '/sm') }
    & $signTool @signArgs $Path
    if ($LASTEXITCODE -ne 0) { throw "signtool failed for '$Path' (exit $LASTEXITCODE)" }
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$wxs = Join-Path $root 'CloudClient.wxs'
$rootFull = [System.IO.Path]::GetFullPath($PayloadDir)

# ── Ship the license + admin notice into the install folder ───────────────
$licenseSrc = Join-Path $repoRoot 'LICENSE.md'
if (-not (Test-Path $licenseSrc)) { throw "AGPL LICENSE.md not found at '$licenseSrc'" }
$noticeSrc = Join-Path $root 'NOTICE.txt'
if (-not (Test-Path $noticeSrc)) { throw "NOTICE.txt not found at '$noticeSrc'" }

# ── Generate the license RTF for the install wizard ──────────────────────
$licenseRtf = Join-Path $root 'license.rtf'
function ConvertTo-Rtf([string]$text) {
    $sb = New-Object System.Text.StringBuilder
    foreach ($ch in $text.ToCharArray()) {
        $code = [int]$ch
        if ($ch -eq '\') { [void]$sb.Append('\\') }
        elseif ($ch -eq '{') { [void]$sb.Append('\{') }
        elseif ($ch -eq '}') { [void]$sb.Append('\}') }
        elseif ($ch -eq "`r") { }
        elseif ($ch -eq "`n") { [void]$sb.Append('\par ') }
        elseif ($code -lt 128) { [void]$sb.Append($ch) }
        else { [void]$sb.Append('\u' + $code + '?') }
    }
    return $sb.ToString()
}
$licenseBody = ConvertTo-Rtf (Get-Content $licenseSrc -Raw)
$rtf = "{\rtf1\ansi\ansicpg1252\deff0\nouicompat\deflang1040" +
       "{\fonttbl{\f0\fnil\fcharset0 Segoe UI;}}" +
       "\viewkind4\f0\fs20\b CLOUD CLIENT - LICENSE\b0\par " +
       "This software is licensed under the GNU Affero General Public License, version 3.0 (AGPL-3.0). " +
       "The full license text follows and is also shipped as LICENSE.md in the installation folder.\par\par " +
       $licenseBody + "\par}"
[System.IO.File]::WriteAllText($licenseRtf, $rtf, (New-Object System.Text.UTF8Encoding($false)))

# ── WiX document ─────────────────────────────────────────────────────────
Add-Type -AssemblyName System.Xml.Linq
$X = [System.Xml.Linq.XNamespace]::Get('http://wixtoolset.org/schemas/v4/wxs')

$dirIds = @{}
$dirIds[$rootFull] = 'INSTALLFOLDER'
$nextDir = 0
foreach ($d in (Get-ChildItem $rootFull -Directory -Recurse | Sort-Object FullName)) { $dirIds[$d.FullName] = 'D' + (++$nextDir) }

function New-WixDirectory([System.IO.DirectoryInfo]$dir, [string]$id) {
    $el = [System.Xml.Linq.XElement]::new($X + 'Directory',
        [System.Xml.Linq.XAttribute]::new('Id', $id),
        [System.Xml.Linq.XAttribute]::new('Name', $dir.Name))
    foreach ($child in ($dir.GetDirectories() | Sort-Object Name)) {
        $el.Add((New-WixDirectory $child $dirIds[$child.FullName]))
    }
    return $el
}

$xw = [System.Xml.Linq.XDocument]::new()
$xw.Declaration = [System.Xml.Linq.XDeclaration]::new('1.0', 'utf-8', $null)
$wix = [System.Xml.Linq.XElement]::new($X + 'Wix')

$pkg = [System.Xml.Linq.XElement]::new($X + 'Package',
    [System.Xml.Linq.XAttribute]::new('Name', $productName),
    [System.Xml.Linq.XAttribute]::new('Manufacturer', $manufacturer),
    [System.Xml.Linq.XAttribute]::new('Version', $ver),
    [System.Xml.Linq.XAttribute]::new('UpgradeCode', $upgradeCode),
    [System.Xml.Linq.XAttribute]::new('Scope', 'perMachine'),
    [System.Xml.Linq.XAttribute]::new('Compressed', 'yes'))
$pkg.Add([System.Xml.Linq.XElement]::new($X + 'MajorUpgrade',
    [System.Xml.Linq.XAttribute]::new('DowngradeErrorMessage', "A newer version of $productName is already installed.")))
$pkg.Add([System.Xml.Linq.XElement]::new($X + 'MediaTemplate',
    [System.Xml.Linq.XAttribute]::new('EmbedCab', 'yes'),
    [System.Xml.Linq.XAttribute]::new('CompressionLevel', $Compression)))
$pkg.Add([System.Xml.Linq.XElement]::new($X + 'StandardDirectory', [System.Xml.Linq.XAttribute]::new('Id', 'ProgramFiles64Folder')))
$pkg.Add([System.Xml.Linq.XElement]::new($X + 'StandardDirectory', [System.Xml.Linq.XAttribute]::new('Id', 'ProgramMenuFolder')))
$pkg.Add([System.Xml.Linq.XElement]::new($X + 'StandardDirectory', [System.Xml.Linq.XAttribute]::new('Id', 'DesktopFolder')))

# Shortcuts
$sc = [System.Xml.Linq.XElement]::new($X + 'ComponentGroup', [System.Xml.Linq.XAttribute]::new('Id', 'ShortcutComponents'))
$scStart = [System.Xml.Linq.XElement]::new($X + 'Component',
    [System.Xml.Linq.XAttribute]::new('Id', 'StartShortcut'),
    [System.Xml.Linq.XAttribute]::new('Directory', 'CCPROGMENU'),
    [System.Xml.Linq.XAttribute]::new('Guid', '3a7b1c9d-4e2f-4a6b-8c1d-5e7f9a2b4c6d'))
$scStart.Add([System.Xml.Linq.XElement]::new($X + 'Shortcut',
    [System.Xml.Linq.XAttribute]::new('Id', 'CloudClientStartMenu'),
    [System.Xml.Linq.XAttribute]::new('Name', $productName),
    [System.Xml.Linq.XAttribute]::new('Description', $appDescription),
    [System.Xml.Linq.XAttribute]::new('Target', '[INSTALLFOLDER]Cloud.exe'),
    [System.Xml.Linq.XAttribute]::new('WorkingDirectory', 'INSTALLFOLDER')))
$scStart.Add([System.Xml.Linq.XElement]::new($X + 'RemoveFolder', [System.Xml.Linq.XAttribute]::new('Id', 'RemoveStartMenuFolder'), [System.Xml.Linq.XAttribute]::new('On', 'uninstall')))
$sc.Add($scStart)
$scDesktop = [System.Xml.Linq.XElement]::new($X + 'Component',
    [System.Xml.Linq.XAttribute]::new('Id', 'DesktopShortcut'),
    [System.Xml.Linq.XAttribute]::new('Directory', 'DesktopFolder'),
    [System.Xml.Linq.XAttribute]::new('Guid', '5c9d3e1f-6a8b-4c2d-9e4f-7a1b3c5d7e9f'))
$scDesktop.Add([System.Xml.Linq.XElement]::new($X + 'Shortcut',
    [System.Xml.Linq.XAttribute]::new('Id', 'CloudClientDesktop'),
    [System.Xml.Linq.XAttribute]::new('Name', $productName),
    [System.Xml.Linq.XAttribute]::new('Description', $appDescription),
    [System.Xml.Linq.XAttribute]::new('Target', '[INSTALLFOLDER]Cloud.exe'),
    [System.Xml.Linq.XAttribute]::new('WorkingDirectory', 'INSTALLFOLDER')))
$sc.Add($scDesktop)
$pkg.Add($sc)

# Harvested payload file components
$pc = [System.Xml.Linq.XElement]::new($X + 'ComponentGroup', [System.Xml.Linq.XAttribute]::new('Id', 'ProductComponents'))
foreach ($f in (Get-ChildItem $rootFull -Recurse -File | Sort-Object FullName)) {
    $cid++
    $dir = Split-Path -Parent $f.FullName
    $c = [System.Xml.Linq.XElement]::new($X + 'Component',
        [System.Xml.Linq.XAttribute]::new('Id', ('C' + $cid)),
        [System.Xml.Linq.XAttribute]::new('Directory', $dirIds[$dir]))
    $c.Add([System.Xml.Linq.XElement]::new($X + 'File',
        [System.Xml.Linq.XAttribute]::new('Id', ('F' + $cid)),
        [System.Xml.Linq.XAttribute]::new('Source', $f.FullName)))
    $pc.Add($c)
}
$pkg.Add($pc)

# License + notice components (shipped in the install folder)
function New-LicenseComponent([string]$id, [string]$destName, [string]$src) {
    $c = [System.Xml.Linq.XElement]::new($X + 'Component',
        [System.Xml.Linq.XAttribute]::new('Id', $id),
        [System.Xml.Linq.XAttribute]::new('Directory', 'INSTALLFOLDER'),
        [System.Xml.Linq.XAttribute]::new('Guid', (New-Guid).ToString()))
    $c.Add([System.Xml.Linq.XElement]::new($X + 'File',
        [System.Xml.Linq.XAttribute]::new('Id', $id + '_F'),
        [System.Xml.Linq.XAttribute]::new('Source', $src),
        [System.Xml.Linq.XAttribute]::new('Name', $destName)))
    return $c
}
$lic = [System.Xml.Linq.XElement]::new($X + 'ComponentGroup', [System.Xml.Linq.XAttribute]::new('Id', 'LicenseComponents'))
$lic.Add((New-LicenseComponent 'LicenseAgplFile' 'LICENSE.md' $licenseSrc))
$lic.Add((New-LicenseComponent 'NoticeFile' 'NOTICE.txt' $noticeSrc))
$pkg.Add($lic)

$feat = [System.Xml.Linq.XElement]::new($X + 'Feature',
    [System.Xml.Linq.XAttribute]::new('Id', 'Main'),
    [System.Xml.Linq.XAttribute]::new('Title', $productName),
    [System.Xml.Linq.XAttribute]::new('Level', '1'))
$feat.Add([System.Xml.Linq.XElement]::new($X + 'ComponentGroupRef', [System.Xml.Linq.XAttribute]::new('Id', 'ProductComponents')))
$feat.Add([System.Xml.Linq.XElement]::new($X + 'ComponentGroupRef', [System.Xml.Linq.XAttribute]::new('Id', 'ShortcutComponents')))
$feat.Add([System.Xml.Linq.XElement]::new($X + 'ComponentGroupRef', [System.Xml.Linq.XAttribute]::new('Id', 'LicenseComponents')))
$pkg.Add($feat)

if ($withUi) {
    $pkg.Add([System.Xml.Linq.XElement]::new($X + 'WixVariable',
        [System.Xml.Linq.XAttribute]::new('Id', 'WixUILicenseRtf'),
        [System.Xml.Linq.XAttribute]::new('Value', $licenseRtf)))
    $pkg.Add([System.Xml.Linq.XElement]::new($X + 'UIRef', [System.Xml.Linq.XAttribute]::new('Id', 'WixUI_InstallDir')))
}

$wix.Add($pkg)

# Directory fragment
$dirFrag = [System.Xml.Linq.XElement]::new($X + 'Fragment')
$pfStd = [System.Xml.Linq.XElement]::new($X + 'StandardDirectory', [System.Xml.Linq.XAttribute]::new('Id', 'ProgramFiles64Folder'))
$install = [System.Xml.Linq.XElement]::new($X + 'Directory',
    [System.Xml.Linq.XAttribute]::new('Id', 'INSTALLFOLDER'),
    [System.Xml.Linq.XAttribute]::new('Name', 'Graphene Lab\Cloud Client'))
foreach ($t in (Get-ChildItem $rootFull -Directory | Sort-Object Name)) { $install.Add((New-WixDirectory $t $dirIds[$t.FullName])) }
$pfStd.Add($install)
$dirFrag.Add($pfStd)
$pmStd = [System.Xml.Linq.XElement]::new($X + 'StandardDirectory', [System.Xml.Linq.XAttribute]::new('Id', 'ProgramMenuFolder'))
$pmStd.Add([System.Xml.Linq.XElement]::new($X + 'Directory',
    [System.Xml.Linq.XAttribute]::new('Id', 'CCPROGMENU'),
    [System.Xml.Linq.XAttribute]::new('Name', $productName)))
$dirFrag.Add($pmStd)
$wix.Add($dirFrag)

$xw.Add($wix)
$xw.Save($wxs)

# ── Sign the payload PE files before the cabinet embeds them ──────────────
if ($signTool) {
    $peFiles = @(Get-ChildItem $rootFull -Recurse -File | Where-Object { $_.Extension -in '.exe', '.dll', '.sys' })
    Write-Host ("Signing {0} payload PE files" -f $peFiles.Count)
    foreach ($f in $peFiles) {
        if ((Get-AuthenticodeSignature $f.FullName).Status -ne 'Valid') { Invoke-Sign $f.FullName }
    }
}
else {
    Write-Warning 'No signing certificate supplied: the MSI is UNSIGNED. (AGPL-3.0 project -> eligible for the free SignPath open-source signing program.)'
}

# ── Build the MSI ─────────────────────────────────────────────────────────
Push-Location $root
try {
    dotnet tool restore | Out-Null
    $oldTmp = $env:TMP; $oldTemp = $env:TEMP
    $wixTmp = Join-Path (Split-Path -Parent $root) '.wix-tmp'
    New-Item -ItemType Directory -Force -Path $wixTmp | Out-Null
    $env:TMP = $wixTmp; $env:TEMP = $wixTmp
    try {
        $msi = Join-Path $OutDir ("CloudClient-" + $Version.TrimStart('v') + '.msi')
        $wixArgs = @('tool', 'run', 'wix', 'build', $wxs, '-o', $msi, '-arch', 'x64')
        if ($withUi) { $wixArgs += @('-ext', 'WixToolset.UI.wixext') }
        & dotnet @wixArgs
        if ($LASTEXITCODE -ne 0) { throw "wix build failed (exit $LASTEXITCODE)" }
        if ($signTool) { Invoke-Sign $msi }
        Write-Host ("MSI created: {0} ({1:N1} MB)" -f $msi, ((Get-Item $msi).Length / 1MB))
    }
    finally { $env:TMP = $oldTmp; $env:TEMP = $oldTemp }
}
finally { Pop-Location }
