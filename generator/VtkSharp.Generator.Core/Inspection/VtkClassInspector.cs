using CppAst;
using VtkSharp.Generator.Core.Types;

namespace VtkSharp.Generator.Core.Inspection;

public sealed record InspectedClass(
    string Name,
    IReadOnlyList<InspectedFunction> Functions,
    bool HasStaticNew = false);

public sealed record InspectedFunction(
    string Name,
    string CppSignature,
    string ReturnType,
    IReadOnlyList<InspectedParameter> Parameters,
    bool IsSupported);

public sealed record InspectedParameter(
    string Type,
    string Name);

public sealed class VtkClassInspector
{
    private readonly TypeCanonicalizer _canonicalizer = new();

    public InspectedClass InspectSynthetic(
        string className,
        IReadOnlyList<(string Name, string ReturnType, IReadOnlyList<(string Type, string Name)> Parameters)> functions)
    {
        var inspected = functions.Select(function =>
        {
            var parameters = function.Parameters
                .Select(parameter => new InspectedParameter(_canonicalizer.Canonicalize(parameter.Type).Text, parameter.Name))
                .ToList();

            var returnType = _canonicalizer.Canonicalize(function.ReturnType).Text;
            var signature = $"{returnType} {function.Name}(" +
                            string.Join(", ", parameters.Select(parameter => $"{parameter.Type} {parameter.Name}")) +
                            ")";

            return new InspectedFunction(function.Name, signature, returnType, parameters, IsSupported: true);
        }).ToList();

        return new InspectedClass(className, inspected);
    }

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
                var parameters = function.Parameters
                    .Select((parameter, index) =>
                    {
                        var name = string.IsNullOrWhiteSpace(parameter.Name) ? $"_arg{index + 1}" : parameter.Name;
                        return new InspectedParameter(_canonicalizer.Canonicalize(parameter.Type.FullName).Text, name);
                    })
                    .ToList();

                var returnType = _canonicalizer.Canonicalize(function.ReturnType.FullName).Text;
                var signature = $"{returnType} {function.Name}(" +
                                string.Join(", ", parameters.Select(parameter => $"{parameter.Type} {parameter.Name}")) +
                                ")";

                return new InspectedFunction(function.Name, signature, returnType, parameters, IsSupported: true);
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

        return new InspectedClass(className, functions, hasStaticNew);
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
}
