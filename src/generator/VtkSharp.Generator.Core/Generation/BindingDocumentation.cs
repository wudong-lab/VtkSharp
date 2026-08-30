using System.Globalization;
using System.Text.RegularExpressions;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Types;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Core.Generation;

internal static class BindingDocumentation
{
    internal const string OwnedReference = "The C# wrapper owns a native reference. Call Dispose() when finished to release that reference.";
    private const string BorrowedReference = "The C# wrapper borrows the native object without adding a reference. Dispose() does not release the borrowed reference. Use the wrapper only while the native object remains alive.";

    public static ApiDocumentation ForNew(ApiDocumentation? source)
        => new(source?.Summary, Join(source?.Remarks, OwnedReference), Returns: source?.Returns);

    public static ApiDocumentation FromBorrowedPointer { get; } = new(
        "Wraps a live native object without adding a reference or taking ownership.",
        "Dispose() does not release the borrowed reference. Keep the native object alive while using this wrapper.",
        [new("nativePointer", "A non-null pointer to a live native object of the corresponding VTK type.")],
        "A wrapper that does not own a native reference.");

    public static ApiDocumentation TakeReference { get; } = new(
        "Takes ownership of one existing native reference without incrementing the reference count.",
        "The caller transfers responsibility for releasing this reference to the wrapper; do not release it separately or transfer it twice. Call Dispose() when finished.",
        [new("nativePointer", "A non-null pointer to a live native object of the corresponding VTK type, with one owned reference to transfer.")],
        "A wrapper that releases the transferred reference on Dispose().");

    public static ApiDocumentation Register { get; } = new(
        "Creates another wrapper for the same native object and increments its reference count by one.",
        "The source wrapper's ownership is unchanged. Call Dispose() on the returned wrapper to release the additional reference. This does not copy the native object.",
        [new("sourceObject", "A wrapper for a native object that is still alive.")],
        "A wrapper that owns the additional native reference independently of the source wrapper.");

    public static ApiDocumentation ForMethod(WhitelistFunction function, InspectedFunction? inspected, Action<string>? warning = null)
    {
        var source = inspected?.Documentation;
        var parameters = new List<ParameterDocumentation>();
        for (var index = 0; index < function.Parameters.Count; index++)
        {
            var parameter = function.Parameters[index];
            var originalName = inspected?.Parameters[index].Name;
            var descriptions = source?.Parameters?.Where(p => p.Name == originalName).ToList();
            var text = descriptions is null ? null : Join(descriptions.Select(p => p.Text).ToArray());
            var direction = parameter.Direction ?? descriptions?.FirstOrDefault()?.Direction;
            var directionText = direction switch
            {
                "in" => "Input parameter.",
                "out" => "Output parameter.",
                "inout" or "in,out" => "Input/output parameter.",
                _ => null,
            };
            parameters.Add(new ParameterDocumentation(parameter.Name, Join(text, directionText, GetLengthDescription(parameter)) ?? ""));
        }

        string? ownership = null;
        if (TypeClassifier.TryGetVtkClassPointerName(function.Return.Type, out _))
        {
            ownership = function.Return.Ownership == "owned" ? OwnedReference : BorrowedReference;
            // 只报告明确的冲突模式，不尝试理解任意自然语言中的所有权契约。
            if (function.Return.Ownership != "owned" && Regex.IsMatch(
                Join(source?.Summary, source?.Remarks, source?.Returns) ?? "",
                @"\bcaller\s+is\s+responsible\s+for\s+(?:deleting|freeing)\b", RegexOptions.IgnoreCase))
                warning?.Invoke("VTK documentation assigns deletion to the caller, but the C# wrapper uses borrowed ownership. Review the whitelist ownership.");
        }
        else if (BindingTypeMapper.IsVtkStringValue(function.Return.Type) || BindingTypeMapper.IsStringPointer(function.Return.Type))
            ownership = "The result is copied to a managed string. The caller does not release native memory for this return value.";
        else if (TypeClassifier.IsVtkValueStruct(function.Return.Type))
            ownership = "The result is copied to a C# value type. The caller does not release native memory for this return value.";

        // 无法确定返回指针的长度/所有者时不补写；上游明确的返回说明直接保留。
        return new ApiDocumentation(source?.Summary, Join(source?.Remarks, ownership),
            parameters.Any(p => p.Text.Length > 0) ? parameters : null,
            function.Return.Type == "void" ? null : source?.Returns);
    }

    private static string? GetLengthDescription(WhitelistParameter parameter)
    {
        var length = parameter.Length;
        if (length is { Kind: "fixed", Value: > 0 })
            return $"Buffer length: {length.Value.Value.ToString(CultureInfo.InvariantCulture)} elements.";
        if (length is { Kind: "parameter", Name: not null })
            return $"The number of buffer elements is specified by {length.Name}.";
        var match = Regex.Match(parameter.Type, @"^[^\[\]]+\[([1-9][0-9]*)\]$");
        return match.Success ? $"Buffer length: {match.Groups[1].Value} elements." : null;
    }

    private static string? Join(params string?[] parts)
    {
        var text = string.Join("\n\n", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        return text.Length == 0 ? null : text;
    }
}
