using System;

namespace VtkSharp.ExampleBrowser;

public class ExampleInfo
{
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Type ExampleType { get; init; } = null!;
    public string[] SourceFiles { get; init; } = [];
}
