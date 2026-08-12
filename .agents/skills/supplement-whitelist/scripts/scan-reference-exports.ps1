[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $ReferenceDirectory,

    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'

$resolvedDirectory = (Resolve-Path -LiteralPath $ReferenceDirectory).Path
$files = Get-ChildItem -LiteralPath $resolvedDirectory -Recurse -File -Filter '*_export_gen.cpp'
$classes = [ordered]@{}
$warnings = [System.Collections.Generic.List[string]]::new()

foreach ($file in $files) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $expectedClassName = $file.BaseName -replace '_export_gen$', ''
    $includeMatches = [regex]::Matches($content, '#include\s*[<"](?<class>vtk[A-Za-z0-9_]+)\.h[>"]')
    $className = $includeMatches |
        ForEach-Object { $_.Groups['class'].Value } |
        Where-Object { $_ -eq $expectedClassName } |
        Select-Object -First 1

    if (-not $className) {
        $warnings.Add("No VTK class include found: $($file.FullName)")
        continue
    }

    $prefix = "${className}_"
    $methods = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
    $exportMatches = [regex]::Matches(
        $content,
        '(?m)^\s*(?:VTK_NET_API|VTKSHARP_API)\b[^\r\n(]*?\b(?<function>vtk[A-Za-z0-9_]+)\s*\('
    )

    foreach ($match in $exportMatches) {
        $functionName = $match.Groups['function'].Value
        if (-not $functionName.StartsWith($prefix, [System.StringComparison]::Ordinal)) {
            continue
        }

        $methodName = $functionName.Substring($prefix.Length) -replace '_\d+$', ''
        if ($methodName -and $methodName -ne 'New') {
            [void] $methods.Add($methodName)
        }
    }

    if (-not $classes.Contains($className)) {
        $classes[$className] = [ordered]@{
            Files = [System.Collections.Generic.List[string]]::new()
            Methods = [System.Collections.Generic.SortedSet[string]]::new([System.StringComparer]::Ordinal)
        }
    }

    $classes[$className].Files.Add($file.FullName)
    foreach ($method in $methods) {
        [void] $classes[$className].Methods.Add($method)
    }
}

$classResults = foreach ($entry in $classes.GetEnumerator()) {
    [ordered]@{
        Name = $entry.Key
        Files = @($entry.Value.Files)
        Methods = @($entry.Value.Methods)
    }
}

$result = [ordered]@{
    ReferenceDirectory = $resolvedDirectory
    Files = $files.Count
    Classes = @($classResults)
    Warnings = @($warnings)
}

$json = $result | ConvertTo-Json -Depth 6
if ($OutputPath) {
    $parent = Split-Path -Parent $OutputPath
    if ($parent) {
        New-Item -ItemType Directory -Force -Path $parent | Out-Null
    }
    Set-Content -LiteralPath $OutputPath -Value $json -Encoding utf8
}

$json
