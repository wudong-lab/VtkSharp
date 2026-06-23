using VtkSharp.Generator.Core.Inspection;

namespace VtkSharp.Generator.Tests;

public sealed class VtkClassInspectorTests
{
    [Fact]
    public void InspectHeader_DetectsStaticNew()
    {
        var directory = CreateHeader("""
            class vtkThing
            {
            public:
                static vtkThing* New();
                void Update();
            };
            """);
        var inspector = new VtkClassInspector();

        var inspected = inspector.InspectHeader(directory, "vtkThing.h", "vtkThing");

        Assert.True(inspected.HasStaticNew);
        Assert.Contains(inspected.Functions, function => function.Name == "Update");
    }

    [Fact]
    public void InspectHeader_DoesNotReportStaticNewWhenMissing()
    {
        var directory = CreateHeader("""
            class vtkBase
            {
            public:
                void Render();
            };
            """, "vtkBase.h");
        var inspector = new VtkClassInspector();

        var inspected = inspector.InspectHeader(directory, "vtkBase.h", "vtkBase");

        Assert.False(inspected.HasStaticNew);
    }

    private static string CreateHeader(string text, string fileName = "vtkThing.h")
    {
        var directory = Path.Combine(Path.GetTempPath(), "VtkSharp.Generator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, fileName), text);
        return directory;
    }
}
