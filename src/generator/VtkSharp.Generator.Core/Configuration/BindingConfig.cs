namespace VtkSharp.Generator.Core.Configuration;

public sealed record BindingConfig
{
    public string Namespace { get; init; } = "";
    public string NativeLibraryName { get; init; } = "";
    public List<string> ManualBindingClasses { get; init; } = [];
}