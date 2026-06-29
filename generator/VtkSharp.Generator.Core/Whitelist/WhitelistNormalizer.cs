using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Types;
using VtkSharp.Generator.Core.Vtk;

namespace VtkSharp.Generator.Core.Whitelist;

public sealed class WhitelistNormalizer
{
    private readonly TypeCanonicalizer _canonicalizer = new();

    public IReadOnlyList<WhitelistDocument> Normalize(
        IReadOnlyList<WhitelistDocument> documents,
        IReadOnlyDictionary<string, VtkHierarchyEntry> hierarchyEntries,
        IReadOnlyCollection<string> manualBindingClasses)
    {
        var manualClasses = manualBindingClasses.ToHashSet(StringComparer.Ordinal);
        var documentsByModule = documents
            .Select(CloneDocument)
            .ToDictionary(document => document.Module, StringComparer.Ordinal);
        var classesByName = documentsByModule.Values
            .SelectMany(document => document.Classes.Select(whitelistClass => (document, whitelistClass)))
            .ToDictionary(item => item.whitelistClass.Name, item => item, StringComparer.Ordinal);

        foreach (var whitelistClass in classesByName.Values.ToList())
        {
            NormalizeClass(whitelistClass.whitelistClass);
            AddBaseClassChain(whitelistClass.whitelistClass.Name, hierarchyEntries, documentsByModule, classesByName, manualClasses);
            AddDependencyClasses(whitelistClass.whitelistClass, hierarchyEntries, documentsByModule, classesByName, manualClasses);
        }

        return documentsByModule.Values
            .OrderBy(document => document.Module, StringComparer.Ordinal)
            .Select(document => new WhitelistDocument
            {
                Module = document.Module,
                Classes = document.Classes
                    .OrderBy(item => item.Name, StringComparer.Ordinal)
                    .Select(NormalizeClass)
                    .ToList(),
            })
            .ToList();
    }

    private static WhitelistDocument CloneDocument(WhitelistDocument document)
        => new()
        {
            Module = document.Module,
            Classes = document.Classes.Select(CloneClass).ToList(),
        };

    private static WhitelistClass CloneClass(WhitelistClass whitelistClass)
        => new()
        {
            Name = whitelistClass.Name,
            Header = whitelistClass.Header,
            Functions = whitelistClass.Functions.Select(CloneFunction).ToList(),
        };

    private static WhitelistFunction CloneFunction(WhitelistFunction function)
        => new()
        {
            Name = function.Name,
            CppSignature = function.CppSignature,
            Return = new WhitelistReturn { Type = function.Return.Type, Ownership = function.Return.Ownership },
            Parameters = function.Parameters.Select(parameter => new WhitelistParameter
            {
                Type = parameter.Type,
                Name = parameter.Name,
                Direction = parameter.Direction,
                Length = parameter.Length is null
                    ? null
                    : new WhitelistLength
                    {
                        Kind = parameter.Length.Kind,
                        Value = parameter.Length.Value,
                        Name = parameter.Length.Name,
                    },
            }).ToList(),
        };

    private WhitelistClass NormalizeClass(WhitelistClass whitelistClass)
        => whitelistClass with
        {
            Functions = whitelistClass.Functions
                .OrderBy(function => function.Name, StringComparer.Ordinal)
                .ThenBy(function => function.CppSignature, StringComparer.Ordinal)
                .Select(NormalizeFunction)
                .ToList(),
        };

    private WhitelistFunction NormalizeFunction(WhitelistFunction function)
        => function with
        {
            Return = function.Return with { Type = _canonicalizer.Canonicalize(function.Return.Type).Text },
            Parameters = function.Parameters
                .Select(parameter => parameter with { Type = _canonicalizer.Canonicalize(parameter.Type).Text })
                .ToList(),
        };

    private void AddDependencyClasses(
        WhitelistClass whitelistClass,
        IReadOnlyDictionary<string, VtkHierarchyEntry> hierarchyEntries,
        Dictionary<string, WhitelistDocument> documentsByModule,
        Dictionary<string, (WhitelistDocument document, WhitelistClass whitelistClass)> classesByName,
        HashSet<string> manualClasses)
    {
        foreach (var dependencyClassName in whitelistClass.Functions.SelectMany(GetDependencyClassNames).Distinct(StringComparer.Ordinal))
        {
            AddClass(dependencyClassName, hierarchyEntries, documentsByModule, classesByName, manualClasses);
            AddBaseClassChain(dependencyClassName, hierarchyEntries, documentsByModule, classesByName, manualClasses);
        }
    }

    private IEnumerable<string> GetDependencyClassNames(WhitelistFunction function)
    {
        foreach (var type in function.Parameters.Select(parameter => parameter.Type).Append(function.Return.Type))
        {
            var canonical = _canonicalizer.Canonicalize(type).Text;
            var className = ExtractVtkClassName(canonical);
            if (className is not null)
                yield return className;
        }
    }

    private static void AddBaseClassChain(
        string className,
        IReadOnlyDictionary<string, VtkHierarchyEntry> hierarchyEntries,
        Dictionary<string, WhitelistDocument> documentsByModule,
        Dictionary<string, (WhitelistDocument document, WhitelistClass whitelistClass)> classesByName,
        HashSet<string> manualClasses)
    {
        var currentClassName = className;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (hierarchyEntries.TryGetValue(currentClassName, out var entry) &&
               !string.IsNullOrWhiteSpace(entry.BaseClassName) &&
               visited.Add(currentClassName))
        {
            var baseClassName = entry.BaseClassName;
            if (manualClasses.Contains(baseClassName))
                break;

            AddClass(baseClassName, hierarchyEntries, documentsByModule, classesByName, manualClasses);
            currentClassName = baseClassName;
        }
    }

    private static void AddClass(
        string className,
        IReadOnlyDictionary<string, VtkHierarchyEntry> hierarchyEntries,
        Dictionary<string, WhitelistDocument> documentsByModule,
        Dictionary<string, (WhitelistDocument document, WhitelistClass whitelistClass)> classesByName,
        HashSet<string> manualClasses)
    {
        if (manualClasses.Contains(className) || classesByName.ContainsKey(className))
            return;

        if (!hierarchyEntries.TryGetValue(className, out var entry))
            return;

        if (!documentsByModule.TryGetValue(entry.Module, out var document))
        {
            document = new WhitelistDocument { Module = entry.Module };
            documentsByModule.Add(entry.Module, document);
        }

        var whitelistClass = new WhitelistClass
        {
            Name = entry.ClassName,
            Header = entry.Header,
            Functions = [],
        };
        document.Classes.Add(whitelistClass);
        classesByName.Add(className, (document, whitelistClass));
    }

    private static string? ExtractVtkClassName(string type)
    {
        var normalized = type.Replace("const", "", StringComparison.Ordinal)
            .Replace("*", "", StringComparison.Ordinal)
            .Replace("&", "", StringComparison.Ordinal)
            .Trim();

        var nestedTypeSeparator = normalized.IndexOf("::", StringComparison.Ordinal);
        if (nestedTypeSeparator >= 0)
            normalized = normalized[..nestedTypeSeparator];

        return normalized.StartsWith("vtk", StringComparison.Ordinal) &&
               normalized is not "vtkTypeBool" and not "vtkTypeUInt32" and not "vtkIdType" and not "vtkMTimeType"
               && !TypeClassifier.IsVtkValueStruct(normalized)
            ? normalized
            : null;
    }
}
