using VtkSharp.Generator.Core.Configuration;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Core.Generation;

public sealed class BindingGenerationService
{
    public const string IncrementalCacheVersion = "2026-07-01.incremental-v1";

    public int Generate(string configPath, string outputRoot, bool continueOnError, bool incremental, bool force, TextWriter output, TextWriter error)
        => incremental
            ? this.GenerateIncremental(configPath, outputRoot, continueOnError, force, output, error)
            : this.GenerateFull(configPath, outputRoot, continueOnError, output, error);

    public int CheckGeneratedOutput(string configPath, string outputRoot, TextWriter output, TextWriter error)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        var currentManagedDirectory = workspace.GetManagedOutputDirectory();
        var currentNativeDirectory = workspace.GetNativeOutputDirectory();
        var currentNativeProjectFile = workspace.GetNativeProjectFile();
        var currentModulesFile = workspace.GetNativeModulesFile();
        var generatedManagedDirectory = Path.Combine(outputRoot, "bindings", "VtkSharp");
        var generatedNativeDirectory = Path.Combine(outputRoot, "bindings", "VtkSharp.Native", "src");
        var generatedNativeProjectFile = Path.Combine(outputRoot, "bindings", "VtkSharp.Native", "CMakeLists.txt");
        var generatedModulesFile = Path.Combine(outputRoot, "bindings", "VtkSharp.Native", "vtksharp.modules.generated.cmake");

        var comparer = new GeneratedOutputComparer();
        var differences = comparer.CompareDirectories(currentManagedDirectory, generatedManagedDirectory, "*_gen.cs")
            .Concat(comparer.CompareDirectories(currentNativeDirectory, generatedNativeDirectory, "*_export_gen.cpp"))
            .Concat(comparer.CompareFiles(currentNativeProjectFile, generatedNativeProjectFile, "bindings/VtkSharp.Native/CMakeLists.txt"))
            .Concat(comparer.CompareFiles(currentModulesFile, generatedModulesFile, "bindings/VtkSharp.Native/vtksharp.modules.generated.cmake"))
            .ToList();

        if (differences.Count == 0)
        {
            output.WriteLine("Generated output is up to date.");
            return 0;
        }

        error.WriteLine("Generated output differs from current files:");
        foreach (var difference in differences)
        {
            error.WriteLine($"  {difference.RelativePath}: {difference.Message}");
        }

