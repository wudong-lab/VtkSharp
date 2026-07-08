param(
    [string]$Url = "https://examples.vtk.org/site/ColorNamesSeries/ColorNamePatches.html",
    [string]$OutputPath = "src/bindings/VtkSharp/Core/VtkColor3d.cs"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$atomicWords = @(
    "goldenrod", "turquoise", "lavender", "aquamarine", "chartreuse",
    "gainsboro", "burlywood", "cornsilk", "chocolate", "firebrick",
    "olivedrab", "whitesmoke", "mintcream",
    "dark", "light", "medium", "deep", "pale", "hot", "cold", "warm",
    "raw", "burnt", "cornflower", "midnight", "cadet", "royal", "dodger",
    "powder", "indian", "rosy", "sandy", "saddle", "forest", "spring",
    "floral", "ghost", "misty", "blanched", "antique", "alice", "navajo",
    "lemon", "peach", "papaya", "mint", "bisque", "orchid", "salmon",
    "coral", "khaki", "peru", "plum", "sienna", "thistle", "azure",
    "ivory", "beige", "linen", "seashell", "honeydew", "moccasin",
    "brick", "melon", "sepia", "cerulean", "cobalt", "carrot", "banana",
    "peacock", "cream", "raspberry", "ochre", "umber", "greenish", "sap", "madder",
    "crimson", "fuchsia", "indigo", "maroon", "emerald", "sapphire",
    "violet", "purple", "slate", "steel", "sky", "sea", "lawn", "lime",
    "gold", "gray", "grey", "pink", "tan", "teal", "aqua", "navy",
    "olive", "wheat", "tomato", "white", "black", "brown", "red",
    "green", "blue", "cyan", "magenta", "yellow", "orange", "silver"
)

function Get-Section {
    param(
        [string]$Html,
        [string]$Id,
        [string]$NextId
    )

    $start = $Html.IndexOf("<a id=`"$Id`">", [StringComparison]::Ordinal)
    if ($start -lt 0) {
        throw "Section '$Id' was not found."
    }

    if ([string]::IsNullOrEmpty($NextId)) {
        $end = $Html.Length
    }
    else {
        $end = $Html.IndexOf("<a id=`"$NextId`">", $start + 1, [StringComparison]::Ordinal)
        if ($end -lt 0) {
            throw "Section '$NextId' was not found."
        }
    }

    return $Html.Substring($start, $end - $start)
}

function ConvertTo-AtomicWords {
    param([string]$Name)

    $normalized = $Name -replace ",", " "
    $normalized = $normalized -creplace "([a-z])([A-Z])", '$1_$2'
    $normalized = $normalized -replace "[^A-Za-z0-9]+", "_"
    $parts = @($normalized.Split("_", [System.StringSplitOptions]::RemoveEmptyEntries))
    $words = New-Object System.Collections.Generic.List[string]

    foreach ($part in $parts) {
        $lower = $part.ToLowerInvariant()
        $index = 0
        while ($index -lt $lower.Length) {
            $match = $null
            foreach ($word in $atomicWords) {
                if ($word.Length -le $lower.Length - $index -and
                    $word.Length -gt $(if ($null -eq $match) { 0 } else { $match.Length }) -and
                    $lower.Substring($index, $word.Length) -eq $word) {
                    $match = $word
                }
            }

            if ($null -eq $match) {
                $words.Add($lower.Substring($index))
                break
            }

            $words.Add($match)
            $index += $match.Length
        }
    }

    return @($words)
}

function ConvertTo-PascalCase {
    param([string]$Name)

    $words = ConvertTo-AtomicWords $Name
    return (($words | ForEach-Object {
        $_.Substring(0, 1).ToUpperInvariant() + $_.Substring(1)
    }) -join "")
}

function Get-ComparisonKeys {
    param([string]$Name)

    $words = ConvertTo-AtomicWords $Name | ForEach-Object {
        if ($_ -eq "grey") { "gray" } else { $_ }
    }

    $compact = ($words -join "")
    $sorted = (($words | Sort-Object) -join "_")
    return @($compact, $sorted)
}

function ConvertTo-DoubleLiteral {
    param([int]$Value)

    return "$Value/255D"
}

function Read-ColorRows {
    param(
        [string]$Section,
        [string]$Source
    )

    $pattern = '<tr>\s*<td[^>]*style="background:[^>]*>(.*?)</td>\s*<td[^>]*style="background:[^>]*>(.*?)</td>\s*</tr>'
    $matches = [regex]::Matches($Section, $pattern, [Text.RegularExpressions.RegexOptions]::Singleline)

    foreach ($match in $matches) {
        $nameText = [Net.WebUtility]::HtmlDecode(($match.Groups[1].Value -replace "<.*?>", "").Trim())
        $rgbText = [Net.WebUtility]::HtmlDecode(($match.Groups[2].Value -replace "<.*?>", " "))
        $numbers = @([regex]::Matches($rgbText, "\d+") | ForEach-Object { [int]$_.Value })
        if ($numbers.Count -ne 3) {
            continue
        }

        $names = if ($Source -eq "Synonym") {
            @($nameText.Split(",", [StringSplitOptions]::RemoveEmptyEntries) | ForEach-Object { $_.Trim() })
        }
        else {
            @($nameText)
        }

        foreach ($name in $names) {
            [pscustomobject]@{
                Name = $name
                R = $numbers[0]
                G = $numbers[1]
                B = $numbers[2]
                Source = $Source
            }
        }
    }
}

$response = Invoke-WebRequest -Uri $Url -UseBasicParsing
$html = $response.Content

$webRows = @(Read-ColorRows (Get-Section $html "WebColorNames" "ParaViewColorNames") "Web")
$vtkRows = @(Read-ColorRows (Get-Section $html "VTKColorNames" "Synonyms") "VTK")
$synonymRows = @(Read-ColorRows (Get-Section $html "Synonyms" $null) "Synonym")

$seenNames = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$seenKeys = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$webRgb = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$entries = New-Object System.Collections.Generic.List[object]

foreach ($row in $webRows) {
    $pascal = ConvertTo-PascalCase $row.Name
    if (-not $seenNames.Add($pascal)) {
        continue
    }

    foreach ($key in Get-ComparisonKeys $row.Name) {
        [void]$seenKeys.Add($key)
    }

    [void]$webRgb.Add("$($row.R),$($row.G),$($row.B)")
    $entries.Add([pscustomobject]@{
        Name = $pascal
        R = $row.R
        G = $row.G
        B = $row.B
        Source = $row.Source
    })
}

$candidateRows = @($vtkRows + $synonymRows) | Sort-Object `
    @{ Expression = { if ((ConvertTo-AtomicWords $_.Name)[0] -in @("dark", "light", "medium", "deep", "pale", "hot", "cold", "warm")) { 0 } else { 1 } } }, `
    @{ Expression = { ConvertTo-PascalCase $_.Name } }

foreach ($row in $candidateRows) {
    $rgbKey = "$($row.R),$($row.G),$($row.B)"
    if ($webRgb.Contains($rgbKey)) {
        continue
    }

    $pascal = ConvertTo-PascalCase $row.Name
    if ($seenNames.Contains($pascal)) {
        continue
    }

    $keys = @(Get-ComparisonKeys $row.Name)
    $isAlias = $false
    foreach ($key in $keys) {
        if ($seenKeys.Contains($key)) {
            $isAlias = $true
            break
        }
    }

    if ($isAlias) {
        continue
    }

    [void]$seenNames.Add($pascal)
    foreach ($key in $keys) {
        [void]$seenKeys.Add($key)
    }

    $entries.Add([pscustomobject]@{
        Name = $pascal
        R = $row.R
        G = $row.G
        B = $row.B
        Source = $row.Source
    })
}

$entries = @($entries | Sort-Object Name)

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add("using System;")
$lines.Add("")
$lines.Add("namespace VtkSharp;")
$lines.Add("")
$lines.Add("/// <summary>")
$lines.Add("/// VTK颜色值，对应<see cref=`"vtkColor3d`"/>（RGB，分量范围 0.0–1.0）。")
$lines.Add("/// <para>")
$lines.Add("/// 静态预设值来源于 VTK examples ColorNamePatches 页面列出的 Web color Names")
$lines.Add("/// 以及与 Web 颜色无同值别名冲突的 VTK color Names；ParaView color Names 不包含在内。")
$lines.Add("/// </para>")
$lines.Add("/// </summary>")
$lines.Add("public readonly struct VtkColor3d")
$lines.Add("{")
$lines.Add("    public double R { get; }")
$lines.Add("    public double G { get; }")
$lines.Add("    public double B { get; }")
$lines.Add("")
$lines.Add("    public VtkColor3d(double r, double g, double b)")
$lines.Add("    {")
$lines.Add("        this.R = r;")
$lines.Add("        this.G = g;")
$lines.Add("        this.B = b;")
$lines.Add("    }")
$lines.Add("")
$lines.Add("    internal static unsafe VtkColor3d FromPointer(double* color3d)")
$lines.Add("    {")
$lines.Add("        var data = new Span<double>(color3d, 3);")
$lines.Add("        return new(data[0], data[1], data[2]);")
$lines.Add("    }")
$lines.Add("")
$lines.Add("    /// <summary>")
$lines.Add("    /// 命名颜色常量；Web 颜色字段名保留 VTK examples 页面中的大小写（如 <see cref=`"FireBrick`"/>）。")
$lines.Add("    /// </summary>")
$lines.Add("    // Auto-generated by tools/color_names/generate-vtk-color3d.ps1; do not edit manually.")

foreach ($entry in $entries) {
    $r = ConvertTo-DoubleLiteral $entry.R
    $g = ConvertTo-DoubleLiteral $entry.G
    $b = ConvertTo-DoubleLiteral $entry.B
    $lines.Add("    public static readonly VtkColor3d $($entry.Name) = new($r, $g, $b);")
}

$lines.Add("}")

$resolvedOutputPath = if ([IO.Path]::IsPathRooted($OutputPath)) {
    $OutputPath
}
else {
    Join-Path (Get-Location) $OutputPath
}

[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($resolvedOutputPath)) | Out-Null
[IO.File]::WriteAllLines($resolvedOutputPath, $lines, [Text.UTF8Encoding]::new($false))

Write-Host "Generated $($entries.Count) color presets: $resolvedOutputPath"
