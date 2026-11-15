[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('1.40.8', '1.44.1')]
    [string]$GameVersion,

    [string]$BeatSaberDir = $env:BeatSaberDir,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($BeatSaberDir)) {
    throw 'Pass -BeatSaberDir or set the BeatSaberDir environment variable.'
}

$root = $PSScriptRoot
$versionsPath = Join-Path $root 'versions.json'
$manifestPath = Join-Path $root 'HttpSiraStatus\manifest.json'
$manifestOriginal = Get-Content -Path $manifestPath -Raw

try {
    & (Join-Path $root 'scripts\prepare-manifest.ps1') -GameVersion $GameVersion

    $versions = Get-Content -Path $versionsPath -Raw | ConvertFrom-Json
    $entry = @($versions.versions | Where-Object { $_.game_version -eq $GameVersion })[0]

    & dotnet build (Join-Path $root 'HttpSiraStatus\HttpSiraStatus.csproj') `
        -c $Configuration `
        -p:Version=$($entry.mod_version) `
        -p:BeatSaberDir=$BeatSaberDir `
        -p:GameDirectory=$BeatSaberDir `
        -p:LocalRefsDir=$BeatSaberDir `
        -p:DisableCopyToPlugins=true

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }
}
finally {
    [System.IO.File]::WriteAllText(
        $manifestPath,
        $manifestOriginal,
        (New-Object System.Text.UTF8Encoding($false)))
}

