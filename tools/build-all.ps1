param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$VtkDir = $env:VTK_DIR,

    [switch]$SkipNativeBuild
)

$ErrorActionPreference = "Stop"

if (-not $SkipNativeBuild -and [string]::IsNullOrWhiteSpace($VtkDir)) {
    throw "Set VTK_DIR to the installed VTK CMake package directory, or pass -VtkDir. See README.md."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactsDir = Join-Path $repoRoot "artifacts\bin"

if (Test-Path $artifactsDir) {
    Remove-Item -Recurse -Force $artifactsDir
}

if (-not $SkipNativeBuild) {
    $buildNativeArgs = @{ Configuration = $Configuration }
    if ($VtkDir) {
        $buildNativeArgs.VtkDir = $VtkDir
    }

    & "$PSScriptRoot/build-native.ps1" @buildNativeArgs
    if ($LASTEXITCODE -ne 0) { throw "Native build failed." }
}

$vtkSharpProject = Join-Path $repoRoot "src\bindings\VtkSharp\VtkSharp.csproj"
dotnet build $vtkSharpProject --configuration $Configuration
if ($LASTEXITCODE -ne 0) { throw "VtkSharp build failed." }

$nativeBuildRoot = Join-Path $repoRoot "src\bindings\VtkSharp.Native\out\build"
$nativeOutputDirectory = $null
foreach ($preset in @("win-x64-vs2026", "win-x64-vs2022")) {
    $candidate = Join-Path $nativeBuildRoot "$preset\$Configuration"
    if (Test-Path $candidate) {
        $nativeOutputDirectory = $candidate
        break
    }
}

$targets = @(
    @{ Label = "netstandard2.0"; ManagedDirectory = "src\bindings\VtkSharp\bin\$Configuration\netstandard2.0" },
    @{ Label = "net8.0"; ManagedDirectory = "src\bindings\VtkSharp\bin\$Configuration\net8.0" }
)

foreach ($target in $targets) {
    $outputDirectory = Join-Path $artifactsDir $target.Label
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

    $managedDirectory = Join-Path $repoRoot $target.ManagedDirectory
    foreach ($fileName in @("VtkSharp.dll", "VtkSharp.pdb", "VtkSharp.xml")) {
        $source = Join-Path $managedDirectory $fileName
        if (Test-Path $source) {
            Copy-Item $source -Destination $outputDirectory -Force
        }
    }

    $nativeFiles = @("VtkSharp.Native.dll")
    if ($Configuration -eq "Debug") {
        $nativeFiles += "VtkSharp.Native.pdb"
    }

    foreach ($fileName in $nativeFiles) {
        $source = if ($nativeOutputDirectory) { Join-Path $nativeOutputDirectory $fileName } else { $null }
        if ($source -and (Test-Path $source)) {
            Copy-Item $source -Destination $outputDirectory -Force
        }
        else {
            Write-Warning "[MISSING] $fileName"
        }
    }
}

Get-ChildItem -Recurse -File $artifactsDir | Sort-Object FullName
