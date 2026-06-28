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

        var generateCommand = new Command("generate-bindings", "Generate C# and C++ bindings from the whitelist")
        {
            configOption,
            outputRootOption,
            checkOption,
            continueOnErrorOption,
        };

        generateCommand.SetAction(parseResult =>
        {
            var check = parseResult.GetValue(checkOption);
            var outputRoot = parseResult.GetValue(outputRootOption);
            var continueOnError = parseResult.GetValue(continueOnErrorOption);
            var configPath = parseResult.GetValue(configOption)?.FullName
                             ?? GetDefaultConfigPath();
            var outputRootPath = outputRoot?.FullName
                                 ?? (check
                                     ? Path.Combine(Path.GetTempPath(), "VtkSharp.Generator", "check", Guid.NewGuid().ToString("N"))
                                     : throw new InvalidOperationException("--output-root is required unless --check is specified."));

            var exitCode = Generate(configPath, outputRootPath, continueOnError);
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
            Description = "Only include functions whose types are all supported (filters out unsigned long, basic_ostream, int&, etc.)",
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
        => Path.GetFullPath(Path.Combine("generator", "config", "vtksharp.generator.yml"));

    private static int InspectFunction(string configPath, string className, string functionName, string format)
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
        var inspected = new VtkClassInspector().InspectHeader(includeDirectory, header, className);

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
    {
        var configDirectory = Path.GetDirectoryName(configPath)
                              ?? throw new InvalidOperationException($"Config path '{configPath}' does not have a directory.");

        var config = LoadConfig(configPath);
        var whitelistDirectory = Path.GetFullPath(Path.Combine(configDirectory, config.Paths.WhitelistDirectory));
        var formal = new WhitelistLoader().LoadDirectory(whitelistDirectory);
        var candidate = LoadCandidateFile(candidatePath);

        var formalFingerprints = BuildFingerprints(formal);
        var candidateFingerprints = BuildCandidateFingerprints(candidate);

        var added = candidateFingerprints.Keys.Except(formalFingerprints, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var unchanged = candidateFingerprints.Keys.Intersect(formalFingerprints, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var result = new
            {
                added = added.Select(FingerprintToJson),
                unchanged = unchanged.Select(FingerprintToJson),
            };
            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        Console.WriteLine($"Candidate: {candidatePath}");
        Console.WriteLine($"Formal:    {whitelistDirectory}");
        Console.WriteLine();
        Console.WriteLine($"Added ({added.Count}):");
        foreach (var entry in added)
            Console.WriteLine($"  + {entry}");
        Console.WriteLine();
        Console.WriteLine($"Already present ({unchanged.Count}):");
        foreach (var entry in unchanged)
            Console.WriteLine($"    {entry}");

        return 0;
    }

    private static int CreateCandidate(string configPath, string className, string outputPath, string sourceKind, string? sourceName, string? sourceOriginal, bool supportedOnly = false, IReadOnlyList<string>? methods = null, bool skipMissingMethods = false)
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

        // Determine module from hierarchy
        var entries = LoadHierarchyEntries(config);
        var hierarchyModule = entries.TryGetValue(className, out var entry) ? entry.Module : "";
        if (string.IsNullOrWhiteSpace(hierarchyModule))
        {
            Console.Error.WriteLine($"Class '{className}' was not found in the VTK hierarchy.");
            return 1;
        }

        var inspected = new VtkClassInspector().InspectHeader(includeDirectory, header, className);

        using var writer = new StringWriter();
        writer.WriteLine("# yaml-language-server: $schema=../schemas/vtksharp.whitelist-candidate.schema.json");
        writer.WriteLine();
        writer.WriteLine("status: proposed");
        writer.WriteLine("source:");
        writer.WriteLine($"  kind: {sourceKind}");
        if (!string.IsNullOrWhiteSpace(sourceName))
            writer.WriteLine($"  name: {sourceName}");
        if (!string.IsNullOrWhiteSpace(sourceOriginal))
            writer.WriteLine($"  original: \"{sourceOriginal}\"");
        writer.WriteLine();
        writer.WriteLine("requirements:");
        writer.WriteLine($"  - module: {hierarchyModule}");
        writer.WriteLine($"    class: {className}");
        writer.WriteLine($"    header: {header}");
        writer.WriteLine("    functions:");

        var functions = supportedOnly
            ? inspected.Functions.Where(IsAllTypesSupported).ToList()
            : inspected.Functions;

        if (methods is { Count: > 0 })
        {
            var methodSet = methods.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var matched = functions.Where(f => methodSet.Contains(f.Name)).ToList();
            var notFound = methodSet.Where(m => !functions.Any(f => f.Name.Equals(m, StringComparison.OrdinalIgnoreCase))).ToList();
            if (notFound.Count > 0)
            {
                if (skipMissingMethods)
                {
                    foreach (var name in notFound)
                        Console.Error.WriteLine($"Warning: method '{name}' not found on '{className}' — skipped.");
                }
                else
                {
                    Console.Error.WriteLine($"Method(s) not found on '{className}': {string.Join(", ", notFound)}");
                    return 1;
                }
            }
            functions = matched;
        }

        if (functions.Count == 0)
        {
            writer.WriteLine("      []");
        }
        else
        {
            foreach (var function in functions)
            {
                writer.WriteLine($"      - name: {function.Name}");
                writer.WriteLine($"        cppSignature: \"{EscapeYaml(function.CppSignature)}\"");
                writer.WriteLine($"        return:");
                writer.WriteLine($"          type: {function.ReturnType}");
                writer.WriteLine($"        parameters:");
                if (function.Parameters.Count == 0)
                {
                    writer.WriteLine("          []");
                }
                else
                {
                    foreach (var parameter in function.Parameters)
                        writer.WriteLine($"          - {{ type: \"{EscapeYaml(parameter.Type)}\", name: {parameter.Name} }}");
                }
            }
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        File.WriteAllText(outputPath, writer.ToString(), System.Text.Encoding.UTF8);
        Console.WriteLine($"Candidate written to: {Path.GetFullPath(outputPath)}");
        return 0;
    }

    private static int MergeCandidate(string configPath, string candidatePath)
    {
        var configDirectory = Path.GetDirectoryName(configPath)
                              ?? throw new InvalidOperationException($"Config path '{configPath}' does not have a directory.");

        var config = LoadConfig(configPath);
        var whitelistDirectory = Path.GetFullPath(Path.Combine(configDirectory, config.Paths.WhitelistDirectory));
        var formal = new WhitelistLoader().LoadDirectory(whitelistDirectory);
        var candidate = LoadCandidateFile(candidatePath);

        var documentsByModule = formal.ToDictionary(d => d.Module, StringComparer.Ordinal);
        var formalFingerprints = BuildFingerprints(formal);
        var addedCount = 0;
        var newClassCount = 0;

        foreach (var requirement in candidate.Requirements)
        {
            if (!documentsByModule.TryGetValue(requirement.Module, out var document))
            {
                document = new WhitelistDocument { Module = requirement.Module, Classes = [] };
                documentsByModule.Add(requirement.Module, document);
            }

            var whitelistClass = document.Classes.FirstOrDefault(c => c.Name == requirement.Class);
            if (whitelistClass is null)
            {
                whitelistClass = new WhitelistClass { Name = requirement.Class, Header = requirement.Header, Functions = [] };
                document.Classes.Add(whitelistClass);
                newClassCount++;
            }

            foreach (var function in requirement.Functions)
            {
                var fingerprint = MakeFingerprint(requirement.Module, requirement.Class, function);
                if (formalFingerprints.Contains(fingerprint))
                    continue;

                whitelistClass.Functions.Add(function);
                formalFingerprints.Add(fingerprint);
                addedCount++;
            }
        }

        if (addedCount == 0 && newClassCount == 0)
        {
            Console.WriteLine("No new entries to merge.");
            return 0;
        }

        new WhitelistWriter().WriteDirectory(whitelistDirectory, documentsByModule.Values.ToList());

        // Auto-normalize after merge.
        var hierarchyEntries = LoadHierarchyEntries(config);
        if (hierarchyEntries.Count > 0)
        {
            var normalized = new WhitelistNormalizer().Normalize(documentsByModule.Values.ToList(), hierarchyEntries, config.Binding.ManualBindingClasses);
            new WhitelistWriter().WriteDirectory(whitelistDirectory, normalized);
        }

        var parts = new List<string>();
        if (newClassCount > 0) parts.Add($"{newClassCount} class(es)");
        if (addedCount > 0) parts.Add($"{addedCount} function(s)");
        Console.WriteLine($"Merged {string.Join(", ", parts)} from candidate into formal whitelist.");
        Console.WriteLine("Review the changes with git diff before committing.");
        return 0;
    }

    private static CandidateDocument LoadCandidateFile(string path)
    {
        using var reader = File.OpenText(path);
        var deserializer = new YamlDotNet.Serialization.DeserializerBuilder()
            .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        return deserializer.Deserialize<CandidateDocument>(reader);
    }

    private static HashSet<string> BuildFingerprints(IReadOnlyList<WhitelistDocument> documents)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var document in documents)
            foreach (var whitelistClass in document.Classes)
                foreach (var function in whitelistClass.Functions)
                    set.Add(MakeFingerprint(document.Module, whitelistClass.Name, function));
        return set;
    }

    private static Dictionary<string, string> BuildCandidateFingerprints(CandidateDocument candidate)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var requirement in candidate.Requirements)
            foreach (var function in requirement.Functions)
            {
                var fingerprint = MakeFingerprint(requirement.Module, requirement.Class, function);
                dict[fingerprint] = requirement.Class;
            }
        return dict;
    }

    private static string MakeFingerprint(string module, string className, WhitelistFunction function)
    {
        var paramTypes = string.Join(",", function.Parameters.Select(p => p.Type));
        return $"{module}/{className}::{function.Name}({paramTypes})->{function.Return.Type}";
    }

    private static object FingerprintToJson(string fingerprint)
    {
        var parts = fingerprint.Split("::", 2);
        var path = parts[0];
        var signature = parts.Length > 1 ? parts[1] : "";
        return new { path, signature };
    }

    private static bool IsAllTypesSupported(InspectedFunction function)
    {
        var types = function.Parameters.Select(p => p.Type).Append(function.ReturnType);
        return types.All(WhitelistValidator.IsSupportedType);
    }

    private static string EscapeYaml(string text)
    {
        if (text.Contains('"'))
            return text.Replace("\"", "\\\"");
        return text;
    }

    private static int ListModules(string configPath)
    {
        var hierarchyEntries = LoadHierarchyEntries(LoadConfig(configPath));
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
        var hierarchyEntries = LoadHierarchyEntries(LoadConfig(configPath));
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

    private static int Generate(string configPath, string outputRoot, bool continueOnError = false)
    {
        var context = CreateGeneratorRunContext(configPath);
        if (context is null)
            return 1;

        var validationResult = RunValidation(context, continueOnError, "text");
        if (validationResult != 0 && !continueOnError)
            return validationResult;

        Directory.CreateDirectory(outputRoot);

        var csharpEmitter = new CSharpBindingEmitter();
        var cppEmitter = new CppExportEmitter();
        var cmakeEmitter = new CMakeModulesEmitter();
        var nativeProjectEmitter = new NativeProjectEmitter();

        var manualClasses = context.Config.Binding.ManualBindingClasses.ToHashSet(StringComparer.Ordinal);
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

                    Console.Error.WriteLine($"Class '{whitelistClass.Name}' could not be inspected.");
                    return 1;
                }

                var baseClassName = context.HierarchyResolver.GetBaseClassName(whitelistClass.Name);
                var managedPath = Path.Combine(outputRoot, "bindings", "VtkSharp", document.Module, $"{whitelistClass.Name}_gen.cs");
                var nativePath = Path.Combine(outputRoot, "native", "src", document.Module, $"{whitelistClass.Name}_export_gen.cpp");
                var includeClassNames = GetIncludeClassNames(whitelistClass)
                    .Where(name => name != whitelistClass.Name)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                WriteText(managedPath, csharpEmitter.Emit(context.Config.Binding.Namespace, whitelistClass.Name, baseClassName, inspectedClass.HasStaticNew, whitelistClass.Functions));
                WriteText(nativePath, cppEmitter.Emit(whitelistClass.Name, includeClassNames, inspectedClass.HasStaticNew, whitelistClass.Functions));
            }
        }

        var modulesPath = Path.Combine(outputRoot, "native", "vtksharp.modules.generated.cmake");
        var vtkModules = context.Documents
            .Select(document => document.Module)
            .Concat(context.Config.Vtk.RuntimeModules)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        WriteText(modulesPath, cmakeEmitter.Emit(vtkModules));
        WriteText(Path.Combine(outputRoot, "native", "CMakeLists.txt"), nativeProjectEmitter.EmitCMakeLists(context.Config.Binding.NativeLibraryName));
        WriteText(Path.Combine(outputRoot, "native", "CMakePresets.json"), nativeProjectEmitter.EmitCMakePresets());
        WriteText(Path.Combine(outputRoot, "native", "include", "vtksharp_api.h"), nativeProjectEmitter.EmitApiHeader());

        if (continueOnError && skippedCount > 0)
            Console.WriteLine($"Skipped {skippedCount} class(es) due to inspection/validation failures.");

        Console.WriteLine($"Generated files will be written to: {Path.GetFullPath(outputRoot)}");
        return 0;
    }

    private static int RunValidation(GeneratorRunContext context, bool continueOnError, string format)
    {
        var diagnostics = new List<ValidationDiagnostic>(context.InspectionDiagnostics);

        var validator = new WhitelistValidator();
        foreach (var document in context.Documents)
            diagnostics.AddRange(validator.Validate(document, context.InspectedClasses, context.HierarchyResolver).Diagnostics);

        if (diagnostics.Count == 0)
        {
            if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
                Console.WriteLine(JsonSerializer.Serialize(new { ok = true, errors = Array.Empty<string>() }, new JsonSerializerOptions { WriteIndented = true }));
            else
                Console.WriteLine("Whitelist validation succeeded.");
            return 0;
        }

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(JsonSerializer.Serialize(new { ok = false, errors = diagnostics.Select(d => d.Message) }, new JsonSerializerOptions { WriteIndented = true }));
        }
        else
        {
            foreach (var diagnostic in diagnostics)
                Console.Error.WriteLine(diagnostic.Message);
        }

        return continueOnError ? 0 : 1;
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
        var currentNativeProjectFile = Path.GetFullPath(Path.Combine(configDirectory, config.Paths.NativeProjectFile));
        var currentModulesFile = Path.GetFullPath(Path.Combine(configDirectory, config.Paths.NativeModulesFile));
        var generatedManagedDirectory = Path.Combine(outputRoot, "bindings", "VtkSharp");
        var generatedNativeDirectory = Path.Combine(outputRoot, "native", "src");
        var generatedNativeProjectFile = Path.Combine(outputRoot, "native", "CMakeLists.txt");
        var generatedModulesFile = Path.Combine(outputRoot, "native", "vtksharp.modules.generated.cmake");

        var comparer = new GeneratedOutputComparer();
        var differences = comparer.CompareDirectories(currentManagedDirectory, generatedManagedDirectory, "*_gen.cs")
            .Concat(comparer.CompareDirectories(currentNativeDirectory, generatedNativeDirectory, "*_export_gen.cpp"))
            .Concat(comparer.CompareFiles(currentNativeProjectFile, generatedNativeProjectFile, "native/CMakeLists.txt"))
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
        => GeneratedFileWriter.WriteIfChanged(path, content);

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

        if (!normalized.StartsWith("vtk", StringComparison.Ordinal))
            return null;
        if (normalized is "vtkTypeBool" or "vtkIdType" or "vtkMTimeType")
            return null;
        if (TypeClassifier.IsVtkValueStruct(normalized))
            return null;
        return normalized;
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

    private static int ValidateWhitelist(string configPath, bool continueOnError, string format)
    {
        var context = CreateGeneratorRunContext(configPath);
        if (context is null)
            return 1;
        return RunValidation(context, continueOnError, format);
    }

    private static GeneratorRunContext? CreateGeneratorRunContext(string configPath)
    {
        var configDirectory = Path.GetDirectoryName(configPath)
                              ?? throw new InvalidOperationException($"Config path '{configPath}' does not have a directory.");

        var config = LoadConfig(configPath);
        var whitelistDirectory = Path.GetFullPath(Path.Combine(configDirectory, config.Paths.WhitelistDirectory));
        var includeDirectory = ResolveIncludeDirectory(config);
        if (includeDirectory is null)
        {
            Console.Error.WriteLine("VTK include directory was not found. Set VTK_ROOT or vtk.includeDirectory in local config.");
            return null;
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

        return new GeneratorRunContext(
            config,
            documents,
            LoadHierarchyResolver(config),
            inspectedClasses,
            diagnostics);
    }

    private sealed record GeneratorRunContext(
        GeneratorConfig Config,
        IReadOnlyList<WhitelistDocument> Documents,
        VtkHierarchyResolver HierarchyResolver,
        IReadOnlyDictionary<string, InspectedClass> InspectedClasses,
        IReadOnlyList<ValidationDiagnostic> InspectionDiagnostics);
}
