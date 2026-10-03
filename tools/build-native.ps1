param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateSet("Static", "Dynamic")]
    [string]$Linkage = "Dynamic",

    [string]$VtkDir = $env:VTK_DIR
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$nativeDir = Join-Path $repoRoot "src\bindings\VtkSharp.Native"

if ([string]::IsNullOrWhiteSpace($VtkDir)) {
    throw "Set VTK_DIR to the installed VTK CMake package directory, or pass -VtkDir. See README.md."
}

if ($VtkDir) {
    $VtkDir = [IO.Path]::GetFullPath($VtkDir)
    $vtkConfigFiles = @("vtk-config.cmake", "VTKConfig.cmake")
    if (-not ($vtkConfigFiles | Where-Object { Test-Path -LiteralPath (Join-Path $VtkDir $_) -PathType Leaf })) {
        throw "VTK CMake package was not found in: $VtkDir. Build and install VTK first, or pass a valid -VtkDir."
    }
}

$vtkInstallDirectory = $null
$vtkBuildInfo = $null
if ($Linkage -eq "Dynamic") {
    $vtkInstallDirectory = [IO.Path]::GetFullPath((Join-Path $VtkDir "../../.."))
    $vtkBuildInfoPath = Join-Path $vtkInstallDirectory "vtk-build-info.json"
    if (-not (Test-Path -LiteralPath $vtkBuildInfoPath -PathType Leaf)) {
        throw "Dynamic VTK build record is missing: $vtkBuildInfoPath. Reinstall VTK with tools/build-vtk-for-vtksharp.ps1."
    }
    $vtkBuildInfo = Get-Content -LiteralPath $vtkBuildInfoPath -Raw | ConvertFrom-Json
    if (-not $vtkBuildInfo.buildSharedLibs -or $vtkBuildInfo.linkage -ne "Shared") {
        throw "VTK_DIR does not reference a shared VTK installation: $VtkDir"
    }
    if ($vtkBuildInfo.configuration -ne $Configuration -or $vtkBuildInfo.architecture -ne "x64") {
        throw "VTK configuration/architecture does not match native build: requested $Configuration x64; found $($vtkBuildInfo.configuration) $($vtkBuildInfo.architecture)."
    }
}

$suffix = if ($Linkage -eq "Dynamic") { "-dynamic" } else { "" }
$configurationSuffix = $Configuration.ToLowerInvariant()
$candidates = @(
    @{ Name = "Visual Studio 2026"; ConfigurePreset = "win-x64-vs2026$suffix"; BuildPreset = "win-x64-vs2026$suffix-$configurationSuffix"; BinaryDirectory = $(if ($Linkage -eq "Dynamic") { "dynamic\win-x64-vs2026" } else { "win-x64-vs2026" }) },
    @{ Name = "Visual Studio 2022"; ConfigurePreset = "win-x64-vs2022$suffix"; BuildPreset = "win-x64-vs2022$suffix-$configurationSuffix"; BinaryDirectory = $(if ($Linkage -eq "Dynamic") { "dynamic\win-x64-vs2022" } else { "win-x64-vs2022" }) }
)

function Invoke-CMakeConfigure {
    param(
        [string]$Preset,
        [bool]$Fresh
    )

    $arguments = @("--preset", $Preset)
    if ($Fresh) {
        $arguments += "--fresh"
    }

    if ($VtkDir) {
        $arguments += "-DVTK_DIR=$VtkDir"
    }

    $output = & cmake @arguments 2>&1
    $exitCode = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }

    $outputText = $output | Out-String
    return [pscustomobject]@{
        ExitCode = $exitCode
        GeneratorUnavailable = $outputText -match "could not find any instance of Visual Studio|Could not create named generator"
    }
}

Push-Location $nativeDir
try {
    foreach ($candidate in $candidates) {
        Write-Host "Configuring native project with $($candidate.Name)..."

        $result = Invoke-CMakeConfigure -Preset $candidate.ConfigurePreset -Fresh $false
        if ($result.ExitCode -ne 0) {
            Write-Host "Retrying $($candidate.Name) with a fresh CMake cache..."
            $result = Invoke-CMakeConfigure -Preset $candidate.ConfigurePreset -Fresh $true
        }

        if ($result.ExitCode -ne 0 -and $result.GeneratorUnavailable) {
            Write-Warning "$($candidate.Name) is not available. Trying next candidate."
            continue
        }
        if ($result.ExitCode -ne 0) {
            throw "CMake configuration failed with $($candidate.Name). See the CMake errors above."
        }

        Write-Host "Building native project with $($candidate.Name) ($Configuration)..."
        & cmake --build --preset $candidate.BuildPreset
        if ($LASTEXITCODE -ne 0) {
            throw "Native build failed with $($candidate.Name)."
        }

        if ($Linkage -eq "Dynamic") {
            $entryDll = Join-Path $nativeDir "out\build\$($candidate.BinaryDirectory)\$Configuration\VtkSharp.Native.dll"
            & "$PSScriptRoot/collect-native-dependencies.ps1" `
                -InputDll $entryDll `
                -VtkInstallDirectory $vtkInstallDirectory `
                -Configuration $Configuration `
                -VtkVersion $vtkBuildInfo.vtkVersion `
                -VtkBuildId $vtkBuildInfo.buildId `
                -ExplicitRuntimeModule @("vtkRenderingOpenGL2-9.7.dll")
            $vtkBinDirectory = Join-Path $vtkInstallDirectory "bin"
            & "$PSScriptRoot/copy-native-dependencies.ps1" `
                -ManifestPath (Join-Path (Split-Path -Parent $entryDll) "native-dependencies.json") `
                -SourceRoot @("entrypoint=$(Split-Path -Parent $entryDll)", "vtk=$vtkBinDirectory") `
                -DestinationDirectory (Split-Path -Parent $entryDll)
        }

        exit 0
    }

    throw "No supported Visual Studio generator was available. Install Visual Studio 2026 or Visual Studio 2022 with C++ desktop tools."
}
finally {
    Pop-Location
}
