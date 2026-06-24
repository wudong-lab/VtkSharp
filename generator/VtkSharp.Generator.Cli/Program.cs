using System.CommandLine;
using System.Text.Json;
using VtkSharp.Generator.Core.Configuration;
using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;
using VtkSharp.Generator.Core.Whitelist;

var classArgument = new Argument<string>("class-name");
var formatOption = new Option<string>("--format")
{
    Description = "Output format: text or json",
    DefaultValueFactory = _ => "text",
};

var inspectClassCommand = new Command("inspect-class", "Inspect a VTK class")
{
    classArgument,
    formatOption,
};

inspectClassCommand.SetAction(parseResult =>
{
    var className = parseResult.GetValue(classArgument)!;
    var format = parseResult.GetValue(formatOption)!;
    var inspector = new VtkClassInspector();
    var inspected = inspector.InspectSynthetic(className,
    [
        ("SetMapper", "void", [("vtkMapper *", "mapper")]),
    ]);

    if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine(JsonSerializer.Serialize(inspected, new JsonSerializerOptions { WriteIndented = true }));
        return;
    }

    Console.WriteLine(inspected.Name);
    foreach (var function in inspected.Functions)
        Console.WriteLine($"  {function.CppSignature}");
});

var configOption = new Option<FileInfo>("--config")
{
    Description = "Generator config file",
};

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

var outputRootOption = new Option<DirectoryInfo>("--output-root")
{
    Description = "Temporary output root",
};

var generateCommand = new Command("generate", "Generate bindings")
{
    configOption,
    outputRootOption,
};

generateCommand.SetAction(parseResult =>
{
    var outputRoot = parseResult.GetValue(outputRootOption)
        ?? throw new InvalidOperationException("--output-root is required for the first MVP.");

    var configPath = parseResult.GetValue(configOption)?.FullName
        ?? GetDefaultConfigPath();

    Generate(configPath, outputRoot.FullName);
});

var rootCommand = new RootCommand("VtkSharp binding generator")
{
    inspectClassCommand,
    validateCommand,
    generateCommand,
};

return rootCommand.Parse(args).Invoke();

static string GetDefaultConfigPath()
    => Path.GetFullPath(Path.Combine("generator", "config", "vtksharp.generator.yml"));

static void Generate(string configPath, string outputRoot)
{
    var configDirectory = Path.GetDirectoryName(configPath)
        ?? throw new InvalidOperationException($"Config path '{configPath}' does not have a directory.");

    var localConfigPath = Path.Combine(configDirectory, "vtksharp.generator.local.yml");
    var config = new GeneratorConfigLoader().Load(configPath, localConfigPath);
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

    foreach (var document in documents)
    {
        foreach (var whitelistClass in document.Classes)
        {
            if (manualClasses.Contains(whitelistClass.Name))
                continue;

            var baseClassName = ResolveMvpBaseClass(whitelistClass.Name);
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
}

static void WriteText(string path, string content)
{
    var directory = Path.GetDirectoryName(path)
        ?? throw new InvalidOperationException($"Output path '{path}' does not have a directory.");

    Directory.CreateDirectory(directory);
    File.WriteAllText(path, content);
}

static string ResolveMvpBaseClass(string className)
    => className switch
    {
        "vtkAlgorithm" => "vtkObject",
        "vtkAlgorithmOutput" => "vtkObject",
        "vtkPolyDataAlgorithm" => "vtkAlgorithm",
        "vtkConeSource" => "vtkPolyDataAlgorithm",
        "vtkWindow" => "vtkObject",
        "vtkProp" => "vtkObject",
        "vtkProp3D" => "vtkProp",
        "vtkActor" => "vtkProp3D",
        "vtkMapper" => "vtkObject",
        "vtkPolyDataMapper" => "vtkMapper",
        "vtkRenderer" => "vtkObject",
        "vtkRenderWindow" => "vtkWindow",
        "vtkRenderWindowInteractor" => "vtkObject",
        _ => "vtkObject",
    };

static IEnumerable<string> GetIncludeClassNames(WhitelistClass whitelistClass)
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

static string? ExtractVtkClassName(string typeName)
{
    var normalized = typeName.Replace("const", "", StringComparison.Ordinal)
        .Replace("*", "", StringComparison.Ordinal)
        .Trim();

    return normalized.StartsWith("vtk", StringComparison.Ordinal) ? normalized : null;
}

static string? ResolveIncludeDirectory(GeneratorConfig config)
{
    var candidates = new[]
    {
        config.Vtk.IncludeDirectory,
        config.Vtk.RootDirectory is null ? null : Path.Combine(config.Vtk.RootDirectory, "include", $"vtk-{config.Vtk.Version}"),
        config.Vtk.RootDirectory is null ? null : Path.Combine(config.Vtk.RootDirectory, "include"),
    };

    return candidates.FirstOrDefault(path => path is not null && Directory.Exists(path));
}

static bool TryInspectHasStaticNew(VtkClassInspector inspector, string includeDirectory, WhitelistClass whitelistClass)
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

static int ValidateWhitelist(string configPath)
{
    var configDirectory = Path.GetDirectoryName(configPath)
        ?? throw new InvalidOperationException($"Config path '{configPath}' does not have a directory.");

    var localConfigPath = Path.Combine(configDirectory, "vtksharp.generator.local.yml");
    var config = new GeneratorConfigLoader().Load(configPath, localConfigPath);
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
