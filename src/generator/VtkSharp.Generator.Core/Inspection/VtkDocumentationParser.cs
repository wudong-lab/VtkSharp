using System.Text.RegularExpressions;

namespace VtkSharp.Generator.Core.Inspection;

// 只解析调用者需要的指令；不尝试从任意自然语言推断参数、数组长度或所有权。
internal static class VtkDocumentationParser
{
    private static readonly Regex Command = new(@"^[@\\](\w+)\b\s*(.*)$");
    private static readonly Regex Parameter = new(@"^(?:\[\s*(in|out|in\s*,\s*out|inout)\s*\]\s*)?([A-Za-z_]\w*(?:\s*,\s*[A-Za-z_]\w*)*)\s*(.*)$");
    private static readonly Regex InlineCommand = new(@"[@\\](?:p|c|a|b|e|em|ref)\s+(\S+)");

    public static ApiDocumentation? Parse(IReadOnlyList<string> lines)
    {
        var body = new List<string>();
        var remarks = new List<string>();
        var returns = new List<string>();
        var parameters = new List<ParameterDocumentation>();
        var content = new List<string>();
        var section = "body";
        var names = "";
        string? direction = null;
        string? blockEnd = null;

        void Flush()
        {
            var text = InlineCommand.Replace(string.Join('\n', content).Trim(), "$1");
            content.Clear();
            if (text.Length == 0) return;
            switch (section)
            {
                case "body": body.Add(text); break;
                case "remarks": remarks.Add(text); break;
                case "see": remarks.Add("See also: " + text); break;
                case "returns": returns.Add(text); break;
                case "param":
                    foreach (var name in names.Split(',', StringSplitOptions.TrimEntries))
                        parameters.Add(new ParameterDocumentation(name, text, direction));
                    break;
            }
        }

        foreach (var line in lines)
        {
            var command = Command.Match(line);
            var name = command.Success ? command.Groups[1].Value : "";
            var argument = command.Success ? command.Groups[2].Value : "";
            if (blockEnd is not null)
            {
                if (name == blockEnd || (blockEnd is "```" or "~~~" && line.StartsWith(blockEnd, StringComparison.Ordinal)))
                    blockEnd = null;
                continue;
            }

            // 整块跳过，块内的 @param/@return 不得误当成 API 文档。
            if (name is "code" or "verbatim" or "dot" or "htmlonly" or "latexonly" ||
                line.StartsWith("```", StringComparison.Ordinal) || line.StartsWith("~~~", StringComparison.Ordinal))
            {
                Flush();
                blockEnd = name.Length == 0 ? line[..3] : "end" + name;
                if (name.Length > 0 && Regex.IsMatch(argument, @"[@\\]" + blockEnd + @"\b")) blockEnd = null;
                continue;
            }
            if (line is "@{" or "@}" or "\\{" or "\\}") continue;
            if (!command.Success)
            {
                content.Add(line);
                continue;
            }

            Flush();
            section = "discard";
            switch (name)
            {
                case "brief": section = "body"; break;
                case "details": case "remark": case "remarks": section = "remarks"; break;
                case "note": case "warning":
                    section = "remarks";
                    argument = ((name == "note" ? "Note: " : "Warning: ") + argument).TrimEnd();
                    break;
                case "sa": case "see": case "seealso": section = "see"; break;
                case "return": case "returns": case "retval": section = "returns"; break;
                case "param":
                    var match = Parameter.Match(argument);
                    if (!match.Success) break;
                    section = "param";
                    names = match.Groups[2].Value;
                    direction = match.Groups[1].Success ? match.Groups[1].Value.Replace(" ", "", StringComparison.Ordinal) : null;
                    argument = match.Groups[3].Value;
                    break;
                // 元数据占一行，不吞掉紧随 @class 的普通类型说明。
                case "class": case "struct": case "ingroup": case "name":
                    section = "body";
                    argument = "";
                    break;
            }
            content.Add(argument);
        }
        Flush();
        var paragraphs = string.Join("\n\n", body).Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var summary = paragraphs.FirstOrDefault();
        var detail = string.Join("\n\n", paragraphs.Skip(1).Concat(remarks));
        var result = string.Join("\n\n", returns);
        if (summary is null && detail.Length == 0 && parameters.Count == 0 && result.Length == 0) return null;
        return new ApiDocumentation(summary, detail.Length == 0 ? null : detail,
            parameters.Count == 0 ? null : parameters, result.Length == 0 ? null : result);
    }
}
