using VtkSharp.Generator.Core.Vtk;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Core.Configuration;

public sealed class GeneratorWorkspace
{
    private GeneratorWorkspace(
        string configPath,
        string configDirectory,
        GeneratorConfig config,
        string whitelistDirectory,
        string? includeDirectory,
        string? hierarchyDirectory)
    {
        this.ConfigPath = configPath;
        this.ConfigDirectory = configDirectory;
        this.Config = config;
        this.WhitelistDirectory = whitelistDirectory;
        this.IncludeDirectory = includeDirectory;
        this.HierarchyDirectory = hierarchyDirectory;
    }

    public string ConfigPath { get; }
    public string ConfigDirectory { get; }
    public GeneratorConfig Config { get; }
    public string WhitelistDirectory { get; }
    public string? IncludeDirectory { get; }
    public string? HierarchyDirectory { get; }

    public static GeneratorWorkspace Load(string configPath)
    {
        var fullConfigPath = Path.GetFullPath(configPath);
        var configDirectory = Path.GetDirectoryName(fullConfigPath)
                              ?? throw new InvalidOperationException($"Config path '{configPath}' does not have a directory.");
        var localConfigPath = Path.Combine(configDirectory, "vtksharp.generator.local.yml");
        var config = new GeneratorConfigLoader().Load(fullConfigPath, localConfigPath);
        var whitelistDirectory = Path.GetFullPath(Path.Combine(configDirectory, config.Paths.WhitelistDirectory));

        return new GeneratorWorkspace(
            fullConfigPath,
            configDirectory,
            config,
            whitelistDirectory,
            ResolveIncludeDirectory(config),
            ResolveHierarchyDirectory(config));
    }

    public IReadOnlyList<WhitelistDocument> LoadWhitelist()
        => new WhitelistLoader().LoadDirectory(this.WhitelistDirectory);

    public IReadOnlyDictionary<string, VtkHierarchyEntry> LoadHierarchyEntries()
        => this.HierarchyDirectory is null
            ? new Dictionary<string, VtkHierarchyEntry>(StringComparer.Ordinal)
            : new VtkHierarchyReader().ReadDirectory(this.HierarchyDirectory);

    public VtkHierarchyResolver LoadHierarchyResolver()
        => new(this.LoadHierarchyEntries());

    public string GetManagedOutputDirectory()
        => Path.GetFullPath(Path.Combine(this.ConfigDirectory, this.Config.Paths.ManagedOutputDirectory));

    public string GetNativeOutputDirectory()
        => Path.GetFullPath(Path.Combine(this.ConfigDirectory, this.Config.Paths.NativeOutputDirectory));

    public string GetNativeProjectFile()
        => Path.GetFullPath(Path.Combine(this.ConfigDirectory, this.Config.Paths.NativeProjectFile));

    public string GetNativeModulesFile()
        => Path.GetFullPath(Path.Combine(this.ConfigDirectory, this.Config.Paths.NativeModulesFile));

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
}
