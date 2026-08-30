using System.Text;
using System.Text.RegularExpressions;
using CppAst;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Core.Inspection;

/// <summary>只接受直接声明的标量 Get/Set 和无副作用的常量便捷转发。</summary>
internal static partial class EnumPropertyInspector
{
    private static readonly HashSet<string> WrapperMembers = new(StringComparer.Ordinal)
    {
        "New", "Register", "FromBorrowedPointer", "TakeReference", "NativePointer", "Dispose",
    };
    internal static (List<EnumProperty> Properties, List<string> Diagnostics) Inspect(
        CppClass type, CppParserOptions options, string headerPath)
    {
        var properties = new List<EnumProperty>();
        var diagnostics = new List<string>();
        var functions = type.Functions.Where(f => f.Visibility == CppVisibility.Public && !f.IsStatic).ToList();
        var inheritedNames = GetBaseNames(type, []);
        foreach (var getter in functions.Where(f => f.Name.StartsWith("Get", StringComparison.Ordinal) && f.Parameters.Count == 0))
        {
            var name = getter.Name[3..];
            var setters = functions.Where(f => f.Name == "Set" + name).ToList();
            var helpers = functions.Where(f => f.Name.StartsWith("Set" + name + "To", StringComparison.Ordinal)).ToList();
            var nativeEnum = getter.ReturnType as CppEnum ?? type.Enums.FirstOrDefault(e => e.FullName == getter.ReturnType.FullName);
            if (helpers.Count == 0 && nativeEnum is null) continue;
            string? reason = null;
            if (!Identifier().IsMatch(name) || type.Name == name || WrapperMembers.Contains(name) || type.Functions.Any(f => f.Name == name))
                reason = "managed enum name conflicts with a member or is not an identifier";
            else if (type.BaseTypes.Count > 1 || inheritedNames.Any(n => n == getter.Name || n == "Set" + name || n.StartsWith("Set" + name + "To", StringComparison.Ordinal)))
                reason = "override, hiding or extended inherited enum contract is outside the supported scope";
            else if (setters.Count != 1 || functions.Count(f => f.Name == getter.Name) != 1 ||
                     setters[0].Parameters.Count != 1 || setters[0].ReturnType.FullName != "void" ||
                     setters[0].Parameters[0].Type.FullName != getter.ReturnType.FullName)
                reason = "get/set signatures are overloaded or not a matching scalar pair";
            else if (nativeEnum is null ? getter.ReturnType.FullName != "int" :
                     nativeEnum.Visibility != CppVisibility.Public || nativeEnum.IntegerType.GetCanonicalType().FullName != "int")
                reason = "only int properties and public enums with int underlying type are supported";
            else if (nativeEnum is null && helpers.Count < 2)
                reason = "an int property needs at least two constant choices; a lone reset/default method does not establish enum semantics";

            var values = new List<EnumValue>();
            var expressions = new List<string>();
            if (reason is null)
            {
                foreach (var helper in helpers)
                {
                    if (string.IsNullOrEmpty(helper.SourceFile) || !Path.GetFullPath(helper.SourceFile).Equals(headerPath, StringComparison.OrdinalIgnoreCase))
                    {
                        reason = $"{helper.Name} is defined outside the declaring header";
                        break;
                    }
                    var body = ReadBody(helper);
                    var match = Forwarder().Match(body);
                    if (!Identifier().IsMatch(helper.Name[(5 + name.Length)..]))
                    {
                        reason = $"{helper.Name} produces an unsupported C# enum member identifier";
                        break;
                    }
                    if (helper.Parameters.Count != 0 || helper.ReturnType.FullName != "void" || !match.Success ||
                        match.Groups["setter"].Value != "Set" + name)
                    {
                        reason = $"{helper.Name} is not a simple constant setter forwarder";
                        break;
                    }
                    var expression = match.Groups["value"].Value;
                    expressions.Add(expression);
                    if (nativeEnum is null)
                        values.Add(new EnumValue { Name = helper.Name[(5 + name.Length)..], NativeExpression = expression });
                }
            }
            if (reason is null && nativeEnum is not null)
            {
                if (nativeEnum.Items.Count == 0 || nativeEnum.Items.Any(v => !Identifier().IsMatch(v.Name) || v.Name == name ||
                    v.Value < int.MinValue || v.Value > int.MaxValue ||
                    v.ValueExpression?.ToString()?.Contains("<<", StringComparison.Ordinal) == true ||
                    v.ValueExpression?.ToString()?.Contains('|') == true))
                    reason = "empty, flags-like or unrepresentable native enum";
                else
                    values.AddRange(nativeEnum.Items.Select(v => new EnumValue
                    {
                        Name = v.Name, NativeExpression = nativeEnum.FullName + "::" + v.Name, Value = (int)v.Value,
                    }));
            }
            if (reason is null && expressions.Count > 0)
            {
                // 在原方法位置插入仅用于解析的枚举常量，保留类作用域和该处的预处理状态。
                // 不修改磁盘头文件、不执行 native 代码，也不自行解释 C++ 表达式。
                var probeOptions = VtkClassInspector.CreateParserOptions(options.IncludeFolders[0]);
                var source = File.ReadAllBytes(headerPath).ToList();
                foreach (var i in Enumerable.Range(0, helpers.Count).OrderByDescending(i => helpers[i].Span.Start.Offset))
                {
                    var expression = nativeEnum is null ? expressions[i] : $"static_cast<int>({expressions[i]})";
                    var declaration = $"enum : int {{ VtkSharpEnumProbe{i} = {expression} }};\n";
                    source.InsertRange(helpers[i].Span.Start.Offset, Encoding.UTF8.GetBytes(declaration));
                }
                var probe = CppParser.Parse(Encoding.UTF8.GetString(source.ToArray()), probeOptions, headerPath);
                var evaluated = probe.Classes.FirstOrDefault(c => c.FullName == type.FullName)?.Enums.SelectMany(e => e.Items)
                    .Where(v => v.Name.StartsWith("VtkSharpEnumProbe", StringComparison.Ordinal))
                    .ToDictionary(v => v.Name, v => v.Value);
                if (probe.HasErrors || evaluated?.Count != expressions.Count)
                    reason = "constant values cannot be resolved in the configured header context";
                else if (nativeEnum is null)
                {
                    for (var i = 0; i < values.Count; i++) values[i] = values[i] with { Value = checked((int)evaluated[$"VtkSharpEnumProbe{i}"]) };
                }
                else if (evaluated.Values.Any(v => !values.Any(item => item.Value == v)))
                    reason = "convenience setter value is outside the declared enum members";
            }
            if (reason is null && (values.Count == 0 || values.Any(v => v.Name == name || v.Name == "value__") || values.Select(v => v.Name).Distinct().Count() != values.Count))
                reason = "empty or conflicting enum member names";
            if (reason is null && nativeEnum is null && values.Select(v => v.Value).Distinct().Count() < 2)
                reason = "int property choices do not establish two distinct values";
            if (reason is not null)
            {
                diagnostics.Add($"{type.Name}.{name}: enum conversion skipped: {reason}; ordinary function export is unchanged.");
                continue;
            }
            properties.Add(new EnumProperty
            {
                Name = name, NativeType = getter.ReturnType.FullName, Getter = getter.Name, Setter = setters[0].Name,
                Values = values.OrderBy(v => v.Name, StringComparer.Ordinal).ToList(),
                ConvenienceMethods = helpers.Select(f => f.Name).Order(StringComparer.Ordinal).ToList(),
            });
        }
        return (properties, diagnostics);
    }

