using System;

namespace VtkSharp.ExampleBrowser;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ExampleAttribute : Attribute
{
    public string Name { get; }
    public string Category { get; }

    public string? Description { get; init; }
    public string[]? SourceFiles { get; init; }

    public ExampleAttribute(string name, string category)
    {
        this.Name = name;
        this.Category = category;
    }
}
