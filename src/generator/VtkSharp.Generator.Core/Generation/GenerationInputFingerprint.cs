using System.Security.Cryptography;
using System.Text;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Core.Generation;

public static class GenerationInputFingerprint
{
    public static string Compute(
        string generatorVersion,
        string vtkVersion,
        string bindingNamespace,
        string nativeLibraryName,
        string module,
        string className,
        string header,
        string baseClassName,
        string headerContentHash,
        IReadOnlyList<WhitelistFunction> functions)
    {
        var sb = new StringBuilder();
        Append(sb, "generatorVersion", generatorVersion);
        Append(sb, "vtkVersion", vtkVersion);
        Append(sb, "bindingNamespace", bindingNamespace);
        Append(sb, "nativeLibraryName", nativeLibraryName);
        Append(sb, "module", module);
        Append(sb, "className", className);
        Append(sb, "header", header);
        Append(sb, "baseClassName", baseClassName);
        Append(sb, "headerContentHash", headerContentHash);

        sb.AppendLine("functions");
        foreach (var function in functions)
        {
            Append(sb, "function.name", function.Name);
            Append(sb, "function.cppSignature", function.CppSignature);
            Append(sb, "function.return.type", function.Return.Type);
            Append(sb, "function.return.ownership", function.Return.Ownership ?? "");
            foreach (var parameter in function.Parameters)
            {
                Append(sb, "parameter.type", parameter.Type);
                Append(sb, "parameter.name", parameter.Name);
                Append(sb, "parameter.direction", parameter.Direction ?? "");
                Append(sb, "parameter.length.kind", parameter.Length?.Kind ?? "");
                Append(sb, "parameter.length.value", parameter.Length?.Value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "");
                Append(sb, "parameter.length.name", parameter.Length?.Name ?? "");
            }
        }

        return HashText(sb.ToString());
    }

    public static string HashFileText(string path)
        => File.Exists(path) ? HashNormalizedText(File.ReadAllText(path)) : "";

    public static string HashGeneratedText(string text)
        => HashNormalizedText(text);

    private static string HashNormalizedText(string text)
        => HashText(NormalizeLineEndings(text));

    private static string HashText(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string NormalizeLineEndings(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);

    private static void Append(StringBuilder sb, string key, string value)
        => sb.Append(key).Append('=').Append(value.Length).Append(':').Append(value).AppendLine();
}
