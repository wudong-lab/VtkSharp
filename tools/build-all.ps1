param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("Static", "Dynamic")]
    [string]$Linkage = "Static",

    [string]$VtkDir = $env:VTK_DIR,

    [switch]$SkipNativeBuild
)

$ErrorActionPreference = "Stop"

function Copy-VtkLicenseBundle {
    param(
        [string]$InstallDirectory,
        [string]$DestinationDirectory
    )

    $licenseCandidates = @(
        (Join-Path $InstallDirectory "share\vtk-9.7\licenses"),
        (Join-Path $InstallDirectory "share\licenses\VTK")
    )
    $licenseSource = $licenseCandidates | Where-Object {
        (Test-Path -LiteralPath (Join-Path $_ "VTK-Copyright.txt") -PathType Leaf) -or
        (Test-Path -LiteralPath (Join-Path $_ "Copyright.txt") -PathType Leaf)
    } | Select-Object -First 1

    if (-not $licenseSource) {
        throw "VTK license bundle is missing from the selected installation. Checked: $($licenseCandidates -join ', ')"
    }

    New-Item -ItemType Directory -Path $DestinationDirectory -Force | Out-Null
    Get-ChildItem -LiteralPath $licenseSource -Force | Copy-Item -Destination $DestinationDirectory -Recurse -Force
    $copyrightSource = Join-Path $DestinationDirectory "Copyright.txt"
    $copyrightDestination = Join-Path $DestinationDirectory "VTK-Copyright.txt"
    if ((Test-Path -LiteralPath $copyrightSource -PathType Leaf) -and -not (Test-Path -LiteralPath $copyrightDestination -PathType Leaf)) {
        Move-Item -LiteralPath $copyrightSource -Destination $copyrightDestination
    }
}

if ((-not $SkipNativeBuild -or $Linkage -eq "Dynamic") -and [string]::IsNullOrWhiteSpace($VtkDir)) {
    throw "Set VTK_DIR to the installed VTK CMake package directory, or pass -VtkDir. See README.md."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactsDir = Join-Path $repoRoot "artifacts\bin"
$artifactsDir = Join-Path $artifactsDir $Configuration

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
        $licenseDestination = Join-Path $outputDirectory "licenses\VTK"
        Copy-VtkLicenseBundle -InstallDirectory $vtkInstallDirectory -DestinationDirectory $licenseDestination

        & "$PSScriptRoot/copy-native-dependencies.ps1" `
            -ManifestPath (Join-Path $nativeOutputDirectory "native-dependencies.json") `
            -SourceRoot $sourceRoots `
            -DestinationDirectory $managedDirectory
    }
    else {
        & "$PSScriptRoot/remove-native-dependencies.ps1" -Directory $outputDirectory
        & "$PSScriptRoot/remove-native-dependencies.ps1" -Directory $managedDirectory
        & "$PSScriptRoot/remove-native-dependencies.ps1" -Directory (Join-Path $outputDirectory "native\VtkSharp\win-x64")
        & "$PSScriptRoot/remove-native-dependencies.ps1" -Directory (Join-Path $managedDirectory "native\VtkSharp\win-x64")
        if ($VtkDir) {
            $vtkInstallDirectory = [IO.Path]::GetFullPath((Join-Path $VtkDir "../../.."))
            $licenseDestination = Join-Path $outputDirectory "licenses\VTK"
            Copy-VtkLicenseBundle -InstallDirectory $vtkInstallDirectory -DestinationDirectory $licenseDestination
        }
    }
}

Write-Host "Build artifacts collected in: $artifactsDir"
