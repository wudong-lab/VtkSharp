namespace VtkSharp.Generator.Core.Configuration;

public sealed record GenerationConfig
{
    public bool CreateManualExtensionFiles { get; init; }
    public bool OverwriteGeneratedFiles { get; init; }
    public bool DeleteOrphanGeneratedFiles { get; init; }
}