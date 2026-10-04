param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("Dynamic")]
    [string]$Linkage = "Dynamic",

    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [string]$OutputDirectory = $null,

    [string]$VtkDir = $env:VTK_DIR,

    [switch]$SkipNativeBuild
)

$ErrorActionPreference = "Stop"

if (-not $SkipNativeBuild -and [string]::IsNullOrWhiteSpace($VtkDir)) {
    throw "Set VTK_DIR to the installed VTK CMake package directory, or pass -VtkDir. See README.md."
}
if ($Configuration -ne "Release") {
    throw "NuGet packages are Release-only. Debug artifacts are reserved for local source development."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$bindingsDir = Join-Path (Join-Path $repoRoot "src") "bindings"

if ([string]::IsNullOrWhiteSpace($Version)) {
    # Hour is mapped to 1–24 range (0 o'clock → 24).
    $now = Get-Date
    $hour = if ($now.Hour -eq 0) { 24 } else { $now.Hour }
    $version = "$($now.ToString('yy.Mdd')).$hour$($now.ToString('mm'))"
}
else {
    $version = $Version
}

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot "artifacts\nuget\$version"
}

if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}
if (Get-ChildItem -LiteralPath $OutputDirectory -Filter "VtkSharp.$version.nupkg" -File -ErrorAction SilentlyContinue) {
    throw "Package version $version already exists. Wait for the next minute or use a new output/version before packaging."
}

# 1. Build native DLL (unless skipped)
if (-not $SkipNativeBuild) {
    Write-Host "=== Building native DLL ==="
    $buildNativeArgs = @{
        Configuration = $Configuration
        Linkage = $Linkage
    }
    if ($VtkDir) {
        $buildNativeArgs.VtkDir = $VtkDir
    }
    & "$PSScriptRoot/build-native.ps1" @buildNativeArgs
}
else {
    Write-Host "=== Skipping native build (--SkipNativeBuild) ==="
}

Write-Host "Package version: $version"

$runtimeDirectory = $null
if ($Linkage -eq "Dynamic") {
    if ([string]::IsNullOrWhiteSpace($VtkDir)) { throw "Dynamic NuGet packaging requires -VtkDir, including with -SkipNativeBuild." }
    $vtkInstallDirectory = [IO.Path]::GetFullPath((Join-Path $VtkDir "../../.."))
    $vtkBuildInfoPath = Join-Path $vtkInstallDirectory "vtk-build-info.json"
    if (-not (Test-Path -LiteralPath $vtkBuildInfoPath -PathType Leaf)) { throw "VTK build record is missing: $vtkBuildInfoPath" }
    $vtkBuildInfo = Get-Content -LiteralPath $vtkBuildInfoPath -Raw | ConvertFrom-Json
    if (-not $vtkBuildInfo.buildSharedLibs -or $vtkBuildInfo.configuration -ne "Release" -or $vtkBuildInfo.architecture -ne "x64") {
        throw "NuGet packaging requires a matching Shared Release x64 VTK installation: $VtkDir"
    }
    $nativeBuildRoot = Join-Path $bindingsDir "VtkSharp.Native\out\build\dynamic"
    $nativeOutputDirectory = @("win-x64-vs2026", "win-x64-vs2022") |
        ForEach-Object { Join-Path $nativeBuildRoot "$_\Release" } |
        Where-Object { Test-Path -LiteralPath (Join-Path $_ "native-dependencies.json") -PathType Leaf } |
        Sort-Object { (Get-Item (Join-Path $_ "native-dependencies.json")).LastWriteTimeUtc } -Descending |
        Select-Object -First 1
    if (-not $nativeOutputDirectory) { throw "Dynamic Release native output was not found. Build native first." }
    $manifestPath = Join-Path $nativeOutputDirectory "native-dependencies.json"
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.configuration -ne "Release" -or $manifest.architecture -ne "x64" -or $manifest.vtk.buildId -ne $vtkBuildInfo.buildId) {
        throw "Selected native output was built against a different VTK build. Rebuild native with the selected -VtkDir."
    }
    $licenseSource = Join-Path $vtkInstallDirectory "share\vtk-9.7\licenses"
    if (-not (Test-Path -LiteralPath (Join-Path $licenseSource "VTK-Copyright.txt") -PathType Leaf)) {
        throw "VTK license bundle is missing from the selected installation. Reinstall with tools/build-vtk-for-vtksharp.ps1."
    }
    $runtimeDirectory = Join-Path $repoRoot "artifacts\package-runtime\$version"
    if (Test-Path -LiteralPath $runtimeDirectory) { throw "Package runtime staging directory already exists: $runtimeDirectory" }
    $entrypointRoots = @($manifest.inputSources | ForEach-Object { "$($_.id)=$nativeOutputDirectory" })
    $sourceRoots = $entrypointRoots + "vtk=$(Join-Path $vtkInstallDirectory 'bin')"
    & "$PSScriptRoot/copy-native-dependencies.ps1" -ManifestPath $manifestPath -SourceRoot $sourceRoots -DestinationDirectory $runtimeDirectory
    $licenseDestination = Join-Path $runtimeDirectory "licenses"
    New-Item -ItemType Directory -Path $licenseDestination -Force | Out-Null
    Get-ChildItem -LiteralPath $licenseSource -Force | Copy-Item -Destination $licenseDestination -Recurse -Force
    $licenseRenames = [Collections.Generic.List[object]]::new()
    foreach ($licenseFile in (Get-ChildItem -LiteralPath $licenseDestination -File -Recurse | Where-Object { $_.Name -match '^(LICENSE|COPYING|COPYRIGHT|NOTICE)$' })) {
        $relativePath = [IO.Path]::GetRelativePath($licenseDestination, $licenseFile.FullName).Replace('\', '/')
        $renamedPath = "$($licenseFile.FullName).txt"
        Move-Item -LiteralPath $licenseFile.FullName -Destination $renamedPath
        $licenseRenames.Add([ordered]@{ originalPath = $relativePath; packagedPath = "$relativePath.txt" })
    }
    if ($licenseRenames.Count -gt 0) {
        $licenseRenames | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $licenseDestination "license-file-renames.json") -Encoding utf8
    }
}

# 2. Pack VtkSharp (core bindings)
Write-Host "`n=== Packing VtkSharp ==="
dotnet pack (Join-Path (Join-Path $bindingsDir "VtkSharp") "VtkSharp.csproj") `
    --configuration $Configuration `
    --output $OutputDirectory `
    -p:Version=$version `
    -p:IncludeSymbols=true `
    -p:SymbolPackageFormat=snupkg `
    "-p:VtkSharpNativeLinkage=$Linkage" `
    $(if ($runtimeDirectory) { "-p:VtkSharpNativeRuntimeDirectory=$runtimeDirectory" } else { "-p:VtkSharpNativeRuntimeDirectory=" })
if ($LASTEXITCODE -ne 0) {
    throw "VtkSharp pack failed."
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
