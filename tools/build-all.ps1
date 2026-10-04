param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("Static", "Dynamic")]
    [string]$Linkage = "Dynamic",

    [string]$VtkDir = $env:VTK_DIR,

    [switch]$SkipNativeBuild
)

$ErrorActionPreference = "Stop"

if ((-not $SkipNativeBuild -or $Linkage -eq "Dynamic") -and [string]::IsNullOrWhiteSpace($VtkDir)) {
    throw "Set VTK_DIR to the installed VTK CMake package directory, or pass -VtkDir. See README.md."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactsDir = Join-Path $repoRoot "artifacts\bin"
if ($Linkage -eq "Dynamic") {
    $artifactsDir = Join-Path $artifactsDir "dynamic\$Configuration"
}

if (-not $SkipNativeBuild) {
    $buildNativeArgs = @{ Configuration = $Configuration; Linkage = $Linkage }
    if ($VtkDir) {
        $buildNativeArgs.VtkDir = $VtkDir
    }

    & "$PSScriptRoot/build-native.ps1" @buildNativeArgs
}

$vtkSharpProject = Join-Path $repoRoot "src\bindings\VtkSharp\VtkSharp.csproj"
$nativeBuildRoot = Join-Path $repoRoot "src\bindings\VtkSharp.Native\out\build"
$runtimeProperties = @("-p:VtkSharpNativeLinkage=$Linkage")
dotnet build $vtkSharpProject --configuration $Configuration @runtimeProperties
if ($LASTEXITCODE -ne 0) { throw "VtkSharp build failed." }

$linkageDirectory = if ($Linkage -eq "Dynamic") { "dynamic" } else { $null }
$nativeOutputDirectory = $null
$nativeCandidates = @()
foreach ($preset in @("win-x64-vs2026", "win-x64-vs2022")) {
    $candidatePath = if ($linkageDirectory) { Join-Path $nativeBuildRoot "$linkageDirectory\$preset\$Configuration" } else { Join-Path $nativeBuildRoot "$preset\$Configuration" }
    $candidateArtifact = if ($Linkage -eq "Dynamic") { Join-Path $candidatePath "native-dependencies.json" } else { Join-Path $candidatePath "VtkSharp.Native.dll" }
    if (Test-Path -LiteralPath $candidateArtifact -PathType Leaf) { $nativeCandidates += Get-Item -LiteralPath $candidateArtifact }
}
if ($nativeCandidates.Count -gt 0) {
    $nativeOutputDirectory = Split-Path -Parent (($nativeCandidates | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).FullName)
}
if ($Linkage -eq "Dynamic") {
    if (-not $nativeOutputDirectory) { throw "Dynamic native output directory for $Configuration was not found." }
    $manifest = Get-Content -LiteralPath (Join-Path $nativeOutputDirectory "native-dependencies.json") -Raw | ConvertFrom-Json
    $vtkInstallDirectory = [IO.Path]::GetFullPath((Join-Path $VtkDir "../../.."))
    $vtkBuildInfo = Get-Content -LiteralPath (Join-Path $vtkInstallDirectory "vtk-build-info.json") -Raw | ConvertFrom-Json
    if ($manifest.configuration -ne $Configuration -or $manifest.architecture -ne "x64" -or $manifest.vtk.buildId -ne $vtkBuildInfo.buildId) {
        throw "Selected native output was built against a different VTK build. Rebuild native with the selected -VtkDir."
    }
    $runtimeProperties += "-p:VtkSharpNativeRuntimeDirectory=$nativeOutputDirectory"
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

    if ($Linkage -eq "Dynamic") {
        if (-not $nativeOutputDirectory) { throw "Native output directory for $Linkage VTK was not found." }
        $vtkInstallDirectory = [IO.Path]::GetFullPath((Join-Path $VtkDir "../../.."))
        $entrypointRoots = @($manifest.inputSources | ForEach-Object { "$($_.id)=$nativeOutputDirectory" })
        $sourceRoots = $entrypointRoots + "vtk=$(Join-Path $vtkInstallDirectory 'bin')"
        & "$PSScriptRoot/copy-native-dependencies.ps1" `
            -ManifestPath (Join-Path $nativeOutputDirectory "native-dependencies.json") `
            -SourceRoot $sourceRoots `
            -DestinationDirectory $outputDirectory
        $licenseSource = Join-Path $vtkInstallDirectory "share\vtk-9.7\licenses"
        if (-not (Test-Path -LiteralPath (Join-Path $licenseSource "VTK-Copyright.txt") -PathType Leaf)) {
            throw "VTK license bundle is missing from the selected installation: $licenseSource"
        }
        $licenseDestination = Join-Path $outputDirectory "licenses\VTK"
        New-Item -ItemType Directory -Path $licenseDestination -Force | Out-Null
        Get-ChildItem -LiteralPath $licenseSource -Force | Copy-Item -Destination $licenseDestination -Recurse -Force

        & "$PSScriptRoot/copy-native-dependencies.ps1" `
            -ManifestPath (Join-Path $nativeOutputDirectory "native-dependencies.json") `
            -SourceRoot $sourceRoots `
            -DestinationDirectory $managedDirectory
    }
    else {
        & "$PSScriptRoot/remove-native-dependencies.ps1" -Directory $outputDirectory
        & "$PSScriptRoot/remove-native-dependencies.ps1" -Directory $managedDirectory
    }
}

Get-ChildItem -Recurse -File $artifactsDir | Sort-Object FullName
