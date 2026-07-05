param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$VtkDir,

    [switch]$SkipNativeBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactsDir = Join-Path $repoRoot "artifacts\bin"

# Clean and recreate artifacts directory
if (Test-Path $artifactsDir) {
    Remove-Item -Recurse -Force $artifactsDir
}

# ──────────────────────────────────────────────
# 1. Build native DLLs
# ──────────────────────────────────────────────
if (-not $SkipNativeBuild) {
    Write-Host "=== Building native DLLs ===" -ForegroundColor Cyan
    $buildNativeArgs = @{
        Configuration = $Configuration
    }
    if ($VtkDir) {
        $buildNativeArgs.VtkDir = $VtkDir
    }
    & "$PSScriptRoot/build-native.ps1" @buildNativeArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Native build failed."
    }
}
else {
    Write-Host "=== Skipping native build (--SkipNativeBuild) ===" -ForegroundColor Yellow
}

# ──────────────────────────────────────────────
# 2. Build managed projects
# ──────────────────────────────────────────────
$vtkSharpProj    = Join-Path $repoRoot "src\bindings\VtkSharp\VtkSharp.csproj"
$vtkSharpWpfProj = Join-Path $repoRoot "src\bindings\VtkSharp.Wpf\VtkSharp.Wpf.csproj"

Write-Host "`n=== Building VtkSharp ($Configuration) ===" -ForegroundColor Cyan
dotnet build $vtkSharpProj --configuration $Configuration
if ($LASTEXITCODE -ne 0) { throw "VtkSharp build failed." }

Write-Host "`n=== Building VtkSharp.Wpf ($Configuration) ===" -ForegroundColor Cyan
dotnet build $vtkSharpWpfProj --configuration $Configuration
if ($LASTEXITCODE -ne 0) { throw "VtkSharp.Wpf build failed." }

# ──────────────────────────────────────────────
# 3. Locate native DLLs
# ──────────────────────────────────────────────
function Find-NativeOutputDir {
    param([string]$SubPath)
    $nativeDir = Join-Path $repoRoot "src\bindings\VtkSharp.Native"
    foreach ($preset in @("win-x64-vs2026", "win-x64-vs2022")) {
        $candidate = Join-Path $nativeDir "out\build\$preset\$SubPath"
        if (Test-Path $candidate) { return $candidate }
    }
    return $null
}

$nativeMainDir = Find-NativeOutputDir -SubPath $Configuration
$wpfNativeDir  = Find-NativeOutputDir -SubPath "VtkSharp.Wpf.Native\$Configuration"

# Also check managed output dirs — native DLLs are copied there by
# CopyToOutputDirectory in the .csproj files when native builds first.
function Get-NativeDll {
    param([string]$FileName, [string]$NativeDir, [string]$FallbackManagedDir)
    if ($NativeDir) {
        $path = Join-Path $NativeDir $FileName
        if (Test-Path $path) { return $path }
    }
    if ($FallbackManagedDir) {
        $path = Join-Path $FallbackManagedDir $FileName
        if (Test-Path $path) { return $path }
    }
    return $null
}

# ──────────────────────────────────────────────
# 4. Copy artifacts by target framework
# ──────────────────────────────────────────────
$targets = @(
    @{
        Label      = "net48"
        ManagedSrc = @(
            # VtkSharp → netstandard2.0 (compatible with net48)
            @{ SrcDir = "src\bindings\VtkSharp\bin\$Configuration\netstandard2.0";     Patterns = @("VtkSharp.dll", "VtkSharp.pdb") }
            # VtkSharp.Wpf → net48
            @{ SrcDir = "src\bindings\VtkSharp.Wpf\bin\$Configuration\net48";          Patterns = @("VtkSharp.Wpf.dll", "VtkSharp.Wpf.pdb") }
        )
    },
    @{
        Label      = "net8.0-windows"
        ManagedSrc = @(
            # VtkSharp → net8.0
            @{ SrcDir = "src\bindings\VtkSharp\bin\$Configuration\net8.0";             Patterns = @("VtkSharp.dll", "VtkSharp.pdb") }
            # VtkSharp.Wpf → net8.0-windows
            @{ SrcDir = "src\bindings\VtkSharp.Wpf\bin\$Configuration\net8.0-windows"; Patterns = @("VtkSharp.Wpf.dll", "VtkSharp.Wpf.pdb") }
        )
    }
)

# The native DLLs each framework directory should contain
$nativeDllList = @("VtkSharp.Native.dll", "VtkSharp.Wpf.Native.dll")
if ($Configuration -eq "Debug") {
    $nativeDllList += @("VtkSharp.Native.pdb", "VtkSharp.Wpf.Native.pdb")
}

foreach ($target in $targets) {
    $outDir = Join-Path $artifactsDir $target.Label
    Write-Host "`n  Assembling: $($target.Label)" -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null

    # Copy managed assemblies
    foreach ($src in $target.ManagedSrc) {
        $srcPath = Join-Path $repoRoot $src.SrcDir
        if (-not (Test-Path $srcPath)) {
            Write-Warning "    [MISSING] $srcPath"
            continue
        }
        foreach ($pattern in $src.Patterns) {
            $filePath = Join-Path $srcPath $pattern
            if (Test-Path $filePath) {
                Copy-Item $filePath -Destination $outDir -Force
                $sizeKB = [math]::Round((Get-Item $filePath).Length / 1KB, 1)
                Write-Host "    $pattern  ($sizeKB KB)"
            }
            else {
                Write-Warning "    [MISSING] $pattern in $srcPath"
            }
        }
    }

    # Copy native DLLs
    # VtkSharp.Wpf's net48 output dir has VtkSharp.dll from the ProjectReference,
    # which is where VtkSharp.Native.dll gets resolved at runtime.  Use the managed
    # output as fallback for native DLLs in case CopyToOutputDirectory already placed
    # them there.
    $firstManagedDir = Join-Path $repoRoot $target.ManagedSrc[0].SrcDir

    foreach ($file in $nativeDllList) {
        if ($file -like "VtkSharp.Wpf.Native.*") {
            $nativeRoot = $wpfNativeDir
        }
        else {
            $nativeRoot = $nativeMainDir
        }
        $srcPath = Get-NativeDll -FileName $file -NativeDir $nativeRoot -FallbackManagedDir $firstManagedDir
        if ($srcPath) {
            Copy-Item $srcPath -Destination $outDir -Force
            $sizeKB = [math]::Round((Get-Item $srcPath).Length / 1KB, 1)
            Write-Host "    $file  ($sizeKB KB)"
        }
        else {
            Write-Warning "    [MISSING] $file"
        }
    }
}

# ──────────────────────────────────────────────
# Summary
# ──────────────────────────────────────────────
Write-Host "`n=== Done. Artifacts ($Configuration) ===" -ForegroundColor Green
Get-ChildItem -Recurse -File $artifactsDir | Sort-Object FullName | ForEach-Object {
    $relPath = $_.FullName.Substring($artifactsDir.Length + 1)
    $sizeKB = [math]::Round($_.Length / 1KB, 1)
    Write-Host "  $relPath  ($sizeKB KB)"
}
