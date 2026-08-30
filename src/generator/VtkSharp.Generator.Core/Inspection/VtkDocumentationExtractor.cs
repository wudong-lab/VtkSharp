using System.Text;
using System.Text.RegularExpressions;

namespace VtkSharp.Generator.Core.Inspection;

/// <summary>按原始源码位置关联注释，不使用 Clang 的注释继承或宏展开后的附着结果。</summary>
internal sealed class VtkDocumentationExtractor
{
    private readonly Dictionary<string, ApiDocumentation> _classes = new(StringComparer.Ordinal);
    private readonly List<(int Start, int End, ApiDocumentation Documentation)> _declarations = [];
    private static readonly Regex ClassCommand = new(@"(?:@|\\)class\s+(\w+)");
    private static readonly Regex TargetCommand = new(@"^(?:@|\\)(class|struct|enum|file|fn|defgroup|ingroup|name|internal)\b");

    public ApiDocumentation? GetClassDocumentation(string name, int offset)
        => this._classes.GetValueOrDefault(name) ?? this.GetDeclarationDocumentation(offset);

    public ApiDocumentation? GetDeclarationDocumentation(int offset)
    {
        var low = 0;
        var high = this._declarations.Count - 1;
        while (low <= high)
        {
            var middle = low + (high - low) / 2;
            var entry = this._declarations[middle];
            if (offset < entry.Start) high = middle - 1;
            else if (offset >= entry.End) low = middle + 1;
            else return entry.Documentation;
        }
        return null;
    }

    public static VtkDocumentationExtractor Parse(string source)
    {
        var result = new VtkDocumentationExtractor();
        ApiDocumentation? pending = null;
        var groups = new List<(int Depth, ApiDocumentation? Documentation)>();
        var depth = 0;
        var byteOffset = 0;
        var lastOffset = 0;
        ApiDocumentation? previous = null;
        var i = 0;
        while (i < source.Length)
        {
            var ch = source[i];
            if (char.IsWhiteSpace(ch) || ch == '\uFEFF')
            {
                i++;
                continue;
            }

            // 不扫描预处理定义内部的注释/分组；宏调用仍由下面的普通源码扫描处理。
            if (ch == '#' && IsLineStart(source, i))
            {
                pending = null;
                // 不求值条件编译。跨预处理分支宁可缺失共享说明，也不串用另一分支的注释。
                groups.Clear();
                previous = null;
                do
                {
                    var end = source.IndexOf('\n', i);
                    if (end < 0) { i = source.Length; break; }
                    var continued = source.AsSpan(i, end - i).TrimEnd().EndsWith("\\");
                    i = end + 1;
                    if (!continued) break;
                } while (i < source.Length);
                continue;
            }

            if (ch == '/' && i + 1 < source.Length && source[i + 1] is '/' or '*')
            {
                var start = i;
                var block = source[i + 1] == '*';
                var documented = i + 2 < source.Length && source[i + 2] is '*' or '!' or '/';
                if (block)
                {
                    var end = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? source.Length : end + 2;
                }
                else
                {
                    // 合并相邻的 /// 或 //! 行，保留段落分隔。
                    do
                    {
                        var end = source.IndexOf('\n', i);
                        i = end < 0 ? source.Length : end + 1;
                        if (!documented || i == source.Length || IsGroupLine(source, start)) break;
                        var next = i;
                        while (next < source.Length && source[next] is ' ' or '\t') next++;
                        if (!source.AsSpan(next).StartsWith("///") && !source.AsSpan(next).StartsWith("//!")) break;
                        if (IsGroupLine(source, next)) break;
                        i = next;
                    } while (i < source.Length);
                }

                if (!documented || (start + 3 < source.Length && source[start + 3] == '<'))
                {
                    pending = null;
                    continue;
                }

                var lines = CleanComment(source[start..i], block);
                var opensGroup = lines.Any(line => line is "@{" or "\\{");
                var closesGroup = lines.Any(line => line is "@}" or "\\}");
                if (opensGroup)
                {
                    groups.Add((depth, null));
                    pending = null;
                }
                var classMatch = ClassCommand.Match(string.Join('\n', lines));
                var targeted = lines.Any(line => TargetCommand.IsMatch(line));
                var documentation = ParseDocumentation(lines);
                if (classMatch.Success && documentation is not null)
                {
                    result._classes[classMatch.Groups[1].Value] = documentation;
                    pending = null;
                }
                else if (!targeted && documentation is not null)
                {
                    pending = documentation;
                    if (groups.Count > 0)
                        groups[^1] = (groups[^1].Depth, documentation);
                }
                else if (targeted)
                    pending = null;

                if (closesGroup)
                {
                    if (groups.Count > 0) groups.RemoveAt(groups.Count - 1);
                    pending = null;
                }
                continue;
            }

            var active = pending ?? (groups.Count > 0 ? groups[^1].Documentation : null);
            if (active is not null)
            {
                // libclang 的 offset 按 UTF-8 字节计数，而 C# 字符串按 UTF-16 索引。
                byteOffset += Encoding.UTF8.GetByteCount(source.AsSpan(lastOffset, i - lastOffset));
                lastOffset = i;
                var end = byteOffset + Encoding.UTF8.GetByteCount(source.AsSpan(i, 1));
                if (ReferenceEquals(previous, active))
                    result._declarations[^1] = (result._declarations[^1].Start, end, active);
                else
                    result._declarations.Add((byteOffset, end, active));
            }
            previous = active;

            if (char.IsDigit(ch))
            {
                // 数字分隔符（例如 1'000）不是字符字面量的起点。
                do { i++; } while (i < source.Length && (char.IsAsciiLetterOrDigit(source[i]) || source[i] is '\'' or '.' or '_'));
                continue;
            }

            if (ch == 'R' && i + 1 < source.Length && source[i + 1] == '"')
            {
                var open = source.IndexOf('(', i + 2);
                if (open >= 0 && open - i <= 18)
                {
                    var terminator = ")" + source[(i + 2)..open] + "\"";
                    var close = source.IndexOf(terminator, open + 1, StringComparison.Ordinal);
                    i = close < 0 ? source.Length : close + terminator.Length;
                    continue;
                }
            }
            if (ch is '"' or '\'')
            {
                i++;
                while (i < source.Length)
                {
                    if (source[i] == '\\') { i = Math.Min(i + 2, source.Length); continue; }
                    if (source[i++] == ch) break;
                }
                continue;
            }
            if (ch == '{') depth++;
            if (ch == '}')
            {
                depth--;
                groups.RemoveAll(group => group.Depth > depth);
            }
            if (ch is ';' or '{' or '}') pending = null;
            i++;
        }
        return result;
    }

