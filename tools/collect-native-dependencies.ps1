#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string[]]$InputDll,
    [Parameter(Mandatory)][string]$VtkInstallDirectory,
    [string[]]$AdditionalDependencyDirectory = @(),
    [string[]]$ExplicitRuntimeModule = @(),
    [Parameter(Mandatory)][ValidateSet("Debug", "Release")][string]$Configuration,
    [string]$VtkVersion = "9.7.0",
    [string]$VtkBuildId = "",
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$script:hashCache = @{}

function Read-PeImports([string]$Path) {
    $stream = [IO.File]::OpenRead($Path)
    $reader = [IO.BinaryReader]::new($stream)
    try {
        $stream.Position = 0x3c
        $peOffset = $reader.ReadInt32()
        $stream.Position = $peOffset
        if ($reader.ReadUInt32() -ne 0x00004550) { throw "Invalid PE signature: $Path" }
        $machine = $reader.ReadUInt16()
        if ($machine -ne 0x8664) { throw "Expected Windows x64 PE (machine 0x8664), got 0x$('{0:x4}' -f $machine): $Path" }
        $stream.Position = $peOffset + 6
        $sectionCount = $reader.ReadUInt16()
        $stream.Position = $peOffset + 20
        $optionalSize = $reader.ReadUInt16()
        $optionalOffset = $peOffset + 24
        $stream.Position = $optionalOffset
        $magic = $reader.ReadUInt16()
        $is64Bit = $magic -eq 0x20b
        if (-not $is64Bit -and $magic -ne 0x10b) { throw "Unsupported PE optional header: $Path" }
        $imageBaseOffset = if ($is64Bit) { $optionalOffset + 24 } else { $optionalOffset + 28 }
        $stream.Position = $imageBaseOffset
        $imageBase = if ($is64Bit) { $reader.ReadUInt64() } else { [uint64]$reader.ReadUInt32() }
        $directoryOffset = $optionalOffset + $(if ($is64Bit) { 112 } else { 96 })
        $stream.Position = $directoryOffset + 8
        $importRva = $reader.ReadUInt32()
        $importSize = $reader.ReadUInt32()
        $stream.Position = $directoryOffset + (13 * 8)
        $delayRva = $reader.ReadUInt32()
        $delaySize = $reader.ReadUInt32()

        $sectionOffset = $optionalOffset + $optionalSize
        $sections = for ($i = 0; $i -lt $sectionCount; $i++) {
            $stream.Position = $sectionOffset + ($i * 40) + 8
            $virtualSize = $reader.ReadUInt32()
            $virtualAddress = $reader.ReadUInt32()
            $rawSize = $reader.ReadUInt32()
            $rawPointer = $reader.ReadUInt32()
            [pscustomobject]@{ Va = [uint64]$virtualAddress; Size = [uint64][Math]::Max($virtualSize, $rawSize); Raw = [uint64]$rawPointer }
        }
        $toOffset = {
            param([uint64]$Rva)
            foreach ($section in $sections) {
                if ($Rva -ge $section.Va -and $Rva -lt ($section.Va + $section.Size)) {
                    return [long]($section.Raw + $Rva - $section.Va)
                }
            }
            if ($Rva -lt $sectionOffset) { return [long]$Rva }
            throw "PE RVA 0x$('{0:x}' -f $Rva) is not mapped in $Path"
        }
        $readString = {
            param([uint64]$Rva)
            if ($Rva -eq 0) { return "" }
            $stream.Position = & $toOffset $Rva
            $bytes = [Collections.Generic.List[byte]]::new()
            while (($value = $reader.ReadByte()) -ne 0) { $bytes.Add($value) }
            [Text.Encoding]::ASCII.GetString($bytes.ToArray())
        }

        $imports = [Collections.Generic.List[object]]::new()
        if ($importRva -ne 0 -and $importSize -ne 0) {
            $stream.Position = & $toOffset $importRva
            while ($true) {
                $descriptorPosition = $stream.Position
                $fields = 1..5 | ForEach-Object { $reader.ReadUInt32() }
                if (-not ($fields | Where-Object { $_ -ne 0 })) { break }
                $imports.Add([pscustomobject]@{ Name = (& $readString $fields[3]); Reason = "ordinary-import" })
                $stream.Position = $descriptorPosition + 20
            }
        }
        if ($delayRva -ne 0 -and $delaySize -ne 0) {
            $stream.Position = & $toOffset $delayRva
            while ($true) {
                $descriptorPosition = $stream.Position
                $fields = 1..8 | ForEach-Object { $reader.ReadUInt32() }
                if (-not ($fields | Where-Object { $_ -ne 0 })) { break }
                $nameAddress = [uint64]$fields[1]
                if (($fields[0] -band 1) -ne 0) {
                    $nameRva = $nameAddress
                }
                elseif ($nameAddress -ge $imageBase -and ($nameAddress - $imageBase) -le [uint32]::MaxValue) {
                    $nameRva = $nameAddress - $imageBase
                }
                else {
                    throw "Unsupported VA-based delay import descriptor in x64 image: $Path"
                }
                $imports.Add([pscustomobject]@{ Name = (& $readString $nameRva); Reason = "delay-import" })
                $stream.Position = $descriptorPosition + 32
            }
        }
        return $imports
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
    }
}

