using System.Security.Cryptography;
using System.Text;
using VtkSharp.Generator.Core.Types;

namespace VtkSharp.Generator.Core.Generation;

public sealed class ExportNameGenerator
{
    public string Create(string className, string methodName, IReadOnlyList<CanonicalType> parameterTypes, bool hasOverloads)
    {
        if (!hasOverloads)
            return $"{className}_{methodName}";

        var suffix = string.Join("_", parameterTypes.Select(ToSuffix));
        return $"{className}_{methodName}_{suffix}";
    }

    public string CreateWithHash(string className, string methodName, string canonicalSignature)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalSignature));
        var hash = Convert.ToHexString(bytes).ToLowerInvariant()[..6];
        return $"{className}_{methodName}_h{hash}";
    }

    private static string ToSuffix(CanonicalType type)
    {
        var text = type.Text;

        if (text is "const char*")
            return "constCharPtr";

        if (text is "char*")
            return "charPtr";

        if (text is "void*")
            return "voidPtr";

        if (text.EndsWith("*", StringComparison.Ordinal))
        {
            var core = text[..^1].Replace("const ", "", StringComparison.Ordinal);
            return text.StartsWith("const ", StringComparison.Ordinal) ? $"{core}ConstPtr" : $"{core}Ptr";
        }

        if (text.StartsWith("const ", StringComparison.Ordinal) && text.Contains('[', StringComparison.Ordinal))
            return text.Replace("const ", "", StringComparison.Ordinal).Replace("[", "ConstArray", StringComparison.Ordinal).Replace("]", "", StringComparison.Ordinal);

        if (text.Contains('[', StringComparison.Ordinal))
            return text.Replace("[", "Array", StringComparison.Ordinal).Replace("]", "", StringComparison.Ordinal);

        return text switch
        {
            "unsigned int" => "uint",
            "long long" => "long",
            "unsigned long long" => "ulong",
            "HWND" => "hwnd",
            "HDC" => "hdc",
            "HGLRC" => "hglrc",
            _ => text.Replace(" ", "", StringComparison.Ordinal),
        };
    }
}