        error.WriteLine($"Generated output root: {Path.GetFullPath(outputRoot)}");
        return 1;
    }

    private int GenerateFull(string configPath, string outputRoot, bool continueOnError, TextWriter output, TextWriter error)
    {
        var context = new GeneratorRunContextFactory().Create(configPath, error);
        if (context is null)
            return 1;

        var validationResult = new WhitelistValidationService().Validate(context, continueOnError, "text", output, error);
        if (validationResult != 0 && !continueOnError)
            return validationResult;

        Directory.CreateDirectory(outputRoot);

        var csharpEmitter = new CSharpBindingEmitter();
        var cppEmitter = new CppExportEmitter();
        var config = context.Workspace.Config;
        var manualClasses = config.Binding.ManualBindingClasses.ToHashSet(StringComparer.Ordinal);
        var skippedCount = 0;

        foreach (var document in context.Documents)
        {
            foreach (var whitelistClass in document.Classes)
            {
                if (manualClasses.Contains(whitelistClass.Name))
                    continue;

                if (!context.InspectedClasses.TryGetValue(whitelistClass.Name, out var inspectedClass))
                {
                    if (continueOnError)
                    {
                        skippedCount++;
                        continue;
                    }

                    error.WriteLine($"Class '{whitelistClass.Name}' could not be inspected.");
                    return 1;
                }

                var baseClassName = context.HierarchyResolver.GetBaseClassName(whitelistClass.Name);
                var managedPath = Path.Combine(outputRoot, "bindings", "VtkSharp", document.Module, $"{whitelistClass.Name}_gen.cs");
                var nativePath = Path.Combine(outputRoot, "bindings", "VtkSharp.Native", "src", document.Module, $"{whitelistClass.Name}_export_gen.cpp");
                var includeClassNames = GetIncludeClassNames(whitelistClass)
                    .Where(name => name != whitelistClass.Name)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                WriteText(managedPath, csharpEmitter.Emit(config.Binding.Namespace, whitelistClass.Name, baseClassName, inspectedClass.HasStaticNew, whitelistClass.Functions));
                WriteText(nativePath, cppEmitter.Emit(whitelistClass.Name, includeClassNames, inspectedClass.HasStaticNew, whitelistClass.Functions));
            }
        }

        WriteNativeProjectFiles(outputRoot, config, context.Documents);

        if (continueOnError && skippedCount > 0)
        {
            output.WriteLine($"Skipped {skippedCount} class(es) due to inspection/validation failures.");
        }

        output.WriteLine($"Generated files will be written to: {Path.GetFullPath(outputRoot)}");
        return 0;
    }

    private int GenerateIncremental(string configPath, string outputRoot, bool continueOnError, bool force, TextWriter output, TextWriter error)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        if (workspace.IncludeDirectory is null)
        {
            error.WriteLine("VTK include directory was not found. Set VTK_ROOT or vtk.includeDirectory in local config.");
            return 1;
        }

        Directory.CreateDirectory(outputRoot);

        var config = workspace.Config;
        var documents = workspace.LoadWhitelist();
        var hierarchyResolver = workspace.LoadHierarchyResolver();
        var inspector = new VtkClassInspector();
        var validator = new WhitelistValidator();
        var csharpEmitter = new CSharpBindingEmitter();
        var cppEmitter = new CppExportEmitter();
        var manifestStore = new GeneratedManifestStore();
        var manualClasses = config.Binding.ManualBindingClasses.ToHashSet(StringComparer.Ordinal);
        var manifests = new Dictionary<string, GeneratedManifest>(StringComparer.Ordinal);
        var generatedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;

        foreach (var document in documents)
        {
            var manifestPath = GetManifestPath(outputRoot, document.Module);
            var manifest = force
                ? new GeneratedManifest { SchemaVersion = GeneratedManifestStore.CurrentSchemaVersion, GeneratorVersion = IncrementalCacheVersion, Module = document.Module }
                : manifestStore.Load(manifestPath, document.Module, IncrementalCacheVersion);
            manifests[document.Module] = manifest;

            foreach (var whitelistClass in document.Classes)
            {
                if (manualClasses.Contains(whitelistClass.Name))
                    continue;

                var baseClassName = hierarchyResolver.GetBaseClassName(whitelistClass.Name);
                var managedPath = Path.Combine(outputRoot, "bindings", "VtkSharp", document.Module, $"{whitelistClass.Name}_gen.cs");
                var nativePath = Path.Combine(outputRoot, "bindings", "VtkSharp.Native", "src", document.Module, $"{whitelistClass.Name}_export_gen.cpp");
                var managedRelativePath = Path.Combine("bindings", "VtkSharp", document.Module, $"{whitelistClass.Name}_gen.cs");
                var nativeRelativePath = Path.Combine("bindings", "VtkSharp.Native", "src", document.Module, $"{whitelistClass.Name}_export_gen.cpp");
                var headerPath = Path.Combine(workspace.IncludeDirectory, whitelistClass.Header);
                var inputHash = GenerationInputFingerprint.Compute(
                    IncrementalCacheVersion,
                    config.Vtk.Version,
                    config.Binding.Namespace,
                    config.Binding.NativeLibraryName,
                    document.Module,
                    whitelistClass.Name,
                    whitelistClass.Header,
                    baseClassName,
                    GenerationInputFingerprint.HashFileText(headerPath),
                    whitelistClass.Functions);

                if (!force && GeneratedManifestCache.TryGetReusableEntry(manifest, whitelistClass.Name, inputHash, managedPath, nativePath, out _))
                {
                    skippedCount++;
                    continue;
                }

                InspectedClass inspectedClass;
                try
                {
                    inspectedClass = inspector.InspectHeader(workspace.IncludeDirectory, whitelistClass.Header, whitelistClass.Name);
                }
                catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
                {
                    failedCount++;
                    error.WriteLine($"Class '{whitelistClass.Name}' could not be inspected from '{whitelistClass.Header}': {ex.Message}");
                    if (!continueOnError)
                        return 1;
                    continue;
                }

                var validationDocument = new WhitelistDocument
                {
                    Module = document.Module,
                    Classes = [whitelistClass],
                };
                var validationResult = validator.Validate(
                    validationDocument,
                    new Dictionary<string, InspectedClass>(StringComparer.Ordinal) { [whitelistClass.Name] = inspectedClass },
                    hierarchyResolver);
                if (validationResult.Diagnostics.Count > 0)
                {
                    failedCount++;
                    foreach (var diagnostic in validationResult.Diagnostics)
                        error.WriteLine(diagnostic.Message);
                    if (!continueOnError)
                        return 1;
                    continue;
                }

                var includeClassNames = GetIncludeClassNames(whitelistClass)
                    .Where(name => name != whitelistClass.Name)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                var managedContent = csharpEmitter.Emit(config.Binding.Namespace, whitelistClass.Name, baseClassName, inspectedClass.HasStaticNew, whitelistClass.Functions);
                var nativeContent = cppEmitter.Emit(whitelistClass.Name, includeClassNames, inspectedClass.HasStaticNew, whitelistClass.Functions);

                WriteText(managedPath, managedContent);
                WriteText(nativePath, nativeContent);

                var entry = new GeneratedManifestEntry
                {
                    ClassName = whitelistClass.Name,
                    Header = whitelistClass.Header,
                    BaseClassName = baseClassName,
                    HasStaticNew = inspectedClass.HasStaticNew,
                    InputHash = inputHash,
                    ManagedPath = managedRelativePath,
                    NativePath = nativeRelativePath,
                    ManagedContentHash = GenerationInputFingerprint.HashGeneratedText(managedContent),
                    NativeContentHash = GenerationInputFingerprint.HashGeneratedText(nativeContent),
                };
                manifest = GeneratedManifestStore.WithEntry(manifest, entry);
                manifests[document.Module] = manifest;
                generatedCount++;
            }
        }

        WriteNativeProjectFiles(outputRoot, config, documents);

        foreach (var (module, manifest) in manifests)
        {
            manifestStore.Save(GetManifestPath(outputRoot, module), manifest);
        }

        output.WriteLine($"Generated files will be written to: {Path.GetFullPath(outputRoot)}");
        output.WriteLine($"Incremental generation: generated {generatedCount} class(es), reused {skippedCount} class(es).");
        if (continueOnError && failedCount > 0)
        {
            output.WriteLine($"Skipped {failedCount} class(es) due to inspection/validation failures.");
        }

        return 0;
    }

    private static void WriteNativeProjectFiles(string outputRoot, GeneratorConfig config, IReadOnlyList<WhitelistDocument> documents)
    {
        var cmakeEmitter = new CMakeModulesEmitter();
        var nativeProjectEmitter = new NativeProjectEmitter();
        var modulesPath = Path.Combine(outputRoot, "bindings", "VtkSharp.Native", "vtksharp.modules.generated.cmake");
        var vtkModules = documents
            .Select(document => document.Module)
            .Concat(config.Vtk.RuntimeModules)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        WriteText(modulesPath, cmakeEmitter.Emit(vtkModules));
        WriteText(Path.Combine(outputRoot, "bindings", "VtkSharp.Native", "CMakeLists.txt"), nativeProjectEmitter.EmitCMakeLists(config.Binding.NativeLibraryName));
        WriteText(Path.Combine(outputRoot, "bindings", "VtkSharp.Native", "CMakePresets.json"), nativeProjectEmitter.EmitCMakePresets());
        WriteText(Path.Combine(outputRoot, "bindings", "VtkSharp.Native", "include", "vtksharp_api.h"), nativeProjectEmitter.EmitApiHeader());
    }

    private static string GetManifestPath(string outputRoot, string module)
        => Path.Combine(outputRoot, "bindings", "VtkSharp", module, GeneratedManifestStore.FileName);

    private static void WriteText(string path, string content)
        => GeneratedFileWriter.WriteIfChanged(path, content);

    private static IEnumerable<string> GetIncludeClassNames(WhitelistClass whitelistClass)
    {
        foreach (var function in whitelistClass.Functions)
        {
            foreach (var typeName in function.Parameters.Select(parameter => parameter.Type).Append(function.Return.Type))
            {
                var className = TypeClassifier.ExtractVtkClassName(typeName);
                if (className is not null)
                    yield return className;
            }
        }
    }
}
