param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$OutputDirectory = $null,

    [string]$VtkDir,

    [switch]$SkipNativeBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$bindingsDir = Join-Path $repoRoot "src" "bindings"

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot "artifacts"
}

if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}

# 1. Build native DLL (unless skipped)
if (-not $SkipNativeBuild) {
    Write-Host "=== Building native DLL ==="
    $buildNativeArgs = @("-Configuration", $Configuration)
    if ($VtkDir) {
        $buildNativeArgs += @("-VtkDir", $VtkDir)
    }
    & "$PSScriptRoot/build-native.ps1" @buildNativeArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Native build failed. Aborting pack."
    }
}
else {
    Write-Host "=== Skipping native build (--SkipNativeBuild) ==="
}

# 2. Pack VtkSharp (core bindings)
Write-Host "`n=== Packing VtkSharp ==="
dotnet pack (Join-Path $bindingsDir "VtkSharp" "VtkSharp.csproj") `
    --configuration $Configuration `
    --output $OutputDirectory `
    -p:IncludeSymbols=true `
    -p:SymbolPackageFormat=snupkg
if ($LASTEXITCODE -ne 0) {
    throw "VtkSharp pack failed."
}

# 3. Pack VtkSharp.Wpf
Write-Host "`n=== Packing VtkSharp.Wpf ==="
dotnet pack (Join-Path $bindingsDir "VtkSharp.Wpf" "VtkSharp.Wpf.csproj") `
    --configuration $Configuration `
    --output $OutputDirectory `
    -p:IncludeSymbols=true `
    -p:SymbolPackageFormat=snupkg
if ($LASTEXITCODE -ne 0) {
    throw "VtkSharp.Wpf pack failed."
}

# Summary
Write-Host "`n=== NuGet packages produced in: $OutputDirectory ==="
Get-ChildItem $OutputDirectory -Filter "*.nupkg" | ForEach-Object {
    $sizeKB = [math]::Round($_.Length / 1KB, 1)
    Write-Host "  $($_.Name) ($sizeKB KB)"
}
Get-ChildItem $OutputDirectory -Filter "*.snupkg" | ForEach-Object {
    $sizeKB = [math]::Round($_.Length / 1KB, 1)
    Write-Host "  $($_.Name) ($sizeKB KB)"
}
