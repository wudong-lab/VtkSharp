using System.CommandLine;
using System.Text.Json;
using VtkSharp.Generator.Core.Configuration;
using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;
using VtkSharp.Generator.Core.Vtk;
using VtkSharp.Generator.Core.Whitelist;

internal class Program
{
    public static int Main(string[] args)
    {
        var classArgument = new Argument<string>("class-name");
        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: text or json",
            DefaultValueFactory = _ => "text",
        };
        var configOption = new Option<FileInfo>("--config")
        {
            Description = "Generator config file",
        };

        var inspectClassCommand = new Command("inspect-class", "Inspect a VTK class")
        {
            classArgument,
            formatOption,
            configOption,
        };

        inspectClassCommand.SetAction(parseResult =>
        {
            var className = parseResult.GetValue(classArgument)!;
            var format = parseResult.GetValue(formatOption)!;
            var configPath = parseResult.GetValue(configOption)?.FullName
                             ?? GetDefaultConfigPath();

            return InspectClass(configPath, className, format);
        });

        var validateCommand = new Command("validate-whitelist", "Validate whitelist")
        {
            configOption,
        };

        validateCommand.SetAction(parseResult =>
        {
            var configPath = parseResult.GetValue(configOption)?.FullName
                             ?? GetDefaultConfigPath();

            return ValidateWhitelist(configPath);
        });

        var normalizeCommand = new Command("normalize-whitelist", "Normalize whitelist files")
        {
            configOption,
        };

        normalizeCommand.SetAction(parseResult =>
        {
            var configPath = parseResult.GetValue(configOption)?.FullName
                             ?? GetDefaultConfigPath();

            return NormalizeWhitelist(configPath);
        });

        var outputRootOption = new Option<DirectoryInfo>("--output-root")
        {
            Description = "Temporary output root",
        };
        var checkOption = new Option<bool>("--check")
        {
            Description = "Generate to a temporary directory and compare with current generated files",
        };

        var generateCommand = new Command("generate", "Generate bindings")
        {
            configOption,
            outputRootOption,
            checkOption,
        };

        generateCommand.SetAction(parseResult =>
        {
            var check = parseResult.GetValue(checkOption);
            var outputRoot = parseResult.GetValue(outputRootOption);
            var configPath = parseResult.GetValue(configOption)?.FullName
                             ?? GetDefaultConfigPath();
            var outputRootPath = outputRoot?.FullName
                                 ?? (check
                                     ? Path.Combine(Path.GetTempPath(), "VtkSharp.Generator", "check", Guid.NewGuid().ToString("N"))
                                     : throw new InvalidOperationException("--output-root is required unless --check is specified."));

            var exitCode = Generate(configPath, outputRootPath);
            if (exitCode != 0)
                return exitCode;

            return check ? CheckGeneratedOutput(configPath, outputRootPath) : 0;
        });

        var rootCommand = new RootCommand("VtkSharp binding generator")
        {
            inspectClassCommand,
            validateCommand,
            normalizeCommand,
            generateCommand,
        };

