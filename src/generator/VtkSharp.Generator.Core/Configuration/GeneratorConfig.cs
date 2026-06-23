namespace VtkSharp.Generator.Core.Configuration;

public sealed record GeneratorConfig
{
    public VtkConfig Vtk { get; init; } = new();
    public BindingConfig Binding { get; init; } = new();
    public PathConfig Paths { get; init; } = new();
    public GenerationConfig Generation { get; init; } = new();
}

public sealed record VtkConfig
{
    public string Version { get; init; } = "";
    public string ModulePrefix { get; init; } = "";
    public string? RootDirectory { get; init; }
    public string? IncludeDirectory { get; init; }
    public string? HierarchyDirectory { get; init; }
}

public sealed record BindingConfig
{
    public string Namespace { get; init; } = "";
    public string NativeLibraryName { get; init; } = "";
    public List<string> ManualBindingClasses { get; init; } = [];
}

public sealed record PathConfig
{
    public string WhitelistDirectory { get; init; } = "";
    public string ManagedOutputDirectory { get; init; } = "";
    public string NativeOutputDirectory { get; init; } = "";
    public string NativeModulesFile { get; init; } = "";
}

public sealed record GenerationConfig
{
    public bool CreateManualExtensionFiles { get; init; }
    public bool OverwriteGeneratedFiles { get; init; }
    public bool DeleteOrphanGeneratedFiles { get; init; }
}
