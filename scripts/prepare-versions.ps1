[CmdletBinding()]
param(
    [string]$VersionsPath = (Join-Path $PSScriptRoot '../versions.json'),
    [string]$OutputPath = $env:GITHUB_OUTPUT,
    [string]$Ref = $env:GITHUB_REF,
    [string]$RefName = $env:GITHUB_REF_NAME
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    throw 'GITHUB_OUTPUT is not set. Pass -OutputPath for local testing.'
}

$versions = (Get-Content -Path $VersionsPath -Raw | ConvertFrom-Json).versions
$isRelease = $Ref -like 'refs/tags/*'

if ($isRelease) {
    $selected = @($versions | Where-Object { $_.mod_version -eq $RefName })

    if ($selected.Count -ne 1) {
        throw "Tag '$RefName' is not defined in versions.json. Expected one of: $($versions.mod_version -join ', ')."
    }

    $entry = $selected[0]
    $matrix = @{ include = @($entry) }

    Add-Content -Path $OutputPath -Value 'is-release=true' -Encoding utf8
    Add-Content -Path $OutputPath -Value "game-version=$($entry.game_version)" -Encoding utf8
    Add-Content -Path $OutputPath -Value "mod-version=$($entry.mod_version)" -Encoding utf8
}
else {
    $matrix = @{ include = @($versions) }

    Add-Content -Path $OutputPath -Value 'is-release=false' -Encoding utf8
    Add-Content -Path $OutputPath -Value 'game-version=' -Encoding utf8
    Add-Content -Path $OutputPath -Value 'mod-version=' -Encoding utf8
}

$matrixJson = $matrix | ConvertTo-Json -Compress -Depth 5
Add-Content -Path $OutputPath -Value "matrix=$matrixJson" -Encoding utf8

Write-Output "is-release=$isRelease"
Write-Output "matrix=$matrixJson"

