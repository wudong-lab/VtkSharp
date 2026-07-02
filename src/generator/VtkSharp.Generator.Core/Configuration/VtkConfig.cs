namespace VtkSharp.Generator.Core.Configuration;

public sealed record VtkConfig
{
    public string Version { get; init; } = "";
    public string ModulePrefix { get; init; } = "";
    public string? RootDirectory { get; init; }
    public string? IncludeDirectory { get; init; }
    public string? HierarchyDirectory { get; init; }
    public List<string> RuntimeModules { get; init; } = [];
}