        return rootCommand.Parse(args).Invoke();
    }

    private static string GetDefaultConfigPath()
        => Path.GetFullPath(Path.Combine("generator", "config", "vtksharp.generator.yml"));

    private static int InspectClass(string configPath, string className, string format)
    {
        var config = LoadConfig(configPath);
        var includeDirectory = ResolveIncludeDirectory(config);
        if (includeDirectory is null)
        {
            Console.Error.WriteLine("VTK include directory was not found. Set VTK_ROOT or vtk.includeDirectory in local config.");
            return 1;
        }

        var hierarchyResolver = LoadHierarchyResolver(config);
        var header = hierarchyResolver.GetHeader(className);
        var inspected = new VtkClassInspector().InspectHeader(includeDirectory, header, className) with
        {
            BaseClassName = hierarchyResolver.GetBaseClassName(className),
        };

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(JsonSerializer.Serialize(inspected, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        if (!format.Equals("text", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("--format must be 'text' or 'json'.");
            return 1;
        }

        Console.WriteLine(inspected.Name);
        Console.WriteLine($"  Header: {header}");
        Console.WriteLine($"  BaseClass: {inspected.BaseClassName}");
        Console.WriteLine($"  HasStaticNew: {inspected.HasStaticNew}");
        Console.WriteLine($"  Dependencies: {string.Join(", ", inspected.Dependencies ?? [])}");
        foreach (var function in inspected.Functions)
        {
            Console.WriteLine($"  {function.Name}");
            Console.WriteLine($"    Cpp: {function.CppSignature}");
            Console.WriteLine($"    Canonical: {function.CanonicalSignature}");
            Console.WriteLine($"    Dependencies: {string.Join(", ", function.DependencyTypes ?? [])}");
        }

        return 0;
    }

    private static int Generate(string configPath, string outputRoot)
    {
        var validationExitCode = ValidateWhitelist(configPath);
        if (validationExitCode != 0)
            return validationExitCode;

        var configDirectory = Path.GetDirectoryName(configPath)
                              ?? throw new InvalidOperationException($"Config path '{configPath}' does not have a directory.");

        var config = LoadConfig(configPath);
        var whitelistDirectory = Path.GetFullPath(Path.Combine(configDirectory, config.Paths.WhitelistDirectory));
        var documents = new WhitelistLoader().LoadDirectory(whitelistDirectory);

        Directory.CreateDirectory(outputRoot);

        var csharpEmitter = new CSharpBindingEmitter();
        var cppEmitter = new CppExportEmitter();
        var cmakeEmitter = new CMakeModulesEmitter();
        var nativeProjectEmitter = new NativeProjectEmitter();
        var manualClasses = config.Binding.ManualBindingClasses.ToHashSet(StringComparer.Ordinal);
        var inspector = new VtkClassInspector();
        var includeDirectory = ResolveIncludeDirectory(config);
        var hierarchyResolver = LoadHierarchyResolver(config);

        foreach (var document in documents)
        {
            foreach (var whitelistClass in document.Classes)
            {
                if (manualClasses.Contains(whitelistClass.Name))
                    continue;

                var baseClassName = hierarchyResolver.GetBaseClassName(whitelistClass.Name);
                var managedPath = Path.Combine(outputRoot, "bindings", "VtkSharp", document.Module, $"{whitelistClass.Name}_gen.cs");
                var nativePath = Path.Combine(outputRoot, "native", "src", document.Module, $"{whitelistClass.Name}_export_gen.cpp");
                var includeClassNames = GetIncludeClassNames(whitelistClass)
                    .Where(name => name != whitelistClass.Name)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();
                var hasStaticNew = includeDirectory is not null && TryInspectHasStaticNew(inspector, includeDirectory, whitelistClass);

                WriteText(managedPath, csharpEmitter.Emit(config.Binding.Namespace, whitelistClass.Name, baseClassName, hasStaticNew, whitelistClass.Functions));
                WriteText(nativePath, cppEmitter.Emit(whitelistClass.Name, includeClassNames, hasStaticNew, whitelistClass.Functions));
            }
        }

        var modulesPath = Path.Combine(outputRoot, "native", "vtksharp.modules.generated.cmake");
        WriteText(modulesPath, cmakeEmitter.Emit(documents.Select(document => document.Module).ToList()));
        WriteText(Path.Combine(outputRoot, "native", "CMakeLists.txt"), nativeProjectEmitter.EmitCMakeLists(config.Binding.NativeLibraryName));
        WriteText(Path.Combine(outputRoot, "native", "CMakePresets.json"), nativeProjectEmitter.EmitCMakePresets());
        WriteText(Path.Combine(outputRoot, "native", "include", "vtksharp_api.h"), nativeProjectEmitter.EmitApiHeader());

        Console.WriteLine($"Generated files will be written to: {Path.GetFullPath(outputRoot)}");
        return 0;
    }

    private static int NormalizeWhitelist(string configPath)
    {
        var configDirectory = Path.GetDirectoryName(configPath)
                              ?? throw new InvalidOperationException($"Config path '{configPath}' does not have a directory.");

        var config = LoadConfig(configPath);
        var whitelistDirectory = Path.GetFullPath(Path.Combine(configDirectory, config.Paths.WhitelistDirectory));
        var hierarchyEntries = LoadHierarchyEntries(config);
        if (hierarchyEntries.Count == 0)
        {
            Console.Error.WriteLine("VTK hierarchy directory was not found or contains no hierarchy entries. Set VTK_ROOT or vtk.hierarchyDirectory in local config.");
            return 1;
        }

        var documents = new WhitelistLoader().LoadDirectory(whitelistDirectory);
        var normalized = new WhitelistNormalizer().Normalize(documents, hierarchyEntries, config.Binding.ManualBindingClasses);
        new WhitelistWriter().WriteDirectory(whitelistDirectory, normalized);

        Console.WriteLine($"Whitelist files normalized under: {whitelistDirectory}");
        Console.WriteLine("Review the changes with git diff before committing.");
        return 0;
    }

    private static int CheckGeneratedOutput(string configPath, string outputRoot)
    {
        var configDirectory = Path.GetDirectoryName(configPath)
                              ?? throw new InvalidOperationException($"Config path '{configPath}' does not have a directory.");

        var config = LoadConfig(configPath);
        var currentManagedDirectory = Path.GetFullPath(Path.Combine(configDirectory, config.Paths.ManagedOutputDirectory));
        var currentNativeDirectory = Path.GetFullPath(Path.Combine(configDirectory, config.Paths.NativeOutputDirectory));
        var currentModulesFile = Path.GetFullPath(Path.Combine(configDirectory, config.Paths.NativeModulesFile));
        var generatedManagedDirectory = Path.Combine(outputRoot, "bindings", "VtkSharp");
        var generatedNativeDirectory = Path.Combine(outputRoot, "native", "src");
        var generatedModulesFile = Path.Combine(outputRoot, "native", "vtksharp.modules.generated.cmake");

        var comparer = new GeneratedOutputComparer();
        var differences = comparer.CompareDirectories(currentManagedDirectory, generatedManagedDirectory, "*_gen.cs")
            .Concat(comparer.CompareDirectories(currentNativeDirectory, generatedNativeDirectory, "*_export_gen.cpp"))
            .Concat(comparer.CompareFiles(currentModulesFile, generatedModulesFile, "native/vtksharp.modules.generated.cmake"))
            .ToList();

        if (differences.Count == 0)
        {
            Console.WriteLine("Generated output is up to date.");
            return 0;
        }

        Console.Error.WriteLine("Generated output differs from current files:");
        foreach (var difference in differences)
            Console.Error.WriteLine($"  {difference.RelativePath}: {difference.Message}");
        Console.Error.WriteLine($"Generated output root: {Path.GetFullPath(outputRoot)}");
        return 1;
    }

    private static void WriteText(string path, string content)
    {
        var directory = Path.GetDirectoryName(path)
                        ?? throw new InvalidOperationException($"Output path '{path}' does not have a directory.");

        Directory.CreateDirectory(directory);
        File.WriteAllText(path, content);
    }

    private static IEnumerable<string> GetIncludeClassNames(WhitelistClass whitelistClass)
    {
        foreach (var function in whitelistClass.Functions)
        {
            foreach (var typeName in function.Parameters.Select(parameter => parameter.Type).Append(function.Return.Type))
            {
                var className = ExtractVtkClassName(typeName);
                if (className is not null)
                    yield return className;
            }
        }
    }

    private static string? ExtractVtkClassName(string typeName)
    {
        var normalized = typeName.Replace("const", "", StringComparison.Ordinal)
            .Replace("*", "", StringComparison.Ordinal)
            .Trim();

        return normalized.StartsWith("vtk", StringComparison.Ordinal) ? normalized : null;
    }

    private static string? ResolveIncludeDirectory(GeneratorConfig config)
    {
        var candidates = new[]
        {
            config.Vtk.IncludeDirectory,
            config.Vtk.RootDirectory is null ? null : Path.Combine(config.Vtk.RootDirectory, "include", $"vtk-{config.Vtk.Version}"),
            config.Vtk.RootDirectory is null ? null : Path.Combine(config.Vtk.RootDirectory, "include"),
        };

        return candidates.FirstOrDefault(path => path is not null && Directory.Exists(path));
    }

    private static string? ResolveHierarchyDirectory(GeneratorConfig config)
    {
        var candidates = new[]
        {
            config.Vtk.HierarchyDirectory,
            config.Vtk.RootDirectory is null ? null : Path.Combine(config.Vtk.RootDirectory, "lib", $"vtk-{config.Vtk.Version}", "hierarchy", "VTK"),
            config.Vtk.RootDirectory is null ? null : Path.Combine(config.Vtk.RootDirectory, "lib", $"vtk-{config.Vtk.Version}", "hierarchy"),
        };

        return candidates.FirstOrDefault(path => path is not null && Directory.Exists(path));
    }

    private static VtkHierarchyResolver LoadHierarchyResolver(GeneratorConfig config)
        => new(LoadHierarchyEntries(config));

    private static IReadOnlyDictionary<string, VtkHierarchyEntry> LoadHierarchyEntries(GeneratorConfig config)
    {
        var hierarchyDirectory = ResolveHierarchyDirectory(config);
        return hierarchyDirectory is null
            ? new Dictionary<string, VtkHierarchyEntry>(StringComparer.Ordinal)
            : new VtkHierarchyReader().ReadDirectory(hierarchyDirectory);
    }

    private static GeneratorConfig LoadConfig(string configPath)
    {
        var configDirectory = Path.GetDirectoryName(configPath)
                              ?? throw new InvalidOperationException($"Config path '{configPath}' does not have a directory.");

        var localConfigPath = Path.Combine(configDirectory, "vtksharp.generator.local.yml");
        return new GeneratorConfigLoader().Load(configPath, localConfigPath);
    }

    private static bool TryInspectHasStaticNew(VtkClassInspector inspector, string includeDirectory, WhitelistClass whitelistClass)
    {
        try
        {
            return inspector.InspectHeader(includeDirectory, whitelistClass.Header, whitelistClass.Name).HasStaticNew;
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
        {
            return false;
        }
    }

    private static int ValidateWhitelist(string configPath)
    {
        var configDirectory = Path.GetDirectoryName(configPath)
                              ?? throw new InvalidOperationException($"Config path '{configPath}' does not have a directory.");

        var config = LoadConfig(configPath);
        var whitelistDirectory = Path.GetFullPath(Path.Combine(configDirectory, config.Paths.WhitelistDirectory));
        var includeDirectory = ResolveIncludeDirectory(config);
        if (includeDirectory is null)
        {
            Console.Error.WriteLine("VTK include directory was not found. Set VTK_ROOT or vtk.includeDirectory in local config.");
            return 1;
        }

        var documents = new WhitelistLoader().LoadDirectory(whitelistDirectory);
        var inspector = new VtkClassInspector();
        var inspectedClasses = new Dictionary<string, InspectedClass>(StringComparer.Ordinal);
        var diagnostics = new List<ValidationDiagnostic>();

        foreach (var whitelistClass in documents.SelectMany(document => document.Classes))
        {
            try
            {
                inspectedClasses[whitelistClass.Name] = inspector.InspectHeader(includeDirectory, whitelistClass.Header, whitelistClass.Name);
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
            {
                diagnostics.Add(new ValidationDiagnostic($"Class '{whitelistClass.Name}' could not be inspected from '{whitelistClass.Header}': {ex.Message}"));
            }
        }

        var validator = new WhitelistValidator();
        foreach (var document in documents)
            diagnostics.AddRange(validator.Validate(document, inspectedClasses).Diagnostics);

        if (diagnostics.Count == 0)
        {
            Console.WriteLine("Whitelist validation succeeded.");
            return 0;
        }

        foreach (var diagnostic in diagnostics)
            Console.Error.WriteLine(diagnostic.Message);

        return 1;
    }
}