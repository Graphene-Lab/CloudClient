<#
.SYNOPSIS
Checks that a Cloud Client publish payload contains the external folder icons.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PayloadDir
)

$ErrorActionPreference = 'Stop'
$iconNames = @('Cloud', 'Documents', 'Download', 'Movies', 'Photos', 'Pictures', 'Settings', 'Share')

foreach ($iconName in $iconNames) {
    $iconPath = Join-Path (Join-Path $PayloadDir 'Assets') "$iconName.ico"
    if (-not (Test-Path -LiteralPath $iconPath -PathType Leaf)) {
        throw "Publish payload is missing Assets/$iconName.ico. CloudSyncLibrary must package and copy its icons into the final application."
    }
}

Write-Host 'Publish payload contains all eight Cloud Client folder icons.'