function Get-HashInfo([string]$Path) {
    $canonicalPath = [IO.Path]::GetFullPath($Path)
    if (-not $script:hashCache.ContainsKey($canonicalPath)) {
        $file = Get-Item -LiteralPath $canonicalPath
        $script:hashCache[$canonicalPath] = [pscustomobject]@{ Hash = (Get-FileHash -LiteralPath $canonicalPath -Algorithm SHA256).Hash.ToLowerInvariant(); Bytes = $file.Length }
    }
    $script:hashCache[$canonicalPath]
}

$vtkInstall = [IO.Path]::GetFullPath($VtkInstallDirectory)
if (-not (Test-Path -LiteralPath (Join-Path $vtkInstall 'lib/cmake/vtk-9.7/vtk-config.cmake'))) {
    throw "VTK CMake install was not found: $vtkInstall"
}
$roots = [Collections.Generic.List[object]]::new()
$rootSpecs = [Collections.Generic.List[object]]::new()
$rootSpecs.Add(@{ Id = 'vtk'; Path = (Join-Path $vtkInstall 'bin') })
$extraIndex = 0
foreach ($directory in $AdditionalDependencyDirectory) {
    $rootSpecs.Add(@{ Id = "extra$extraIndex"; Path = $directory })
    $extraIndex++
}
foreach ($spec in $rootSpecs) {
    if (-not (Test-Path -LiteralPath $spec.Path -PathType Container)) { throw "Dependency search root does not exist: $($spec.Path)" }
    $roots.Add([pscustomobject]@{ Id = $spec.Id; Path = [IO.Path]::GetFullPath($spec.Path); Files = @{} })
}
foreach ($root in $roots) {
    Get-ChildItem -LiteralPath $root.Path -Filter '*.dll' -File -Recurse | ForEach-Object {
        if (-not $root.Files.ContainsKey($_.Name)) { $root.Files[$_.Name] = [Collections.Generic.List[string]]::new() }
        $root.Files[$_.Name].Add($_.FullName)
    }
}

