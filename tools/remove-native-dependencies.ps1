#requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Directory)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$directoryPath = [IO.Path]::GetFullPath($Directory)
$manifestPath = Join-Path $directoryPath 'native-dependencies.json'
if (-not (Test-Path -LiteralPath $manifestPath)) { $manifestPath = Join-Path $directoryPath 'VtkSharp.native-dependencies.json' }
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { return }
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.schemaVersion -ne 1) { throw "Unsupported native dependency manifest schema: $($manifest.schemaVersion)" }
foreach ($file in $manifest.files) {
    $path = Join-Path $directoryPath $file.name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $hash = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($hash -eq $file.sha256) { Remove-Item -LiteralPath $path }
}
Remove-Item -LiteralPath $manifestPath
