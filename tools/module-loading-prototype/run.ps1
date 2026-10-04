param(
    [string]$VtkInstallDirectory = 'D:\Code\VTK\VtkGitBuild-shared\install\Release',
    [string]$PrivateNativeRuntimeDirectory = 'D:\Code\wudong-lab\VtkSharpInternal\src\BRDI.VtkSharp.Native\out\build\standalone\dynamic\win-x64-vs2026\Release',
    [ValidateSet('Debug', 'Release')][string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$prototypeRoot = $PSScriptRoot
$nativeBuild = Join-Path $repoRoot 'artifacts\module-loading-prototype\build'
$runtimeDirectory = Join-Path $repoRoot 'artifacts\module-loading-prototype\runtime'
$manifestPath = Join-Path $repoRoot 'artifacts\module-loading-prototype\native-dependencies.json'
$vtkPackageDirectory = Join-Path $VtkInstallDirectory 'lib\cmake\vtk-9.7'
$vtkBuildInfo = Get-Content -LiteralPath (Join-Path $VtkInstallDirectory 'vtk-build-info.json') -Raw | ConvertFrom-Json

if ($vtkBuildInfo.configuration -ne $Configuration -or $vtkBuildInfo.architecture -ne 'x64' -or -not $vtkBuildInfo.buildSharedLibs) {
    throw "Prototype requires a matching Shared $Configuration x64 VTK installation."
}

& cmake -S (Join-Path $prototypeRoot 'native') -B $nativeBuild -G 'Visual Studio 18 2026' -A x64 "-DVTK_DIR=$vtkPackageDirectory"
if ($LASTEXITCODE -ne 0) { throw 'Prototype CMake configuration failed.' }
& cmake --build $nativeBuild --config $Configuration --parallel 8
if ($LASTEXITCODE -ne 0) { throw 'Prototype native build failed.' }

$nativeOutput = Join-Path $nativeBuild $Configuration
$entryPaths = @(
    (Join-Path $nativeOutput 'VtkSharp.Native.CommonCore.dll'),
    (Join-Path $nativeOutput 'VtkSharp.Native.FiltersSources.dll'),
    (Join-Path $nativeOutput 'VtkSharp.Native.Rendering.dll'),
    (Join-Path $PrivateNativeRuntimeDirectory 'BRDI.VtkSharp.Native.dll')
)
& (Join-Path $repoRoot 'tools\collect-native-dependencies.ps1') `
    -InputDll $entryPaths `
    -VtkInstallDirectory $VtkInstallDirectory `
    -Configuration $Configuration `
    -VtkVersion $vtkBuildInfo.vtkVersion `
    -VtkBuildId $vtkBuildInfo.buildId `
    -OutputPath $manifestPath
& (Join-Path $repoRoot 'tools\copy-native-dependencies.ps1') `
    -ManifestPath $manifestPath `
    -SourceRoot @(
        "entrypoint=$nativeOutput",
        "entrypoint1=$nativeOutput",
        "entrypoint2=$nativeOutput",
        "entrypoint3=$PrivateNativeRuntimeDirectory",
        "vtk=$(Join-Path $VtkInstallDirectory 'bin')"
    ) `
    -DestinationDirectory $runtimeDirectory

$managedProject = Join-Path $prototypeRoot 'managed\Prototype.csproj'
& dotnet build $managedProject --configuration $Configuration
if ($LASTEXITCODE -ne 0) { throw 'Prototype managed build failed.' }

$managedOutput = Join-Path (Join-Path (Split-Path -Parent $managedProject) "bin\$Configuration") ''
$dotnetPath = (Get-Command dotnet).Source
$logDirectory = Join-Path $repoRoot 'artifacts\module-loading-prototype\logs'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$savedEnvironment = @{
    PATH = $env:PATH
    VTK_DIR = $env:VTK_DIR
    VTK_ROOT = $env:VTK_ROOT
    VTK_INSTALL_DIR = $env:VTK_INSTALL_DIR
}
try {
    $env:PATH = "$(Join-Path $env:SystemRoot 'System32');$env:SystemRoot"
    $env:VTK_DIR = ''
    $env:VTK_ROOT = ''
    $env:VTK_INSTALL_DIR = ''
    foreach ($framework in @('net8.0-windows', 'net48')) {
        foreach ($scenario in @('core', 'filters', 'render', 'private-first', 'public-first', 'concurrent')) {
            Write-Host "=== $framework / $scenario ==="
            $logPath = Join-Path $logDirectory "$framework-$scenario.txt"
            if ($framework -eq 'net8.0-windows') {
                & $dotnetPath (Join-Path $managedOutput "$framework\Prototype.dll") $runtimeDirectory $scenario *> $logPath
            }
            else {
                & (Join-Path $managedOutput "$framework\Prototype.exe") $runtimeDirectory $scenario *> $logPath
            }
            if ($LASTEXITCODE -ne 0) { throw "Prototype scenario failed: $framework / $scenario; see $logPath" }
            $lines = Get-Content -LiteralPath $logPath
            $runtimePrefix = [IO.Path]::GetFullPath($runtimeDirectory) + [IO.Path]::DirectorySeparatorChar
            $loadedPaths = @($lines | Where-Object { $_ -match '^loaded=' } | ForEach-Object { $_.Substring(7) })
            $outsideRuntime = @($loadedPaths | Where-Object { -not $_.StartsWith($runtimePrefix, [StringComparison]::OrdinalIgnoreCase) })
            if ($outsideRuntime.Count -gt 0) { throw "A prototype native module loaded outside the deployment directory: $($outsideRuntime -join ', ')" }

            $loadRequests = @($lines | Where-Object { $_ -match '^load-request=' } | ForEach-Object { ($_ -split ';', 2)[0].Substring(13) })
            $expectedFirstEntry = switch ($scenario) {
                'core' { 'VtkSharp.Native.CommonCore.dll' }
                'filters' { 'VtkSharp.Native.FiltersSources.dll' }
                'render' { 'VtkSharp.Native.Rendering.dll' }
                'private-first' { 'BRDI.VtkSharp.Native.dll' }
                'public-first' { 'VtkSharp.Native.CommonCore.dll' }
                'concurrent' { $null }
            }
            if ($expectedFirstEntry -and $loadRequests[0] -ne $expectedFirstEntry) {
                throw "Unexpected first native load for $framework / ${scenario}: $($loadRequests -join ', ')"
            }
            $allLoadableEntries = @('VtkSharp.Native.CommonCore.dll', 'VtkSharp.Native.FiltersSources.dll', 'VtkSharp.Native.Rendering.dll', 'BRDI.VtkSharp.Native.dll')
            $unexpectedEntries = @($loadRequests | Where-Object { $_ -notin $allLoadableEntries })
            if ($unexpectedEntries.Count -gt 0) { throw "Unexpected C ABI entry was requested: $($unexpectedEntries -join ', ')" }
            Get-Content -LiteralPath $logPath
        }
    }
}
finally {
    foreach ($name in $savedEnvironment.Keys) {
        Set-Item -Path "Env:$name" -Value $savedEnvironment[$name]
    }
}
