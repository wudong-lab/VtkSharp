using System.Text.RegularExpressions;

namespace VtkSharp.Generator.Core.Types;

public sealed partial class TypeCanonicalizer
{
    public CanonicalType Canonicalize(string typeName)
    {
        var text = NormalizeWhitespace(typeName);
        text = NormalizeWin32Handle(text);
        text = NormalizeVtkStdString(text);
        text = NormalizeArray(text);
        text = NormalizePointer(text);
        text = NormalizeConstPointer(text);
        text = NormalizeConstScalarArray(text);
        return new CanonicalType(text);
    }

    private static string NormalizeWhitespace(string text)
        => WhitespaceRegex().Replace(text.Trim(), " ");

    private static string NormalizeWin32Handle(string text)
        => text switch
        {
            "HWND__ *" or "HWND__*" => "HWND",
            "HDC__ *" or "HDC__*" => "HDC",
            "HGLRC__ *" or "HGLRC__*" => "HGLRC",
            _ => text,
        };

    private static string NormalizeArray(string text)
        => ArraySpaceRegex().Replace(text, "$1[$2]");

    private static string NormalizePointer(string text)
        => PointerSpaceRegex().Replace(text, "$1*");

    private static string NormalizeConstPointer(string text)
    {
        var match = ConstPointerRegex().Match(text);
        return match.Success ? $"const {match.Groups["type"].Value}*" : text;
    }

    private static string NormalizeConstScalarArray(string text)
    {
        var match = ConstArrayRegex().Match(text);
        return match.Success ? $"const {match.Groups["type"].Value}[{match.Groups["count"].Value}]" : text;
    }

    private static string NormalizeVtkStdString(string text)
        => text == "vtkStdString const&" ? "const char*" : text;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^(.+?)\s+\[(\d+)\]$")]
    private static partial Regex ArraySpaceRegex();

    [GeneratedRegex(@"^(.+?)\s+\*$")]
    private static partial Regex PointerSpaceRegex();

    [GeneratedRegex(@"^(?<type>vtk\w+|char|void|double|float|int)\s+const\*$")]
    private static partial Regex ConstPointerRegex();

    [GeneratedRegex(@"^(?<type>double|float|int)\s+const\[(?<count>\d+)\]$")]
    private static partial Regex ConstArrayRegex();
}
