#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string[]]$SourceRoot,
    [Parameter(Mandatory)][string]$DestinationDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$manifestFile = [IO.Path]::GetFullPath($ManifestPath)
$manifest = Get-Content -LiteralPath $manifestFile -Raw | ConvertFrom-Json
if ($manifest.schemaVersion -ne 1) { throw "Unsupported native dependency manifest schema: $($manifest.schemaVersion)" }

$roots = @{}
foreach ($item in $SourceRoot) {
    $separator = $item.IndexOf('=')
    if ($separator -lt 1) { throw "SourceRoot must use id=path syntax: $item" }
    $id = $item.Substring(0, $separator)
    $path = [IO.Path]::GetFullPath($item.Substring($separator + 1))
    if (-not (Test-Path -LiteralPath $path -PathType Container)) { throw "Source root does not exist: $path" }
    $roots[$id] = $path
}

$destination = [IO.Path]::GetFullPath($DestinationDirectory)
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$newNames = @($manifest.files | ForEach-Object { $_.name })
$previousHashes = @{}
$previousManifestPath = Join-Path $destination 'native-dependencies.json'
if (Test-Path -LiteralPath $previousManifestPath -PathType Leaf) {
    $previous = Get-Content -LiteralPath $previousManifestPath -Raw | ConvertFrom-Json
    foreach ($oldFile in $previous.files) {
        $previousHashes[$oldFile.name] = ([string]$oldFile.sha256).ToLowerInvariant()
        if ($newNames -contains $oldFile.name) { continue }
        $oldPath = Join-Path $destination $oldFile.name
        if (Test-Path -LiteralPath $oldPath -PathType Leaf) {
            $oldHash = (Get-FileHash -LiteralPath $oldPath -Algorithm SHA256).Hash.ToLowerInvariant()
            if ($oldHash -eq $oldFile.sha256) { Remove-Item -LiteralPath $oldPath }
        }
    }
}
foreach ($file in $manifest.files) {
    if (-not $roots.ContainsKey($file.sourceRoot)) { throw "No source directory was provided for root '$($file.sourceRoot)'" }
    $source = [IO.Path]::GetFullPath((Join-Path $roots[$file.sourceRoot] $file.relativePath))
    if (-not $source.StartsWith($roots[$file.sourceRoot] + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and $source -ne $roots[$file.sourceRoot]) {
        throw "Manifest path escapes source root '$($file.sourceRoot)': $($file.relativePath)"
    }
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Manifest source file is missing: $source" }
    $hash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($hash -ne $file.sha256) { throw "Manifest is stale: SHA-256 changed for $($file.name). Rebuild dependencies." }
    $target = Join-Path $destination $file.name
    if (Test-Path -LiteralPath $target -PathType Leaf) {
        $targetHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant()
        $previousHash = $previousHashes[$file.name]
        if ($targetHash -ne $file.sha256 -and ($null -eq $previousHash -or $targetHash -ne $previousHash)) {
            throw "Refusing to overwrite a different file with the same name: $target"
        }
    }
    if ($source -ne [IO.Path]::GetFullPath($target)) {
        Copy-Item -LiteralPath $source -Destination $target -Force
    }
}
$targetManifest = [IO.Path]::GetFullPath((Join-Path $destination 'native-dependencies.json'))
if ($manifestFile -ne $targetManifest) { Copy-Item -LiteralPath $manifestFile -Destination $targetManifest -Force }
Write-Host "Copied $($manifest.files.Count) native files to $destination"