    private static bool IsLineStart(string source, int index)
    {
        for (var i = index - 1; i >= 0 && source[i] != '\n'; i--)
            if (!char.IsWhiteSpace(source[i]) && source[i] != '\uFEFF') return false;
        return true;
    }

    private static bool IsGroupLine(string source, int index)
    {
        var end = source.IndexOf('\n', index);
        var text = source.AsSpan(index + 3, (end < 0 ? source.Length : end) - index - 3).Trim();
        return text is "@{" or "@}" or "\\{" or "\\}";
    }

    private static List<string> CleanComment(string text, bool block)
    {
        if (block)
        {
            text = text[2..(text.EndsWith("*/", StringComparison.Ordinal) ? ^2 : ^0)];
            if (text.StartsWith('*') || text.StartsWith('!')) text = text[1..];
        }
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .Select(line =>
            {
                line = line.Trim();
                if (block && line.StartsWith('*')) line = line[1..];
                else if (!block && (line.StartsWith("///", StringComparison.Ordinal) || line.StartsWith("//!", StringComparison.Ordinal))) line = line[3..];
                return line.Trim();
            }).ToList();
    }

    private static ApiDocumentation? ParseDocumentation(List<string> lines)
    {
        var paragraphs = new List<string>();
        var paragraph = new List<string>();
        void Flush()
        {
            if (paragraph.Count == 0) return;
            paragraphs.Add(string.Join('\n', paragraph));
            paragraph.Clear();
        }
        foreach (var original in lines)
        {
            var line = original;
            if (line is "@{" or "@}" or "\\{" or "\\}" || TargetCommand.IsMatch(line)) continue;
            if (line.StartsWith("@brief", StringComparison.Ordinal) || line.StartsWith("\\brief", StringComparison.Ordinal))
                line = line[6..].TrimStart();
            else if (line.StartsWith('@') || line.StartsWith('\\'))
                Flush(); // 第二阶段再转换参数、返回值和链接；本阶段以普通文本保留。
            if (line.Length == 0) Flush();
            else paragraph.Add(line);
        }
        Flush();
        if (paragraphs.Count == 0) return null;
        var hasSummary = paragraphs[0][0] is not ('@' or '\\');
        return new ApiDocumentation(hasSummary ? paragraphs[0] : null,
            paragraphs.Count > (hasSummary ? 1 : 0) ? string.Join("\n\n", paragraphs.Skip(hasSummary ? 1 : 0)) : null);
    }
}
