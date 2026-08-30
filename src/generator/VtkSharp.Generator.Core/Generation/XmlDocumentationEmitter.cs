using System.Text;
using VtkSharp.Generator.Core.Inspection;

namespace VtkSharp.Generator.Core.Generation;

internal static class XmlDocumentationEmitter
{
    public static void Emit(StringBuilder output, ApiDocumentation? documentation, string indent = "")
    {
        if (documentation is null) return;
        EmitElement(output, "summary", documentation.Summary, indent);
        EmitElement(output, "remarks", documentation.Remarks, indent);
    }

    private static void EmitElement(StringBuilder output, string element, string? text, string indent)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        output.AppendLine($"{indent}/// <{element}>");
        var paragraphs = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        foreach (var paragraph in paragraphs)
        {
            if (paragraphs.Length > 1) output.AppendLine($"{indent}/// <para>");
            foreach (var line in paragraph.Split('\n'))
                output.AppendLine($"{indent}/// {Escape(line)}");
            if (paragraphs.Length > 1) output.AppendLine($"{indent}/// </para>");
        }
        output.AppendLine($"{indent}/// </{element}>");
    }

    private static string Escape(string text)
        => text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
