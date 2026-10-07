#requires -Version 7.0
param(
    [string]$VtkDir = $env:VTK_DIR,

    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [ValidateSet("Fast", "Final", "CI")]
    [string]$Mode = "Final",

    [ValidateSet("Static", "Dynamic")]
    [string]$Linkage = "Static",

    [ValidateSet("Development", "Isolated")]
    [string]$RuntimeMode = "Development",

    [string]$VtkBinDirectory,
    [string]$GeneratorConfig,
    [string]$Example,
    [string]$IsolatedExecutable,
    [string[]]$IsolatedArguments = @(),
    [switch]$Regenerate,
    [string]$OutputDirectory,

    [ValidateRange(1, 86400)]
    [int]$StageTimeoutSeconds = 1800,

    [ValidateRange(1, 3600)]
    [int]$ExampleTimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($VtkDir)) {
    throw "Set VTK_DIR to the installed VTK CMake package directory, or pass -VtkDir. See README.md."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot "artifacts/verification/$([DateTime]::Now.ToString('yyyyMMdd-HHmmss'))-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory, $PWD.Path)
if (Test-Path -LiteralPath $OutputDirectory) {
    throw "Output directory already exists; select a new directory: $OutputDirectory"
}
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null

$report = [ordered]@{
    status = "running"
    startedAt = [DateTimeOffset]::Now.ToString("o")
    repository = $repoRoot
    configuration = $Configuration
    linkage = $Linkage
    mode = $Mode
    runtimeMode = $RuntimeMode
    vtkDir = $VtkDir
    vtkBinDirectory = $VtkBinDirectory
    generatorConfig = $GeneratorConfig
    regenerate = [bool]$Regenerate
    example = $Example
    stages = [Collections.Generic.List[object]]::new()
    manualChecks = @("visual-content", "interaction", "repeated-create-dispose")
}
$reportPath = Join-Path $OutputDirectory "verification.json"

function Save-Report {
    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $reportPath -Encoding utf8
}

function Add-Stage([string]$Name, [string]$Executable, [string[]]$Arguments, [bool]$Selected = $true, [int]$Timeout = $StageTimeoutSeconds, [string]$WorkingDirectory = $repoRoot) {
    $report.stages.Add([ordered]@{
        name = $Name; status = "not-run"; selected = $Selected
        reason = $(if ($Selected) { "pending" } else { "not-selected" })
        executable = $Executable; arguments = $Arguments; timeoutSeconds = $Timeout; workingDirectory = $WorkingDirectory
        exitCode = $null; durationSeconds = 0
        stdout = Join-Path $OutputDirectory "$Name.stdout.log"
        stderr = Join-Path $OutputDirectory "$Name.stderr.log"
    })
}

function Invoke-Stage($Stage) {
    Write-Host "Running $($Stage.name)..."
    $Stage.status = "running"
    $Stage.reason = $null
    Save-Report
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $process = [Diagnostics.Process]::new()
    $started = $false
    $stdout = $null
    $stderr = $null
    try {
        $process.StartInfo = [Diagnostics.ProcessStartInfo]@{
            FileName = $Stage.executable; WorkingDirectory = $Stage.workingDirectory
            UseShellExecute = $false; CreateNoWindow = $true
            RedirectStandardOutput = $true; RedirectStandardError = $true
        }
        foreach ($argument in $Stage.arguments) { $process.StartInfo.ArgumentList.Add($argument) }
        if ($Stage.name -ne "isolated-deployment") {
            $process.StartInfo.Environment["PATH"] = "$VtkBinDirectory$([IO.Path]::PathSeparator)$env:PATH"
            $process.StartInfo.Environment["VTK_ROOT"] = [IO.Path]::GetFullPath((Join-Path $VtkDir "../../.."))
            $process.StartInfo.Environment["VTK_DIR"] = $VtkDir
        }
        else {
            $vtkRoots = @($VtkDir, $env:VTK_DIR) | Where-Object { $_ } | ForEach-Object { [IO.Path]::GetFullPath((Join-Path $_ "../../..")) } | Select-Object -Unique
            if ($env:VTK_ROOT) { $vtkRoots += [IO.Path]::GetFullPath($env:VTK_ROOT) }
            $blockedBins = @($VtkBinDirectory) + @($vtkRoots | ForEach-Object { Join-Path $_ "bin" })
            $allowedPath = $env:PATH -split [regex]::Escape([string][IO.Path]::PathSeparator) |
                Where-Object { $entry = [IO.Path]::GetFullPath($_); $blockedBins -notcontains $entry }
            $process.StartInfo.Environment["PATH"] = $allowedPath -join [IO.Path]::PathSeparator
            $process.StartInfo.Environment["VTK_ROOT"] = ""
            $process.StartInfo.Environment["VTK_DIR"] = ""
        }
        $process.StartInfo.Environment["NO_COLOR"] = "1"
        $stdout = [IO.File]::Create($Stage.stdout)
        $stderr = [IO.File]::Create($Stage.stderr)
        $started = $process.Start()
        $copyOut = $process.StandardOutput.BaseStream.CopyToAsync($stdout)
        $copyErr = $process.StandardError.BaseStream.CopyToAsync($stderr)
        if (-not $process.WaitForExit($Stage.timeoutSeconds * 1000)) {
            $process.Kill($true)
            $process.WaitForExit()
            $Stage.reason = "timeout"
        }
        [void]$copyOut.GetAwaiter().GetResult()
        [void]$copyErr.GetAwaiter().GetResult()
        $Stage.exitCode = $process.ExitCode
        $Stage.status = if ($process.ExitCode -eq 0 -and -not $Stage.reason) { "passed" } else { "failed" }
    }
    catch {
        $Stage.status = "failed"
        $Stage.reason = $_.Exception.Message
    }
    finally {
        if ($started -and -not $process.HasExited) {
            $process.Kill($true)
            $process.WaitForExit()
        }
        if ($stdout) { $stdout.Dispose() }
        if ($stderr) { $stderr.Dispose() }
        $process.Dispose()
        $Stage.durationSeconds = [Math]::Round($timer.Elapsed.TotalSeconds, 2)
    }

    # 警告只提取摘要，不据此判定成功，也不声称它们是相对基线的新增警告。
    $warnings = @(Get-Content -LiteralPath $Stage.stdout, $Stage.stderr |
        Where-Object { $_ -match '(?i)\bwarning\b|警告' -and $_ -notmatch '^\s*0\s+(Warning\(s\)|个警告|警告)' } | Select-Object -Unique)
    $Stage.warningCount = $warnings.Count
    $Stage.warningSummary = @($warnings | Select-Object -First 8)
    Write-Host "$($Stage.name): $($Stage.status) ($($Stage.durationSeconds)s), warning lines: $($warnings.Count)"
    $Stage.warningSummary | ForEach-Object { Write-Host "  $_" }
    if ($Stage.status -eq "failed") {
        Write-Host "  Reason: $($Stage.reason); exit code: $($Stage.exitCode)"
        foreach ($logPath in @($Stage.stderr, $Stage.stdout)) {
            Get-Content -LiteralPath $logPath | Select-Object -Last 10 | ForEach-Object { Write-Host "  $_" }
        }
        Write-Host "  Logs: $($Stage.stdout), $($Stage.stderr)"
    }
    Save-Report
}

try {
    $VtkDir = [IO.Path]::GetFullPath($VtkDir, $PWD.Path)
    if (-not (@("VTKConfig.cmake", "vtk-config.cmake") | Where-Object { Test-Path -LiteralPath (Join-Path $VtkDir $_) -PathType Leaf })) {
        throw "VTK CMake package not found: $VtkDir"
    }
    if (-not $VtkBinDirectory) { $VtkBinDirectory = Join-Path $VtkDir "../../../bin" }
    $VtkBinDirectory = (Resolve-Path -LiteralPath $VtkBinDirectory).Path
    if ($GeneratorConfig) { $GeneratorConfig = (Resolve-Path -LiteralPath $GeneratorConfig).Path }
    $report.vtkDir = $VtkDir
    $report.vtkBinDirectory = $VtkBinDirectory
    $report.generatorConfig = $GeneratorConfig
    $report.commit = (& git -C $repoRoot rev-parse HEAD)
    $report.workingTree = @(& git -C $repoRoot status --short)

    $cliProject = "src/generator/VtkSharp.Generator.Cli"
    $generatorConfiguration = "Release"
    $cli = @("run", "--no-build", "--project", $cliProject, "--configuration", $generatorConfiguration, "--")
    $configArgs = if ($GeneratorConfig) { @("--config", $GeneratorConfig) } else { @() }
    Add-Stage "generator-build" "dotnet" @("build", $cliProject, "--configuration", $generatorConfiguration, "--nologo")
    Add-Stage "generator-tests" "dotnet" @("test", "src/generator/VtkSharp.Generator.Tests", "--configuration", $Configuration, "--nologo") ($Mode -ne "Fast")
    Add-Stage "generate" "dotnet" ($cli + @("generate-bindings", "--output-root", "src", "--incremental") + $configArgs) ([bool]$Regenerate)
    Add-Stage "native-build" "pwsh" @("-NoProfile", "-File", "$PSScriptRoot/build-native.ps1", "-Configuration", $Configuration, "-Linkage", $Linkage, "-VtkDir", $VtkDir)
    Add-Stage "managed-tests" "dotnet" @("test", "src/bindings/VtkSharp.slnx", "--configuration", $Configuration, "--nologo")
    Add-Stage "example-build" "dotnet" @("build", "src/examples/ExampleBrowser/ExampleBrowser.csproj", "--configuration", $Configuration, "--nologo") ($RuntimeMode -eq "Development")
    $exampleExe = Join-Path $repoRoot "src/examples/ExampleBrowser/bin/$Configuration/net8.0-windows/ExampleBrowser.exe"
    Add-Stage "example-smoke" $exampleExe @("--smoke", $Example, "--output", (Join-Path $OutputDirectory "example")) ([bool]$Example -and $RuntimeMode -eq "Development") $ExampleTimeoutSeconds
    $deploymentDirectory = Join-Path $OutputDirectory "deployment"
    Add-Stage "example-publish" "dotnet" @("publish", "src/examples/ExampleBrowser/ExampleBrowser.csproj", "--configuration", $Configuration, "--runtime", "win-x64", "--self-contained", "false", "--output", $deploymentDirectory, "--nologo") ($RuntimeMode -eq "Isolated") $StageTimeoutSeconds
    $isolatedPath = if ($IsolatedExecutable) { [IO.Path]::GetFullPath($IsolatedExecutable, $repoRoot) } else { Join-Path $deploymentDirectory "ExampleBrowser.exe" }
    $isolatedArguments = if ($IsolatedExecutable) { $IsolatedArguments } else { @("--smoke", $(if ($Example) { $Example } else { "GeometricObjects/Cone" }), "--output", (Join-Path $OutputDirectory "isolated-example")) }
    Add-Stage "isolated-deployment" $isolatedPath $isolatedArguments ($RuntimeMode -eq "Isolated") $ExampleTimeoutSeconds (Split-Path -Parent $isolatedPath)
    $generatedCheckArgs = @("generate-bindings", "--check")
    if ($Mode -ne "CI") { $generatedCheckArgs += "--incremental" }
    Add-Stage "generated-check" "dotnet" ($cli + $generatedCheckArgs + $configArgs)

    $nativeBuildRoot = Join-Path $repoRoot "src/bindings/VtkSharp.Native/out/build"
    $nativeBuildPresets = if ($Linkage -eq "Dynamic") { @("dynamic/win-x64-vs2026", "dynamic/win-x64-vs2022") } else { @("win-x64-vs2026", "win-x64-vs2022") }
    $nativeRuntimeArguments = @()

    $failed = $false
    foreach ($stage in $report.stages) {
        if (-not $stage.selected) { continue }
        if ($failed) { $stage.reason = "earlier-stage-failed"; continue }
        if ($stage.name -in @("managed-tests", "example-build", "example-publish")) {
            $stage.arguments += "-p:VtkSharpNativeLinkage=$Linkage"
            $stage.arguments += $nativeRuntimeArguments
        }
        Invoke-Stage $stage
        $failed = $stage.status -eq "failed"
        if ($stage.name -eq "native-build" -and -not $failed -and $Linkage -eq "Dynamic") {
            $nativeManifestCandidates = @()
            foreach ($preset in $nativeBuildPresets) {
                $candidate = Join-Path $nativeBuildRoot "$preset/$Configuration"
                $candidateManifest = Join-Path $candidate "native-dependencies.json"
                if (Test-Path -LiteralPath $candidateManifest -PathType Leaf) { $nativeManifestCandidates += Get-Item -LiteralPath $candidateManifest }
            }
            $nativeRuntimeDirectory = if ($nativeManifestCandidates.Count -gt 0) { Split-Path -Parent (($nativeManifestCandidates | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).FullName) } else { $null }
            if (-not $nativeRuntimeDirectory) { throw "Dynamic native runtime output was not found after the native build." }
            $nativeRuntimeArguments = @("-p:VtkSharpNativeRuntimeDirectory=$nativeRuntimeDirectory")
        }
    }
    $report.status = if ($failed) { "failed" } else { "passed" }
}
catch {
    $report.status = "failed"
    $report.error = $_.Exception.Message
    Write-Host "Verification failed: $($report.error)"
}
finally {
    $report.finishedAt = [DateTimeOffset]::Now.ToString("o")
    Save-Report
    Write-Host "Selected checks: $($report.status). Report: $reportPath"
    foreach ($stage in $report.stages | Where-Object status -eq "not-run") {
        Write-Host "Not run: $($stage.name) ($($stage.reason))"
    }
    Write-Host "Manual verification still required: $($report.manualChecks -join ', ')."
}
if ($report.status -ne "passed") { exit 1 }
