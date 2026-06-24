namespace VtkSharp.Generator.Core.Configuration;

public sealed record GeneratorConfig
{
    public VtkConfig Vtk { get; init; } = new();
    public BindingConfig Binding { get; init; } = new();
    public PathConfig Paths { get; init; } = new();
    public GenerationConfig Generation { get; init; } = new();
}