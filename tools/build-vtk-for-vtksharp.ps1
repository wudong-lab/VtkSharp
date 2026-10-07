<#
.SYNOPSIS
为 VtkSharp 配置、构建并安装 VTK 9.7.0。

.DESCRIPTION
使用 Visual Studio 2026、x64、静态或动态 VTK 库和动态 MSVC CRT。默认使用静态 VTK，执行 Release
配置、构建和安装；VTK 安装目录同时供 VtkSharp 生成器和 native CMake package 使用。

.EXAMPLE
.\tools\build-vtk-for-vtksharp.ps1 -Fresh

.EXAMPLE
.\tools\build-vtk-for-vtksharp.ps1 -Configuration Both -Parallel 16

.EXAMPLE
.\tools\build-vtk-for-vtksharp.ps1 -Action Configure -SourceDirectory D:\Dependencies\VTK\source -BuildDirectory D:\Dependencies\VTK\build
#>
[CmdletBinding()]
param(
    [ValidateSet("Configure", "Build", "Install", "All")]
    [string]$Action = "All",

    [ValidateSet("Debug", "Release", "Both")]
    [string]$Configuration = "Release",

    [ValidateSet("Static", "Shared")]
    [string]$Linkage = "Static",

    [string]$SourceDirectory = (Join-Path $PSScriptRoot "..\..\..\VTK\VtkGitSource"),

    [string]$BuildDirectory,

    [string]$InstallDirectory,

    [ValidateRange(1, 256)]
    [int]$Parallel = [Environment]::ProcessorCount,

    [switch]$Fresh
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$expectedVtkVersion = "9.7.0"
$generator = "Visual Studio 18 2026"
$architecture = "x64"

$SourceDirectory = [IO.Path]::GetFullPath($SourceDirectory)
if ([string]::IsNullOrWhiteSpace($BuildDirectory)) {
    $buildName = if ($Linkage -eq "Shared") { "VtkGitBuild-shared" } else { "VtkGitBuild" }
    $BuildDirectory = Join-Path $PSScriptRoot "..\..\..\VTK\$buildName"
}
$BuildDirectory = [IO.Path]::GetFullPath($BuildDirectory)
if ([string]::IsNullOrWhiteSpace($InstallDirectory)) {
    $InstallDirectory = Join-Path $BuildDirectory "install"
}
$InstallDirectory = [IO.Path]::GetFullPath($InstallDirectory)

function Invoke-CheckedCommand {
    param(
        [Parameter(Mandatory)]
        [string]$Command,

        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    Write-Host "> $Command $($Arguments -join ' ')" -ForegroundColor DarkGray
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $Command"
    }
}

function Get-VtkSourceVersion {
    param([Parameter(Mandatory)][string]$SourceRoot)

    $versionFile = Join-Path $SourceRoot "CMake\vtkVersion.cmake"
    if (-not (Test-Path -LiteralPath $versionFile -PathType Leaf)) {
        throw "VTK version file was not found: $versionFile"
    }

    $content = Get-Content -LiteralPath $versionFile -Raw
    $parts = foreach ($name in @("MAJOR", "MINOR", "BUILD")) {
        $match = [regex]::Match($content, "set\(VTK_${name}_VERSION\s+(\d+)\)")
        if (-not $match.Success) {
            throw "Cannot read VTK_${name}_VERSION from $versionFile"
        }
        $match.Groups[1].Value
    }

    return $parts -join "."
}

if (-not (Get-Command cmake -ErrorAction SilentlyContinue)) {
    throw "CMake was not found in PATH."
}
if (-not (Test-Path -LiteralPath $SourceDirectory -PathType Container)) {
    throw "VTK source directory was not found: $SourceDirectory"
}

$actualVtkVersion = Get-VtkSourceVersion -SourceRoot $SourceDirectory
if ($actualVtkVersion -ne $expectedVtkVersion) {
    throw "VtkSharp requires VTK $expectedVtkVersion, but the source tree is VTK $actualVtkVersion."
}

$cmakeHelp = & cmake --help 2>&1 | Out-String
if ($LASTEXITCODE -ne 0 -or $cmakeHelp -notmatch [regex]::Escape($generator)) {
    throw "$generator is not available. Install Visual Studio 2026 with Desktop development with C++."
}

$requiredModules = @(
    "CommonColor",
    "CommonComputationalGeometry",
    "CommonCore",
    "CommonDataModel",
    "CommonExecutionModel",
    "CommonMath",
    "CommonTransforms",
    "FiltersCore",
    "FiltersGeneral",
    "FiltersModeling",
    "FiltersSources",
    "IOImage",
    "ImagingCore",
    "ImagingMath",
    "InteractionImage",
    "InteractionStyle",
    "InteractionWidgets",
    "RenderingAnnotation",
    "RenderingCore",
    "RenderingLabel",
    "RenderingOpenGL2",
    "RenderingUI"
)

$configureArguments = @(
    "-S", $SourceDirectory,
    "-B", $BuildDirectory,
    "-G", $generator,
    "-A", $architecture,
    "-DCMAKE_INSTALL_PREFIX:PATH=$($InstallDirectory.Replace('\', '/'))",
    '-DCMAKE_MSVC_RUNTIME_LIBRARY:STRING=MultiThreaded$<$<CONFIG:Debug>:Debug>DLL',
    "-DBUILD_SHARED_LIBS:BOOL=$(if ($Linkage -eq 'Shared') { 'ON' } else { 'OFF' })",
    "-DVTK_BUILD_ALL_MODULES:BOOL=OFF",
    "-DVTK_GROUP_ENABLE_StandAlone:STRING=WANT",
    "-DVTK_GROUP_ENABLE_Rendering:STRING=WANT",
    "-DVTK_GROUP_ENABLE_Imaging:STRING=WANT",
    "-DVTK_GROUP_ENABLE_Views:STRING=WANT",
    "-DVTK_GROUP_ENABLE_Qt:STRING=NO",
    "-DVTK_GROUP_ENABLE_MPI:STRING=NO",
    "-DVTK_GROUP_ENABLE_Web:STRING=NO",
    "-DVTK_GROUP_ENABLE_Tk:STRING=NO",
    "-DVTK_USE_MPI:BOOL=OFF",
    "-DVTK_USE_CUDA:BOOL=OFF",
    "-DVTK_USE_HIP:BOOL=OFF",
    "-DVTK_USE_KOKKOS:BOOL=OFF",
    "-DVTK_ENABLE_WEBGPU:BOOL=OFF",
    "-DVTK_USE_TK:BOOL=OFF",
    "-DVTK_WRAP_JAVASCRIPT:BOOL=OFF",
    "-DVTK_WRAP_PYTHON:BOOL=OFF",
    "-DVTK_WRAP_JAVA:BOOL=OFF",
    "-DVTK_WRAP_SERIALIZATION:BOOL=OFF",
    "-DVTK_ENABLE_REMOTE_MODULES:BOOL=OFF",
    "-DVTK_BUILD_TESTING:STRING=OFF",
    "-DVTK_BUILD_EXAMPLES:BOOL=OFF",
    "-DVTK_BUILD_DOCUMENTATION:BOOL=OFF",
    "-DVTK_ENABLE_WRAPPING:BOOL=ON",
    "-DVTK_ENABLE_KITS:BOOL=OFF",
    "-DVTK_SMP_IMPLEMENTATION_TYPE:STRING=STDThread"
)

foreach ($module in $requiredModules) {
    $configureArguments += "-DVTK_MODULE_ENABLE_VTK_${module}:STRING=YES"
}

$cacheFile = Join-Path $BuildDirectory "CMakeCache.txt"
if ((Test-Path -LiteralPath $cacheFile -PathType Leaf) -and -not $Fresh) {
    $cachedGenerator = Get-Content -LiteralPath $cacheFile |
        Where-Object { $_ -like "CMAKE_GENERATOR:INTERNAL=*" } |
        Select-Object -First 1
    if ($cachedGenerator -and $cachedGenerator.Split("=", 2)[1] -ne $generator) {
        throw "The build directory uses another CMake generator. Re-run with -Fresh or use a new -BuildDirectory: $BuildDirectory"
    }
    $expectedShared = if ($Linkage -eq "Shared") { "ON" } else { "OFF" }
    $cachedShared = Get-Content -LiteralPath $cacheFile |
        Where-Object { $_ -like "BUILD_SHARED_LIBS:BOOL=*" } |
        Select-Object -First 1
    if ($cachedShared -and $cachedShared.Split("=", 2)[1] -ne $expectedShared) {
        throw "The build directory uses BUILD_SHARED_LIBS=$($cachedShared.Split('=', 2)[1]), but -Linkage $Linkage requires $expectedShared. Use a separate directory or -Fresh: $BuildDirectory"
    }
}

if ($Fresh) {
    $configureArguments = @("--fresh") + $configureArguments
}

if ($Action -in @("Configure", "All")) {
    Write-Host "Configuring VTK $actualVtkVersion for VtkSharp..." -ForegroundColor Cyan
    Invoke-CheckedCommand -Command "cmake" -Arguments $configureArguments
}

$configurations = if ($Configuration -eq "Both") {
    @("Release", "Debug")
}
else {
    @($Configuration)
}

if ($Action -in @("Build", "All")) {
    foreach ($item in $configurations) {
        Write-Host "Building VTK ($item)..." -ForegroundColor Cyan
        Invoke-CheckedCommand -Command "cmake" -Arguments @(
            "--build", $BuildDirectory,
            "--config", $item,
            "--parallel", $Parallel.ToString()
        )
    }
}

if ($Action -in @("Install", "All")) {
    foreach ($item in $configurations) {
        $configurationInstallDirectory = if ($Linkage -eq "Shared") { Join-Path $InstallDirectory $item } else { $InstallDirectory }
        Write-Host "Installing VTK ($item) to $configurationInstallDirectory..." -ForegroundColor Cyan
        Invoke-CheckedCommand -Command "cmake" -Arguments @(
            "--install", $BuildDirectory,
            "--config", $item,
            "--prefix", $configurationInstallDirectory
        )

        $vtkPackageDirectory = Join-Path $configurationInstallDirectory "lib\cmake\vtk-9.7"
        $hierarchyDirectory = Join-Path $configurationInstallDirectory "lib\vtk-9.7\hierarchy\VTK"
        if (-not (Test-Path -LiteralPath (Join-Path $vtkPackageDirectory "vtk-config.cmake") -PathType Leaf)) {
            throw "VTK CMake package was not installed: $vtkPackageDirectory"
        }
        if (-not (Test-Path -LiteralPath (Join-Path $hierarchyDirectory "vtkCommonCore-hierarchy.txt") -PathType Leaf)) {
            throw "VTK hierarchy files required by VtkSharp.Generator were not installed: $hierarchyDirectory"
        }

        $licenseDirectory = Join-Path $configurationInstallDirectory "share\vtk-9.7\licenses"
        New-Item -ItemType Directory -Path $licenseDirectory -Force | Out-Null
        Copy-Item -LiteralPath (Join-Path $SourceDirectory "Copyright.txt") -Destination (Join-Path $licenseDirectory "VTK-Copyright.txt") -Force
        $thirdPartyLicenseFiles = @(Get-ChildItem -LiteralPath (Join-Path $SourceDirectory "ThirdParty") -File -Recurse |
            Where-Object { $_.Name -match '^(LICENSE|COPYING|COPYRIGHT|NOTICE)(\..*)?$' })
        foreach ($license in $thirdPartyLicenseFiles) {
            $relativeLicensePath = [IO.Path]::GetRelativePath((Join-Path $SourceDirectory "ThirdParty"), $license.FullName)
            $destination = Join-Path $licenseDirectory (Join-Path "ThirdParty" $relativeLicensePath)
            New-Item -ItemType Directory -Path (Split-Path -Parent $destination) -Force | Out-Null
            Copy-Item -LiteralPath $license.FullName -Destination $destination -Force
        }

        $sourceCommit = (& git -C $SourceDirectory rev-parse HEAD 2>$null)
        if ($LASTEXITCODE -ne 0) { $sourceCommit = "unknown" }
        $compilerInfoFile = Get-ChildItem -LiteralPath (Join-Path $BuildDirectory "CMakeFiles") -Filter "CMakeCXXCompiler.cmake" -Recurse | Select-Object -First 1
        $compilerMatch = if ($compilerInfoFile) { [regex]::Match((Get-Content -LiteralPath $compilerInfoFile.FullName -Raw), 'set\(CMAKE_CXX_COMPILER_VERSION\s+"([^"]+)"\)') } else { $null }
        $compilerVersion = if ($compilerMatch -and $compilerMatch.Success) { $compilerMatch.Groups[1].Value } else { "unknown" }
        $buildInfo = [ordered]@{
            schemaVersion = 1
            vtkVersion = $actualVtkVersion
            vtkSourceCommit = $sourceCommit.Trim()
            linkage = $Linkage
            buildSharedLibs = ($Linkage -eq "Shared")
            architecture = $architecture
            generator = $generator
            compiler = "MSVC $compilerVersion"
            msvcRuntimeLibrary = "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
            configuration = $item
            licenseFileCount = $thirdPartyLicenseFiles.Count + 1
            thirdPartyLicenseFiles = @($thirdPartyLicenseFiles | ForEach-Object { [IO.Path]::GetRelativePath((Join-Path $SourceDirectory "ThirdParty"), $_.FullName).Replace('\', '/') } | Sort-Object)
            buildOptions = [ordered]@{
                sharedLibraries = ($Linkage -eq "Shared")
                vtkBuildAllModules = $false
                enabledModules = @($requiredModules)
                moduleGroups = @{ StandAlone = "WANT"; Rendering = "WANT"; Imaging = "WANT"; Views = "WANT"; Qt = "NO"; MPI = "NO"; Web = "NO"; Tk = "NO" }
                wrapping = $true
                kits = $false
                smpImplementation = "STDThread"
            }
        }
        $identityText = @($buildInfo.vtkVersion, $buildInfo.vtkSourceCommit, $buildInfo.linkage, $item, $architecture, $generator, $compilerVersion, $buildInfo.msvcRuntimeLibrary, ($requiredModules -join ',')) -join "|"
        $buildInfo.buildId = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($identityText))).ToLowerInvariant()
        $buildInfoPath = Join-Path $configurationInstallDirectory "vtk-build-info.json"
        $buildInfo | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $buildInfoPath -Encoding utf8
        Write-Host "VTK build record: $buildInfoPath"
    }
}

Write-Host "VTK action '$Action' completed." -ForegroundColor Green
if ($Linkage -eq "Shared" -and $Configuration -eq "Both") {
    Write-Host "VTK_INSTALL_DIRECTORY=$($InstallDirectory.Replace('\', '/'))"
    Write-Host "VTK_ROOT_RELEASE=$($InstallDirectory.Replace('\', '/'))/Release"
    Write-Host "VTK_ROOT_DEBUG=$($InstallDirectory.Replace('\', '/'))/Debug"
    Write-Host "VTK_DIR_RELEASE=$($InstallDirectory.Replace('\', '/'))/Release/lib/cmake/vtk-9.7"
    Write-Host "VTK_DIR_DEBUG=$($InstallDirectory.Replace('\', '/'))/Debug/lib/cmake/vtk-9.7"
}
else {
    $configurationPath = if ($Linkage -eq "Shared") { "/$Configuration" } else { "" }
    Write-Host "VTK_ROOT=$($InstallDirectory.Replace('\', '/'))$configurationPath"
    Write-Host "VTK_DIR=$($InstallDirectory.Replace('\', '/'))$configurationPath/lib/cmake/vtk-9.7"
}
Write-Host "BUILD_SHARED_LIBS=$(if ($Linkage -eq 'Shared') { 'ON' } else { 'OFF' })"
