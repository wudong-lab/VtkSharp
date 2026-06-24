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

    [Fact]
    public void InspectHeader_IncludesBaseClassFunctions()
    {
        var directory = CreateHeader("""
            class vtkBase
            {
            public:
                void Update();
            };
            """, "vtkBase.h");
        File.WriteAllText(Path.Combine(directory, "vtkDerived.h"), """
            #include "vtkBase.h"
            class vtkDerived : public vtkBase
            {
            public:
                void Render();
            };
            """);
        var inspector = new VtkClassInspector();

        var inspected = inspector.InspectHeader(directory, "vtkDerived.h", "vtkDerived");

        Assert.Contains(inspected.Functions, function => function.Name == "Render");
        Assert.Contains(inspected.Functions, function => function.Name == "Update");
    }

    [Fact]
    public void InspectHeader_ReportsCanonicalSignaturesAndDependencyTypes()
    {
        var directory = CreateHeader("""
            class vtkMapper;
            class vtkProperty;
            class vtkActor
            {
            public:
                void SetMapper(vtkMapper * mapper);
                vtkProperty * GetProperty();
            };
            """, "vtkActor.h");
        var inspector = new VtkClassInspector();

        var inspected = inspector.InspectHeader(directory, "vtkActor.h", "vtkActor");

        Assert.Equal(["vtkMapper", "vtkProperty"], inspected.Dependencies);
        Assert.Contains(inspected.Functions, function =>
            function.Name == "SetMapper" &&
            function.CanonicalSignature == "void SetMapper(vtkMapper* mapper)" &&
            function.DependencyTypes!.SequenceEqual(["vtkMapper"]));
        Assert.Contains(inspected.Functions, function =>
            function.Name == "GetProperty" &&
            function.CanonicalSignature == "vtkProperty* GetProperty()" &&
            function.DependencyTypes!.SequenceEqual(["vtkProperty"]));
    }

    private static string CreateHeader(string text, string fileName = "vtkThing.h")
    {
        var directory = Path.Combine(Path.GetTempPath(), "VtkSharp.Generator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, fileName), text);
        return directory;
    }
}