$systemRoot = [IO.Path]::GetFullPath([Environment]::GetFolderPath([Environment+SpecialFolder]::System)
).TrimEnd('\') + '\'
$apiSetPattern = '^(api-ms-win-|ext-ms-win-)'
$runtimePattern = '^(vcruntime|msvcp|concrt|vcomp)\d'
$files = @{}
$edges = [Collections.Generic.List[object]]::new()
$systemDependencies = @{}
$runtimeRequirements = @{}
$queue = [Collections.Generic.Queue[object]]::new()
$entryPoints = [Collections.Generic.List[object]]::new()

$inputSources = [Collections.Generic.List[object]]::new()
for ($inputIndex = 0; $inputIndex -lt $InputDll.Count; $inputIndex++) {
    $entry = $InputDll[$inputIndex]
    $entryRoot = if ($inputIndex -eq 0) { 'entrypoint' } else { "entrypoint$inputIndex" }
    $path = [IO.Path]::GetFullPath($entry)
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Native entry DLL does not exist: $path" }
    $info = Get-HashInfo $path
    $name = [IO.Path]::GetFileName($path)
    $record = [ordered]@{ name = $name; sourceRoot = $entryRoot; relativePath = $name; sha256 = $info.Hash; size = $info.Bytes; category = 'entrypoint'; reasons = @('entrypoint') }
    if ($files.ContainsKey($name) -and $files[$name].sha256 -ne $info.Hash) { throw "Conflicting entry DLLs have the same name: $name" }
    $files[$name] = $record
    $entryPoints.Add([ordered]@{ name = $name; sourceRoot = $entryRoot; relativePath = $name; sha256 = $info.Hash })
    $inputSources.Add([ordered]@{ id = $entryRoot })
    $queue.Enqueue([pscustomobject]@{ Name = $name; Path = $path; Reason = 'entrypoint'; Root = $entryRoot; RelativePath = $name })
}

foreach ($module in $ExplicitRuntimeModule) { $queue.Enqueue([pscustomobject]@{ Name = $module; Path = $null; Reason = 'explicit-runtime'; Root = $null; RelativePath = $null }) }

while ($queue.Count -gt 0) {
    $current = $queue.Dequeue()
    if ($current.Reason -eq 'explicit-runtime' -and -not $current.Path) {
        $matches = @()
        foreach ($root in $roots) { if ($root.Files.ContainsKey($current.Name)) { $matches += @($root.Files[$current.Name] | ForEach-Object { [pscustomobject]@{ Root = $root; Path = $_ } }) } }
        if ($matches.Count -eq 0 -and $Configuration -eq 'Debug' -and $current.Name.EndsWith('.dll', [StringComparison]::OrdinalIgnoreCase)) {
            $debugName = $current.Name.Substring(0, $current.Name.Length - 4) + 'd.dll'
            foreach ($root in $roots) { if ($root.Files.ContainsKey($debugName)) { $matches += @($root.Files[$debugName] | ForEach-Object { [pscustomobject]@{ Root = $root; Path = $_ } }) } }
            if ($matches.Count -gt 0) { $current.Name = $debugName }
        }
        if ($matches.Count -eq 0) { throw "Explicit runtime module was not found in the configured roots: $($current.Name)" }
        $candidateHashes = @($matches | ForEach-Object { (Get-HashInfo $_.Path).Hash } | Sort-Object -Unique)
        if ($candidateHashes.Count -gt 1) { throw "Conflicting files named $($current.Name) exist in configured roots." }
        $selected = $matches[0]
        $current.Path = $selected.Path
        $current.Root = $selected.Root.Id
        $current.RelativePath = [IO.Path]::GetRelativePath($selected.Root.Path, $selected.Path).Replace('\', '/')
        $info = Get-HashInfo $current.Path
        $category = if ($selected.Root.Id -eq 'vtk') { 'vtk' } else { 'third-party' }
        if (-not $files.ContainsKey($current.Name)) {
            $files[$current.Name] = [ordered]@{ name = $current.Name; sourceRoot = $current.Root; relativePath = $current.RelativePath; sha256 = $info.Hash; size = $info.Bytes; category = $category; reasons = @('explicit-runtime') }
        }
        else { $files[$current.Name].reasons += 'explicit-runtime' }
        $edges.Add([ordered]@{ from = '<explicit-runtime>'; to = $current.Name; reason = 'explicit-runtime'; classification = 'application-file' })
        $queue.Enqueue([pscustomobject]@{ Name = $current.Name; Path = $current.Path; Reason = 'explicit-runtime'; Root = $current.Root; RelativePath = $current.RelativePath })
        continue
    }

    foreach ($import in (Read-PeImports $current.Path)) {
        $name = $import.Name
        if ($name -match $apiSetPattern) {
            $systemDependencies[$name.ToLowerInvariant()] = [ordered]@{ name = $name; kind = 'api-set'; referencedBy = $current.Name }
            $edges.Add([ordered]@{ from = $current.Name; to = $name; reason = $import.Reason; classification = 'system' })
            continue
        }
        if ($name -match $runtimePattern) {
            $runtimeRequirements[$name.ToLowerInvariant()] = [ordered]@{ name = $name; reason = 'msvc-runtime'; referencedBy = $current.Name }
            $edges.Add([ordered]@{ from = $current.Name; to = $name; reason = $import.Reason; classification = 'runtime-requirement' })
            continue
        }
        $absoluteSystemPath = Join-Path $systemRoot $name
        if (Test-Path -LiteralPath $absoluteSystemPath -PathType Leaf) {
            $systemDependencies[$name.ToLowerInvariant()] = [ordered]@{ name = $name; kind = 'windows-system'; referencedBy = $current.Name }
            $edges.Add([ordered]@{ from = $current.Name; to = $name; reason = $import.Reason; classification = 'system' })
            continue
        }

        $matches = @()
        foreach ($root in $roots) { if ($root.Files.ContainsKey($name)) { $matches += @($root.Files[$name] | ForEach-Object { [pscustomobject]@{ Root = $root; Path = $_ } }) } }
        if ($matches.Count -eq 0) { throw "Unresolved application dependency '$name' imported by '$($current.Name)'. Add its explicit directory or classify its Windows/runtime dependency." }
        $candidateHashes = @($matches | ForEach-Object { (Get-HashInfo $_.Path).Hash } | Sort-Object -Unique)
        if ($candidateHashes.Count -gt 1) { throw "Conflicting files named '$name' exist in configured roots: $($matches.Path -join ', ')" }
        $selected = $matches[0]
        $info = Get-HashInfo $selected.Path
        $relative = [IO.Path]::GetRelativePath($selected.Root.Path, $selected.Path).Replace('\', '/')
        $category = if ($selected.Root.Id -eq 'vtk') { 'vtk' } else { 'third-party' }
        if (-not $files.ContainsKey($name)) {
            $files[$name] = [ordered]@{ name = $name; sourceRoot = $selected.Root.Id; relativePath = $relative; sha256 = $info.Hash; size = $info.Bytes; category = $category; reasons = @() }
            $queue.Enqueue([pscustomobject]@{ Name = $name; Path = $selected.Path; Reason = $import.Reason; Root = $selected.Root.Id; RelativePath = $relative })
        }
        if ($current.Reason -eq 'explicit-runtime') { $files[$name].reasons += 'explicit-runtime' }
        else { $files[$name].reasons += $import.Reason }
        $edges.Add([ordered]@{ from = $current.Name; to = $name; reason = $(if ($current.Reason -eq 'explicit-runtime') { 'explicit-runtime' } else { $import.Reason }); classification = 'application-file' })
    }
}

foreach ($record in $files.Values) { $record.reasons = @($record.reasons | Sort-Object -Unique) }
$orderedFiles = @($files.Values | Sort-Object name)
$totalBytes = ($orderedFiles | ForEach-Object { [long]$_['size'] } | Measure-Object -Sum).Sum
$buildId = if ($VtkBuildId) { $VtkBuildId } else { 'unspecified' }
$manifest = [ordered]@{
    schemaVersion = 1
    generatedAt = [DateTimeOffset]::Now.ToString('o')
    configuration = $Configuration
    architecture = 'x64'
    vtk = [ordered]@{ version = $VtkVersion; buildId = $buildId }
    entryPoints = @($entryPoints | Sort-Object name)
    inputSources = @($inputSources | Sort-Object id)
    files = $orderedFiles
    dependencyEdges = @($edges | Sort-Object from, to, reason)
    systemDependencies = @($systemDependencies.Values | Sort-Object name)
    runtimeRequirements = @($runtimeRequirements.Values | Sort-Object name)
    totals = [ordered]@{ fileCount = $orderedFiles.Count; bytes = [long]$totalBytes }
}
if (-not $OutputPath) { $OutputPath = Join-Path (Split-Path -Parent ([IO.Path]::GetFullPath($InputDll[0]))) 'native-dependencies.json' }
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
$manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath -Encoding utf8
Write-Host "Dependency manifest: $OutputPath"
Write-Host "Files: $($manifest.totals.fileCount); bytes: $($manifest.totals.bytes)"
