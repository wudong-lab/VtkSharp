using System.CommandLine;
using System.Text.Json;
using VtkSharp.Generator.Core.Configuration;
using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;
using VtkSharp.Generator.Core.Whitelist;

internal class Program
{
    public static int Main(string[] args)
    {
        var classArgument = new Argument<string>("class-name")
        {
            Description = "VTK class name (e.g. vtkActor)",
        };
        var formatOption = new Option<string>("--format")
        {
            Description = "Output format: text or json",
            DefaultValueFactory = _ => "text",
        };
        formatOption.AcceptOnlyFromAmong("text", "json");
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

        var functionArgument = new Argument<string>("function-name");
        var inspectFunctionCommand = new Command("inspect-function", "Inspect a VTK method and show suggested whitelist entry")
        {
            classArgument,
            functionArgument,
            formatOption,
            configOption,
        };

        inspectFunctionCommand.SetAction(parseResult =>
        {
            var className = parseResult.GetValue(classArgument)!;
            var functionName = parseResult.GetValue(functionArgument)!;
            var format = parseResult.GetValue(formatOption)!;
            var configPath = parseResult.GetValue(configOption)?.FullName
                             ?? GetDefaultConfigPath();

            return InspectFunction(configPath, className, functionName, format);
        });

        var listModulesCommand = new Command("list-modules", "List VTK modules in the hierarchy")
        {
            configOption,
        };

        listModulesCommand.SetAction(parseResult =>
        {
            var configPath = parseResult.GetValue(configOption)?.FullName
                             ?? GetDefaultConfigPath();

            return ListModules(configPath);
        });

        var moduleArgument = new Option<string>("--module")
        {
            Description = "Filter classes by VTK module name",
        };

        var listClassesCommand = new Command("list-classes", "List VTK classes from hierarchy")
        {
            moduleArgument,
            formatOption,
            configOption,
        };

        listClassesCommand.SetAction(parseResult =>
        {
            var module = parseResult.GetValue(moduleArgument);
            var format = parseResult.GetValue(formatOption)!;
            var configPath = parseResult.GetValue(configOption)?.FullName
                             ?? GetDefaultConfigPath();

            return ListClasses(configPath, module, format);
        });

        var continueOnErrorOption = new Option<bool>("--continue-on-error")
        {
            Description = "Collect all errors instead of stopping on first failure.",
        };

        var validateCommand = new Command("validate-whitelist", "Validate whitelist against VTK headers")
        {
            configOption,
            continueOnErrorOption,
            formatOption,
        };

        validateCommand.SetAction(parseResult =>
        {
            var configPath = parseResult.GetValue(configOption)?.FullName
                             ?? GetDefaultConfigPath();
            var continueOnError = parseResult.GetValue(continueOnErrorOption);
            var format = parseResult.GetValue(formatOption)!;

            return ValidateWhitelist(configPath, continueOnError, format);
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
            Description = "Output root directory for generated files",
        };
        var checkOption = new Option<bool>("--check")
        {
            Description = "Generate to a temporary directory and compare with current generated files",
        };
        var incrementalOption = new Option<bool>("--incremental")
        {
            Description = "Reuse per-class generated output manifests and only regenerate changed classes.",
        };
        var forceOption = new Option<bool>("--force")
        {
            Description = "Ignore existing incremental manifests and regenerate all classes.",
        };

        var generateCommand = new Command("generate-bindings", "Generate C# and C++ bindings from the whitelist")
        {
            configOption,
            outputRootOption,
            checkOption,
            continueOnErrorOption,
            incrementalOption,
            forceOption,
        };

        generateCommand.SetAction(parseResult =>
        {
            var check = parseResult.GetValue(checkOption);
            var outputRoot = parseResult.GetValue(outputRootOption);
            var continueOnError = parseResult.GetValue(continueOnErrorOption);
            var incremental = parseResult.GetValue(incrementalOption);
            var force = parseResult.GetValue(forceOption);
            var configPath = parseResult.GetValue(configOption)?.FullName
                             ?? GetDefaultConfigPath();
            var outputRootPath = outputRoot?.FullName
                                 ?? (check
                                     ? Path.Combine(Path.GetTempPath(), "VtkSharp.Generator", "check", Guid.NewGuid().ToString("N"))
                                     : throw new InvalidOperationException("--output-root is required unless --check is specified."));

            var exitCode = Generate(configPath, outputRootPath, continueOnError, incremental && !check, force);
            if (exitCode != 0)
                return exitCode;

            return check ? CheckGeneratedOutput(configPath, outputRootPath) : 0;
        });

        var candidatePathArgument = new Argument<FileInfo>("candidate-path")
        {
            Description = "Path to candidate whitelist YAML file",
        };

        var diffWhitelistCommand = new Command("diff-whitelist", "Diff a candidate whitelist against the formal whitelist")
        {
            candidatePathArgument,
            formatOption,
            configOption,
        };

        diffWhitelistCommand.SetAction(parseResult =>
        {
            var candidatePath = parseResult.GetValue(candidatePathArgument)!.FullName;
            var format = parseResult.GetValue(formatOption)!;
            var configPath = parseResult.GetValue(configOption)?.FullName
                             ?? GetDefaultConfigPath();

            return DiffWhitelist(configPath, candidatePath, format);
        });

        var outputOption = new Option<FileInfo>("--output")
        {
            Description = "Output path for the candidate YAML file",
            Required = true,
        };
        outputOption.Aliases.Add("-o");
        var sourceKindOption = new Option<string>("--source-kind")
        {
            Description = "Source kind: vtk-example or manual",
            Required = true,
        };
        sourceKindOption.AcceptOnlyFromAmong("vtk-example", "manual");
        var sourceNameOption = new Option<string>("--source-name")
        {
            Description = "Source name (e.g. example name)",
        };
        var sourceOriginalOption = new Option<string>("--source-original")
        {
            Description = "Original C++ source path",
        };

        var supportedOnlyOption = new Option<bool>("--supported-only")
        {
            Description = "Only include functions whose types are all supported (filters out basic_ostream, int&, etc.)",
        };
        var skipMissingMethodsOption = new Option<bool>("--skip-missing-methods")
        {
            Description = "Skip methods that are not found instead of failing",
        };
        var methodsOption = new Option<string[]>("--methods")
        {
            Description = "Only include the specified method names (e.g. --methods SetResolution SetHeight). If omitted, all methods are included.",
            AllowMultipleArgumentsPerToken = true,
        };

        var createCandidateCommand = new Command("create-candidate", "Create a candidate whitelist from VTK inspection")
        {
            classArgument,
            outputOption,
            sourceKindOption,
            sourceNameOption,
            sourceOriginalOption,
            supportedOnlyOption,
            methodsOption,
            skipMissingMethodsOption,
            configOption,
        };

        createCandidateCommand.SetAction(parseResult =>
        {
            var className = parseResult.GetValue(classArgument)!;
            var outputPath = parseResult.GetValue(outputOption)!.FullName;
            var sourceKind = parseResult.GetValue(sourceKindOption)!;
            var sourceName = parseResult.GetValue(sourceNameOption);
            var sourceOriginal = parseResult.GetValue(sourceOriginalOption);
            var supportedOnly = parseResult.GetValue(supportedOnlyOption);
            var methods = parseResult.GetValue(methodsOption);
            var skipMissingMethods = parseResult.GetValue(skipMissingMethodsOption);
            var configPath = parseResult.GetValue(configOption)?.FullName
                             ?? GetDefaultConfigPath();

            return CreateCandidate(configPath, className, outputPath, sourceKind, sourceName, sourceOriginal, supportedOnly, methods, skipMissingMethods);
        });

        var mergeCandidateCommand = new Command("merge-candidate", "Merge a candidate whitelist into the formal whitelist")
        {
            candidatePathArgument,
            configOption,
        };

        mergeCandidateCommand.SetAction(parseResult =>
        {
            var candidatePath = parseResult.GetValue(candidatePathArgument)!.FullName;
            var configPath = parseResult.GetValue(configOption)?.FullName
                             ?? GetDefaultConfigPath();

            return MergeCandidate(configPath, candidatePath);
        });

        var rootCommand = new RootCommand("VtkSharp binding generator")
        {
            inspectClassCommand,
            inspectFunctionCommand,
            listModulesCommand,
            listClassesCommand,
            validateCommand,
            normalizeCommand,
            diffWhitelistCommand,
            createCandidateCommand,
            mergeCandidateCommand,
            generateCommand,
        };

        return rootCommand.Parse(args).Invoke();
    }

    private static string GetDefaultConfigPath()
        => Path.GetFullPath(Path.Combine("src", "generator", "config", "vtksharp.generator.yml"));

    private static int InspectFunction(string configPath, string className, string functionName, string format)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        if (workspace.IncludeDirectory is null)
        {
            Console.Error.WriteLine("VTK include directory was not found. Set VTK_ROOT or vtk.includeDirectory in local config.");
            return 1;
        }

        var hierarchyResolver = workspace.LoadHierarchyResolver();
        var header = hierarchyResolver.GetHeader(className);
        var inspected = new VtkClassInspector().InspectHeader(workspace.IncludeDirectory, header, className);

        var matches = inspected.Functions
            .Where(f => f.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            Console.Error.WriteLine($"No public method '{functionName}' found on class '{className}'.");
            return 1;
        }

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(JsonSerializer.Serialize(new { className, matches }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"{className}::{functionName} ({matches.Count} overload(s))");
        Console.WriteLine($"  Header: {header}");
        Console.WriteLine();
        foreach (var match in matches)
        {
            Console.WriteLine($"  {functionName}");
            Console.WriteLine($"    Cpp:       {match.CppSignature}");
            Console.WriteLine($"    Canonical: {match.CanonicalSignature}");
            Console.WriteLine($"    Supported: {match.IsSupported}");
            Console.WriteLine($"    Dependencies: {string.Join(", ", match.DependencyTypes ?? [])}");
            Console.WriteLine();
            Console.WriteLine("  # whitelist entry:");
            Console.WriteLine($"  - name: {match.Name}");
            Console.WriteLine($"    cppSignature: \"{match.CppSignature}\"");
            Console.WriteLine($"    return:");
            Console.WriteLine($"      type: {match.ReturnType}");
            Console.WriteLine($"    parameters:");
            foreach (var parameter in match.Parameters)
            {
                Console.WriteLine($"      - type: {parameter.Type}");
                Console.WriteLine($"        name: {parameter.Name}");
            }
            Console.WriteLine();
        }

        return 0;
    }

    private static int DiffWhitelist(string configPath, string candidatePath, string format)
        => new CandidateWhitelistService().Diff(configPath, candidatePath, format, Console.Out);

    private static int CreateCandidate(string configPath, string className, string outputPath, string sourceKind, string? sourceName, string? sourceOriginal, bool supportedOnly = false, IReadOnlyList<string>? methods = null, bool skipMissingMethods = false)
        => new CandidateWhitelistService().Create(configPath, className, outputPath, sourceKind, sourceName, sourceOriginal, supportedOnly, methods, skipMissingMethods, Console.Out, Console.Error);

    private static int MergeCandidate(string configPath, string candidatePath)
        => new CandidateWhitelistService().Merge(configPath, candidatePath, Console.Out);

    private static int ListModules(string configPath)
    {
        var hierarchyEntries = GeneratorWorkspace.Load(configPath).LoadHierarchyEntries();
        if (hierarchyEntries.Count == 0)
        {
            Console.Error.WriteLine("VTK hierarchy directory was not found. Set VTK_ROOT or vtk.hierarchyDirectory in local config.");
            return 1;
        }

        var modules = hierarchyEntries.Values
            .Select(entry => entry.Module)
            .Where(entry => !string.IsNullOrWhiteSpace(entry))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        foreach (var module in modules)
            Console.WriteLine(module);

        return 0;
    }

    private static int ListClasses(string configPath, string? moduleFilter, string format)
    {
        var hierarchyEntries = GeneratorWorkspace.Load(configPath).LoadHierarchyEntries();
        if (hierarchyEntries.Count == 0)
        {
            Console.Error.WriteLine("VTK hierarchy directory was not found. Set VTK_ROOT or vtk.hierarchyDirectory in local config.");
            return 1;
        }

        var query = hierarchyEntries.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(moduleFilter))
            query = query.Where(entry => entry.Module.Equals(moduleFilter, StringComparison.OrdinalIgnoreCase));

        var groups = query
            .GroupBy(entry => entry.Module, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal);

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var result = groups.Select(group => new
            {
                module = group.Key,
                classes = group.Select(entry => entry.ClassName).Order(StringComparer.Ordinal).ToList(),
            }).ToList();
            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        foreach (var group in groups)
        {
            Console.WriteLine(group.Key);
            foreach (var entry in group.OrderBy(entry => entry.ClassName, StringComparer.Ordinal))
                Console.WriteLine($"  {entry.ClassName}");
            Console.WriteLine();
        }

        return 0;
    }

