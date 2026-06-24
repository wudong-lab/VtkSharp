using CppAst;
using VtkSharp.Generator.Core.Types;

namespace VtkSharp.Generator.Core.Inspection;

public sealed record InspectedClass(
    string Name,
    IReadOnlyList<InspectedFunction> Functions,
    bool HasStaticNew = false,
    string? BaseClassName = null,
    IReadOnlyList<string>? Dependencies = null);

public sealed record InspectedFunction(
    string Name,
    string CppSignature,
    string ReturnType,
    IReadOnlyList<InspectedParameter> Parameters,
    bool IsSupported,
    string? CanonicalSignature = null,
    IReadOnlyList<string>? DependencyTypes = null);

public sealed record InspectedParameter(
    string Type,
    string Name);

public sealed class VtkClassInspector
{
    private readonly TypeCanonicalizer _canonicalizer = new();

    public InspectedClass InspectHeader(string includeDirectory, string headerFileName, string className)
        => InspectHeader(includeDirectory, headerFileName, className, []);

    private InspectedClass InspectHeader(
        string includeDirectory,
        string headerFileName,
        string className,
        HashSet<string> visitedClassNames)
    {
        if (!visitedClassNames.Add(className))
            return new InspectedClass(className, []);

        var options = new CppParserOptions();
        options.ConfigureForWindowsMsvc(CppTargetCpu.X86_64, CppVisualStudioVersion.VS2022);
        options.IncludeFolders.Add(includeDirectory);

        var headerPath = Path.Combine(includeDirectory, headerFileName);
        var compilation = CppParser.ParseFile(headerPath, options);
        if (compilation.HasErrors)
            throw new InvalidOperationException(string.Join(Environment.NewLine, compilation.Diagnostics));

        var cppClass = compilation.Classes.FirstOrDefault(item => item.Name == className)
            ?? throw new InvalidOperationException($"Class '{className}' was not found in '{headerFileName}'.");

        var hasStaticNew = cppClass.Functions.Any(function =>
            function.Visibility == CppVisibility.Public &&
            function.Name == "New" &&
            function.IsStatic &&
            function.Parameters.Count == 0 &&
            function.ReturnType.FullName.Contains(className, StringComparison.Ordinal));

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
                                string.Join(", ", rawParameters.Select(parameter => $"{parameter.Type} {parameter.Name}")) +
                                ")";

                var parameters = rawParameters
                    .Select(parameter => new InspectedParameter(_canonicalizer.Canonicalize(parameter.Type).Text, parameter.Name))
                    .ToList();
                var returnType = _canonicalizer.Canonicalize(rawReturnType).Text;
                var canonicalSignature = $"{returnType} {function.Name}(" +
                                         string.Join(", ", parameters.Select(parameter => $"{parameter.Type} {parameter.Name}")) +
                                         ")";
                var dependencies = GetDependencyTypes([returnType, .. parameters.Select(parameter => parameter.Type)], className);

                return new InspectedFunction(
                    function.Name,
                    signature,
                    returnType,
                    parameters,
                    IsSupported: true,
                    CanonicalSignature: canonicalSignature,
                    DependencyTypes: dependencies);
            })
            .ToList();

        foreach (var baseClassName in GetBaseClassNames(cppClass))
        {
            var baseHeaderFileName = $"{baseClassName}.h";
            var baseHeaderPath = Path.Combine(includeDirectory, baseHeaderFileName);
            if (!File.Exists(baseHeaderPath))
                continue;

            var baseClass = InspectHeader(includeDirectory, baseHeaderFileName, baseClassName, visitedClassNames);
            foreach (var function in baseClass.Functions)
            {
                if (!functions.Any(item => HasSameSignature(item, function)))
                    functions.Add(function);
            }
        }

        var directBaseClassName = GetBaseClassNames(cppClass).FirstOrDefault();
        return new InspectedClass(className, functions, hasStaticNew, directBaseClassName, GetClassDependencies(functions));
    }

    private static IEnumerable<string> GetBaseClassNames(CppClass cppClass)
    {
        foreach (var baseType in cppClass.BaseTypes)
        {
            var name = baseType.Type.FullName
                .Split("::", StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault()
                ?.Trim();

            if (!string.IsNullOrWhiteSpace(name) && name.StartsWith("vtk", StringComparison.Ordinal))
                yield return name;
        }
    }

    private static bool HasSameSignature(InspectedFunction left, InspectedFunction right)
        => left.Name == right.Name &&
           left.ReturnType == right.ReturnType &&
           left.Parameters.Select(parameter => parameter.Type).SequenceEqual(right.Parameters.Select(parameter => parameter.Type));

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
