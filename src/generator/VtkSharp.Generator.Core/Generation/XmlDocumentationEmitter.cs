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
        if (documentation.Parameters is not null)
        {
            foreach (var parameter in documentation.Parameters)
            {
                // 部分参数无上游说明时保留空标签，避免 CS1573；不编造参数语义。
                var attribute = $" name=\"{Escape(parameter.Name).Replace("\"", "&quot;", StringComparison.Ordinal)}\"";
                if (string.IsNullOrWhiteSpace(parameter.Text)) output.AppendLine($"{indent}/// <param{attribute} />");
                else EmitElement(output, "param", parameter.Text, indent, attribute);
            }
        }
        EmitElement(output, "returns", documentation.Returns, indent);
    }

    private static void EmitElement(StringBuilder output, string element, string? text, string indent, string attribute = "")
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        output.AppendLine($"{indent}/// <{element}{attribute}>");
        var paragraphs = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        foreach (var paragraph in paragraphs)
        {
            if (paragraphs.Length > 1) output.AppendLine($"{indent}/// <para>");
            foreach (var line in paragraph.Split('\n'))
                output.AppendLine(line.TrimEnd().Length == 0 ? $"{indent}///" : $"{indent}/// {Escape(line.TrimEnd())}");
            if (paragraphs.Length > 1) output.AppendLine($"{indent}/// </para>");
        }
        output.AppendLine($"{indent}/// </{element}>");
    }

    private static string Escape(string text)
        => text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
