param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$VtkDir = (Join-Path $PSScriptRoot "..\..\..\VTK\VtkGitBuild\install\lib\cmake\vtk-9.6")
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$nativeDir = Join-Path $repoRoot "src\bindings\VtkSharp.Native"
$VtkDir = [IO.Path]::GetFullPath($VtkDir)

$vtkConfigFiles = @("vtk-config.cmake", "VTKConfig.cmake")
if (-not ($vtkConfigFiles | Where-Object { Test-Path -LiteralPath (Join-Path $VtkDir $_) -PathType Leaf })) {
    throw "VTK CMake package was not found in: $VtkDir. Build and install VTK first, or pass -VtkDir explicitly."
}

$candidates = @(
    @{ Name = "Visual Studio 2026"; ConfigurePreset = "win-x64-vs2026"; BuildPreset = if ($Configuration -eq "Debug") { "win-x64-vs2026-debug" } else { "win-x64-vs2026-release" } },
    @{ Name = "Visual Studio 2022"; ConfigurePreset = "win-x64-vs2022"; BuildPreset = if ($Configuration -eq "Debug") { "win-x64-vs2022-debug" } else { "win-x64-vs2022-release" } }
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

    $arguments += "-DVTK_DIR=$VtkDir"

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

        exit 0
    }

    throw "No supported Visual Studio generator was available. Install Visual Studio 2026 or Visual Studio 2022 with C++ desktop tools."
}
finally {
    Pop-Location
}