    private static int InspectClass(string configPath, string className, string format)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        if (workspace.IncludeDirectory is null)
        {
            Console.Error.WriteLine("VTK include directory was not found. Set VTK_ROOT or vtk.includeDirectory in local config.");
            return 1;
        }

        var hierarchyResolver = workspace.LoadHierarchyResolver();
        var header = hierarchyResolver.GetHeader(className);
        var inspected = new VtkClassInspector().InspectHeader(workspace.IncludeDirectory, header, className) with
        {
            BaseClassName = hierarchyResolver.GetBaseClassName(className),
            Module = hierarchyResolver.GetModule(className),
            Header = header,
        };

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(JsonSerializer.Serialize(inspected, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
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

    private static int Generate(string configPath, string outputRoot, bool continueOnError = false, bool incremental = false, bool force = false)
        => new BindingGenerationService().Generate(configPath, outputRoot, continueOnError, incremental, force, Console.Out, Console.Error);

    private static int NormalizeWhitelist(string configPath)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        var hierarchyEntries = workspace.LoadHierarchyEntries();
        if (hierarchyEntries.Count == 0)
        {
            Console.Error.WriteLine("VTK hierarchy directory was not found or contains no hierarchy entries. Set VTK_ROOT or vtk.hierarchyDirectory in local config.");
            return 1;
        }

        var documents = workspace.LoadWhitelist();
        var normalized = new WhitelistNormalizer().Normalize(documents, hierarchyEntries, workspace.Config.Binding.ManualBindingClasses);
        new WhitelistWriter().WriteDirectory(workspace.WhitelistDirectory, normalized);

        Console.WriteLine($"Whitelist files normalized under: {workspace.WhitelistDirectory}");
        Console.WriteLine("Review the changes with git diff before committing.");
        return 0;
    }

    private static int CheckGeneratedOutput(string configPath, string outputRoot)
        => new BindingGenerationService().CheckGeneratedOutput(configPath, outputRoot, Console.Out, Console.Error);

    private static int ValidateWhitelist(string configPath, bool continueOnError, string format)
    {
        var context = new GeneratorRunContextFactory().Create(configPath, Console.Error);
        if (context is null)
            return 1;
        return new WhitelistValidationService().Validate(context, continueOnError, format, Console.Out, Console.Error);
    }
}
