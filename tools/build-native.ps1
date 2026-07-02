param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [string]$VtkDir
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$nativeDir = Join-Path $repoRoot "src\bindings\VtkSharp.Native"

$candidates = @(
    @{ Name = "Visual Studio 2026"; ConfigurePreset = "windows-x64-vs2026"; BuildPreset = if ($Configuration -eq "Debug") { "windows-x64-vs2026-debug" } else { "windows-x64-vs2026-release" } },
    @{ Name = "Visual Studio 2022"; ConfigurePreset = "windows-x64-vs2022"; BuildPreset = if ($Configuration -eq "Debug") { "windows-x64-vs2022-debug" } else { "windows-x64-vs2022-release" } }
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

    & cmake @arguments 2>&1 | ForEach-Object { Write-Host $_ }
    return $LASTEXITCODE
}

Push-Location $nativeDir
try {
    foreach ($candidate in $candidates) {
        Write-Host "Configuring native project with $($candidate.Name)..."

        $exitCode = Invoke-CMakeConfigure -Preset $candidate.ConfigurePreset -Fresh $false
        if ($exitCode -ne 0) {
            Write-Host "Retrying $($candidate.Name) with a fresh CMake cache..."
            $exitCode = Invoke-CMakeConfigure -Preset $candidate.ConfigurePreset -Fresh $true
        }

        if ($exitCode -ne 0) {
            Write-Warning "$($candidate.Name) is not available. Trying next candidate."
            continue
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