    private static HashSet<string> GetBaseNames(CppClass type, HashSet<string> visited)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (!visited.Add(type.FullName)) return names;
        foreach (var baseType in type.BaseTypes)
            if (baseType.Type.GetCanonicalType() is CppClass parent)
            {
                names.UnionWith(parent.Functions.Select(f => f.Name));
                names.UnionWith(GetBaseNames(parent, visited));
            }
        return names;
    }

    private static string ReadBody(CppFunction function)
    {
        if (function.BodySpan is not { } span || string.IsNullOrEmpty(function.SourceFile) || span.End.Offset <= span.Start.Offset) return "";
        var bytes = File.ReadAllBytes(function.SourceFile);
        if (span.Start.Offset < 0 || span.End.Offset > bytes.Length) return "";
        var body = Encoding.UTF8.GetString(bytes, span.Start.Offset, span.End.Offset - span.Start.Offset);
        // 有条件编译时不根据原始文本猜测实际执行分支。
        return body.Contains('#') ? "" : Comments().Replace(body, " ");
    }

    [GeneratedRegex(@"\A[A-Za-z_][A-Za-z0-9_]*\z")]
    private static partial Regex Identifier();
    [GeneratedRegex(@"\A\s*\{\s*(?:this\s*->\s*)?(?<setter>Set[A-Za-z0-9_]+)\s*\(\s*(?<value>(?:::)?[A-Za-z_][A-Za-z0-9_]*(?:::[A-Za-z_][A-Za-z0-9_]*)*|[+-]?[0-9]+)\s*\)\s*;\s*\}\s*\z")]
    private static partial Regex Forwarder();
    [GeneratedRegex(@"/\*[\s\S]*?\*/|//[^\r\n]*")]
    private static partial Regex Comments();
}
