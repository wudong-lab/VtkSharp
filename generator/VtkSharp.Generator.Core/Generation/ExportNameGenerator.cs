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

    public Dictionary<TKey, string> CreateAll<TKey>(
        string className,
        IReadOnlyList<(TKey Key, string MethodName, IReadOnlyList<CanonicalType> ParameterTypes)> functions)
        where TKey : notnull
    {
        var overloadCounts = functions
            .GroupBy(f => f.MethodName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var names = functions
            .Select(f => (f.Key, Name: this.Create(className, f.MethodName, f.ParameterTypes, overloadCounts[f.MethodName] > 1)))
            .ToList();

        var collisions = names
            .GroupBy(x => x.Name, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g)
            .ToList();

        if (collisions.Count == 0)
            return names.ToDictionary(x => x.Key, x => x.Name);

        var result = new Dictionary<TKey, string>();
        foreach (var (key, name) in names)
        {
            if (collisions.Any(c => c.Key.Equals(key)))
            {
                var func = functions.First(f => f.Key.Equals(key));
                var canonical = string.Join(",", func.ParameterTypes.Select(t => t.Text));
                result[key] = this.CreateWithHash(className, func.MethodName, canonical);
            }
            else
            {
                result[key] = name;
            }
        }

        return result;
    }

    private string CreateWithHash(string className, string methodName, string canonicalSignature)
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
            "char" => "char",
            "unsigned int" => "uint",
            "unsigned long" => "ulong",
            "long long" => "long",
            "unsigned long long" => "ulong",
            "HWND" => "hwnd",
            "HDC" => "hdc",
            "HGLRC" => "hglrc",
            _ => text.Replace(" ", "", StringComparison.Ordinal),
        };
    }
}
