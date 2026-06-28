using CppAst;
using VtkSharp.Generator.Core.Types;

namespace VtkSharp.Generator.Core.Inspection;

public sealed class VtkClassInspector
{
    private readonly TypeCanonicalizer _canonicalizer = new();
    private readonly Dictionary<string, InspectedClass> _cache = new(StringComparer.Ordinal);

    public InspectedClass InspectHeader(string includeDirectory, string headerFileName, string className)
    {
        var cacheKey = CreateCacheKey(includeDirectory, headerFileName, className);
        if (this._cache.TryGetValue(cacheKey, out var cachedClass))
            return cachedClass;

        return this.BuildClass(includeDirectory, headerFileName, className, []);
    }

    public IReadOnlyDictionary<string, InspectedClass> InspectFile(string includeDirectory, string headerFileName)
    {
        var fullIncludeDir = Path.GetFullPath(includeDirectory);
        var options = new CppParserOptions();
        options.ConfigureForWindowsMsvc(CppTargetCpu.X86_64, CppVisualStudioVersion.VS2022);
        options.IncludeFolders.Add(fullIncludeDir);

        var headerPath = Path.Combine(fullIncludeDir, headerFileName);
        var compilation = CppParser.ParseFile(headerPath, options);
        if (compilation.HasErrors)
            throw new InvalidOperationException(string.Join(Environment.NewLine, compilation.Diagnostics));

        var result = new Dictionary<string, InspectedClass>(StringComparer.Ordinal);
        foreach (var cppClass in compilation.Classes)
        {
            if (!cppClass.Name.StartsWith("vtk", StringComparison.Ordinal))
                continue;

            var cacheKey = CreateCacheKey(fullIncludeDir, headerFileName, cppClass.Name);
            if (this._cache.ContainsKey(cacheKey))
                continue;

            var baseClassNames = GetCppBaseClassNames(cppClass);
            var rawClass = BuildRawClass(cppClass, baseClassNames, this._canonicalizer);
            this._cache[cacheKey] = rawClass;
            result[cppClass.Name] = rawClass;
        }

        return result;
    }

    private InspectedClass BuildClass(
        string includeDirectory,
        string headerFileName,
        string className,
        HashSet<string> visitedClassNames)
    {
        var cacheKey = CreateCacheKey(includeDirectory, headerFileName, className);
        if (this._cache.TryGetValue(cacheKey, out var cachedClass) &&
            cachedClass.BaseClassNames is null)
            return cachedClass;

        if (!visitedClassNames.Add(className))
        {
            var empty = new InspectedClass(className, []);
            this._cache[cacheKey] = empty;
            return empty;
        }

        this.InspectFile(includeDirectory, headerFileName);

        var raw = this._cache.TryGetValue(cacheKey, out var rawClass)
            ? rawClass
            : throw new InvalidOperationException($"Class '{className}' was not found in '{headerFileName}'.");

        var directBaseClassName = (raw.BaseClassNames ?? []).FirstOrDefault();
        var result = new InspectedClass(className, raw.Functions, raw.HasStaticNew, directBaseClassName, GetClassDependencies(raw.Functions));
        this._cache[cacheKey] = result;
        return result;
    }

    private static InspectedClass BuildRawClass(CppClass cppClass, IReadOnlyList<string> baseClassNames, TypeCanonicalizer canonicalizer)
    {
        var hasStaticNew = cppClass.Functions.Any(function =>
            function.Visibility == CppVisibility.Public &&
            function.Name == "New" &&
            function.IsStatic &&
            function.Parameters.Count == 0 &&
            function.ReturnType.FullName.Contains(cppClass.Name, StringComparison.Ordinal));

        var functions = cppClass.Functions
            .Where(static function =>
                function.Visibility == CppVisibility.Public &&
                !function.IsConstructor &&
                !function.IsDestructor &&
                !function.IsStatic &&
                !function.IsFunctionTemplate)
            .Select(function =>
            {
                var rawParameters = function.Parameters
                    .Select((parameter, index) =>
                    {
                        var name = string.IsNullOrWhiteSpace(parameter.Name) ? $"_arg{index + 1}" : parameter.Name;
                        return new InspectedParameter(parameter.Type.FullName, name);
                    })
                    .ToList();

                var rawReturnType = function.ReturnType.FullName;
                var signature = $"{rawReturnType} {function.Name}(" +
                                string.Join(", ", rawParameters.Select(p => $"{p.Type} {p.Name}")) +
                                ")";

                var parameters = rawParameters
                    .Select(p => new InspectedParameter(canonicalizer.Canonicalize(p.Type).Text, p.Name))
                    .ToList();
                var returnType = canonicalizer.Canonicalize(rawReturnType).Text;
                var deps = GetDependencyTypes([returnType, .. parameters.Select(p => p.Type)], cppClass.Name);

                return new InspectedFunction(
                    function.Name,
                    signature,
                    returnType,
                    parameters,
                    IsSupported: true,
                    CanonicalSignature: $"{returnType} {function.Name}(" +
                                        string.Join(", ", parameters.Select(p => $"{p.Type} {p.Name}")) +
                                        ")",
                    DependencyTypes: deps);
            })
            .ToList();

        return new InspectedClass(cppClass.Name, functions, hasStaticNew, BaseClassNames: baseClassNames);
    }

    private static IReadOnlyList<string> GetCppBaseClassNames(CppClass cppClass)
    {
        return cppClass.BaseTypes
            .Select(baseType => baseType.Type.FullName
                .Split("::", StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault())
            .Where(name => !string.IsNullOrWhiteSpace(name) && name.StartsWith("vtk", StringComparison.Ordinal))
            .Select(name => name!)
            .ToList();
    }

    private static string CreateCacheKey(string includeDirectory, string headerFileName, string className)
        => $"{Path.GetFullPath(includeDirectory)}|{headerFileName}|{className}";

    private static IReadOnlyList<string> GetClassDependencies(IEnumerable<InspectedFunction> functions)
        => functions
            .SelectMany(function => function.DependencyTypes ?? [])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

    private static IReadOnlyList<string> GetDependencyTypes(IEnumerable<string> typeNames, string className)
        => typeNames
            .Select(ExtractVtkClassName)
            .Where(typeName => typeName is not null && typeName != className)
            .Select(typeName => typeName!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

    private static string? ExtractVtkClassName(string typeName)
    {
        var text = typeName
            .Replace("const", "", StringComparison.Ordinal)
            .Replace("*", "", StringComparison.Ordinal)
            .Replace("&", "", StringComparison.Ordinal)
            .Trim();

        var nestedTypeSeparator = text.IndexOf("::", StringComparison.Ordinal);
        if (nestedTypeSeparator >= 0)
            text = text[..nestedTypeSeparator];

        return text.StartsWith("vtk", StringComparison.Ordinal) ? text : null;
    }
}
