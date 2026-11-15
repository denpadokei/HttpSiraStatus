[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('1.40.8', '1.44.1')]
    [string]$GameVersion,

    [string]$VersionsPath = (Join-Path $PSScriptRoot '..\versions.json'),
    [string]$ManifestPath = (Join-Path $PSScriptRoot '..\HttpSiraStatus\manifest.json')
)

$ErrorActionPreference = 'Stop'

$versions = Get-Content -Path $VersionsPath -Raw | ConvertFrom-Json
$matches = @($versions.versions | Where-Object { $_.game_version -eq $GameVersion })

if ($matches.Count -ne 1) {
    throw "versions.json must contain exactly one entry for game version '$GameVersion'."
}

$entry = $matches[0]
$manifest = Get-Content -Path $ManifestPath -Raw | ConvertFrom-Json

$manifest.gameVersion = $entry.game_version
$manifest.version = $entry.mod_version
$manifest.dependsOn.BSIPA = $entry.bsipa
$manifest.dependsOn.SiraUtil = $entry.sirautil

$json = $manifest | ConvertTo-Json -Depth 10
Set-Content -Path $ManifestPath -Value $json -Encoding utf8

Write-Output ("Prepared {0}: version {1}, BSIPA {2}, SiraUtil {3}" -f $entry.game_version, $entry.mod_version, $entry.bsipa, $entry.sirautil)

