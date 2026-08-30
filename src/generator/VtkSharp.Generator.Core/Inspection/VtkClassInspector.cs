using CppAst;
using System.Text;
using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Types;

namespace VtkSharp.Generator.Core.Inspection;

public sealed class VtkClassInspector
{
    private readonly HashSet<string>? _enumHeaders;

    public VtkClassInspector(IEnumerable<string>? enumHeaders = null)
    {
        this._enumHeaders = enumHeaders?.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private readonly TypeCanonicalizer _canonicalizer = new();
    private readonly Dictionary<string, RawInspectedClass> _rawClassCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, InspectedClass> _classCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, VtkDocumentationExtractor> _documentationCache = new(StringComparer.OrdinalIgnoreCase);

    public InspectedClass InspectHeader(string includeDirectory, string headerFileName, string className)
    {
        var cacheKey = CreateCacheKey(includeDirectory, headerFileName, className);
        if (this._classCache.TryGetValue(cacheKey, out var cachedClass))
            return cachedClass;

        return this.BuildClass(includeDirectory, headerFileName, className, []);
    }

    public IReadOnlyDictionary<string, InspectedClass> InspectFile(string includeDirectory, string headerFileName)
    {
        var fullIncludeDir = Path.GetFullPath(includeDirectory);
        var options = CreateParserOptions(fullIncludeDir);
        var analyzeEnums = this._enumHeaders is null || this._enumHeaders.Contains(headerFileName);
        options.ParseFunctionBodies = analyzeEnums;
        var headerPath = Path.Combine(fullIncludeDir, headerFileName);
        var compilation = CppParser.ParseFile(headerPath, options);
        string? bodyError = null;
        if (compilation.HasErrors)
        {
            bodyError = string.Join("; ", compilation.Diagnostics.Messages.Where(d => d.Type == CppLogMessageType.Error).Take(3));
            // 方法体解析仅用于可选增强，失败不能阻断原有声明解析。
            options.ParseFunctionBodies = false;
            compilation = CppParser.ParseFile(headerPath, options);
        }
        if (compilation.HasErrors)
            throw new InvalidOperationException(string.Join(Environment.NewLine, compilation.Diagnostics));

        var result = new Dictionary<string, InspectedClass>(StringComparer.Ordinal);
        foreach (var cppClass in compilation.Classes)
        {
            if (!cppClass.Name.StartsWith("vtk", StringComparison.Ordinal)) continue;
            var cacheKey = CreateCacheKey(fullIncludeDir, headerFileName, cppClass.Name);
            if (this._rawClassCache.ContainsKey(cacheKey)) continue;
            var baseClassNames = GetCppBaseClassNames(cppClass);
            var rawClass = this.BuildRawClass(cppClass, baseClassNames, this._canonicalizer);
            if (analyzeEnums && Path.GetFullPath(cppClass.SourceFile).Equals(headerPath, StringComparison.OrdinalIgnoreCase))
            {
                var enums = EnumPropertyInspector.Inspect(cppClass, options, headerPath);
                if (bodyError is not null && enums.Diagnostics.Count > 0)
                    enums.Diagnostics.Add($"{cppClass.Name}: optional body analysis unavailable: {bodyError}");
                rawClass = rawClass with
                {
                    EnumProperties = enums.Properties, EnumDiagnostics = enums.Diagnostics,
                    Functions = rawClass.Functions.Select(f => enums.Properties.FirstOrDefault(p => p.Getter == f.Name || p.Setter == f.Name) is { } property
                        ? f with { IsSupported = true, SupportedEnumType = property.NativeType, DependencyTypes = [] } : f).ToList(),
                };
            }
            this._rawClassCache[cacheKey] = rawClass;
            result[cppClass.Name] = rawClass.ToInspectedClassWithBaseClassNames();
        }
        return result;
    }

    internal static CppParserOptions CreateParserOptions(string fullIncludeDir)
    {
        var options = new CppParserOptions();
        options.ConfigureForWindowsMsvc(CppTargetCpu.X86_64, CppVisualStudioVersion.VS2022);
        options.IncludeFolders.Add(fullIncludeDir);
        options.AdditionalArguments.Add("-std=c++17");
        // VTK 9.7's vtkDataSetAttributesFieldList.h uses std::vector without
        // including <vector>; real VTK translation units obtain it transitively.
        options.AdditionalArguments.Add("-include");
        options.AdditionalArguments.Add("vector");

        return options;
    }

    private InspectedClass BuildClass(
        string includeDirectory,
        string headerFileName,
        string className,
        HashSet<string> visitedClassNames)
    {
        var cacheKey = CreateCacheKey(includeDirectory, headerFileName, className);
        if (this._classCache.TryGetValue(cacheKey, out var cachedClass))
            return cachedClass;

        if (!visitedClassNames.Add(className))
        {
            var empty = new InspectedClass(className, []);
            this._classCache[cacheKey] = empty;
            return empty;
        }

        this.InspectFile(includeDirectory, headerFileName);

        var raw = this._rawClassCache.TryGetValue(cacheKey, out var rawClass)
            ? rawClass
            : throw new InvalidOperationException($"Class '{className}' was not found in '{headerFileName}'.");

        var directBaseClassName = raw.BaseClassNames.FirstOrDefault();
        var result = new InspectedClass(className, raw.Functions, raw.HasStaticNew, directBaseClassName, GetClassDependencies(raw.Functions),
            Documentation: raw.Documentation, NewDocumentation: raw.NewDocumentation,
            DeclaredMemberNames: raw.DeclaredMemberNames, HasMultipleBaseClasses: raw.BaseClassNames.Count > 1,
            EnumProperties: raw.EnumProperties, EnumDiagnostics: raw.EnumDiagnostics);
        this._classCache[cacheKey] = result;
        return result;
    }

    private RawInspectedClass BuildRawClass(CppClass cppClass, IReadOnlyList<string> baseClassNames, TypeCanonicalizer canonicalizer)
    {
        var staticNew = cppClass.Functions.FirstOrDefault(function =>
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
                var isSupported = BindingTypeMapper.IsSupportedType(returnType) &&
                                  parameters.All(parameter => BindingTypeMapper.IsSupportedType(parameter.Type));
                var deps = GetDependencyTypes([returnType, .. parameters.Select(p => p.Type)], cppClass.Name);

                return new InspectedFunction(
                    function.Name,
                    signature,
                    returnType,
                    parameters,
                    IsSupported: isSupported,
                    CanonicalSignature: $"{returnType} {function.Name}(" +
                                        string.Join(", ", parameters.Select(p => $"{p.Type} {p.Name}")) +
                                        ")",
                    DependencyTypes: deps,
                    Documentation: this.GetDocumentation(function)?.GetDeclarationDocumentation(function.Span.Start.Offset));
            })
            .ToList();

        return new RawInspectedClass(cppClass.Name, functions, staticNew is not null, baseClassNames,
            this.GetDocumentation(cppClass)?.GetClassDocumentation(cppClass.Name, cppClass.Span.Start.Offset),
            staticNew is null ? null : this.GetDocumentation(staticNew)?.GetDeclarationDocumentation(staticNew.Span.Start.Offset),
            cppClass.Functions.Select(function => function.Name).Concat(cppClass.Fields.Select(field => field.Name))
                .Distinct(StringComparer.Ordinal).ToList());
    }

    private VtkDocumentationExtractor? GetDocumentation(CppElement element)
    {
        var path = element.SourceFile;
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        if (!this._documentationCache.TryGetValue(path, out var documentation))
        {
            // 保留 UTF-8 BOM，确保源码字节位置与 Clang 一致。
            documentation = VtkDocumentationExtractor.Parse(Encoding.UTF8.GetString(File.ReadAllBytes(path)));
            this._documentationCache.Add(path, documentation);
        }
        return documentation;
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
            .Select(TypeClassifier.ExtractVtkClassName)
            .Where(typeName => typeName is not null && typeName != className)
            .Select(typeName => typeName!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
}
