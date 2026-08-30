#requires -Version 7.0
$ErrorActionPreference = "Stop"
$originalVtkDir = $env:VTK_DIR
$scripts = @("build-native.ps1", "build-all.ps1", "package-nuget.ps1", "verify-workflow.ps1")
$checks = 0

try {
    foreach ($name in $scripts) {
        $path = Join-Path $PSScriptRoot $name
        $tokens = $null
        $parseErrors = $null
        $ast = [Management.Automation.Language.Parser]::ParseFile($path, [ref]$tokens, [ref]$parseErrors)
        if ($parseErrors.Count -ne 0) { throw "${name}: $parseErrors" }

        # Evaluate only the real parameter block; do not build or touch artifacts.
        $parameters = [scriptblock]::Create($ast.ParamBlock.Extent.Text + "`n" + 'return $VtkDir')
        $env:VTK_DIR = 'C:\VTK test installation\lib\cmake\vtk-9.7'
        if ((& $parameters) -ne $env:VTK_DIR) { throw "${name}: VTK_DIR default was not used." }
        $checks++

        $explicitDirectory = 'D:\Other VTK\lib\cmake\vtk-9.7'
        if ((& $parameters -VtkDir $explicitDirectory) -ne $explicitDirectory) {
            throw "${name}: explicit -VtkDir did not override VTK_DIR."
        }
        $checks++

        # All entry points must fail before invoking external tools or creating output.
        $env:VTK_DIR = $null
        $caught = $false
        try { & $path }
        catch {
            if ($_.Exception.Message -notlike 'Set VTK_DIR*') { throw }
            $caught = $true
        }
        if (-not $caught) { throw "${name}: missing VTK_DIR was not rejected." }
        $checks++

        $caught = $false
        try { & $path -VtkDir ' ' }
        catch {
            if ($_.Exception.Message -notlike 'Set VTK_DIR*') { throw }
            $caught = $true
        }
        if (-not $caught) { throw "${name}: blank -VtkDir was not rejected." }
        $checks++
    }
}
finally {
    $env:VTK_DIR = $originalVtkDir
}

Write-Host "Passed $checks build environment checks."
