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
    public void InspectHeader_DoesNotIncludeInheritedBaseClassFunctions()
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
        Assert.DoesNotContain(inspected.Functions, function => function.Name == "Update");
        Assert.Equal("vtkBase", inspected.BaseClassName);
    }

    [Fact]
    public void InspectHeader_OnlyReportsDirectPublicOverloads()
    {
        var directory = CreateHeader("""
            class vtkDataObject {};
            class vtkPolyData {};
            class vtkBase
            {
            public:
                void AddInputData(vtkDataObject*);
                void AddInputData(int, vtkDataObject*);
                void Update();
            };
            """, "vtkBase.h");
        File.WriteAllText(Path.Combine(directory, "vtkDerived.h"), """
            #include "vtkBase.h"
            class vtkDerived : public vtkBase
            {
            public:
                void AddInputData(vtkPolyData*);
            };
            """);
        var inspector = new VtkClassInspector();

        var inspected = inspector.InspectHeader(directory, "vtkDerived.h", "vtkDerived");

        Assert.Contains(inspected.Functions, function =>
            function.Name == "AddInputData" &&
            function.Parameters.SequenceEqual([new InspectedParameter("vtkPolyData*", "_arg1")]));
        Assert.DoesNotContain(inspected.Functions, function =>
            function.Name == "AddInputData" &&
            function.Parameters.Any(parameter => parameter.Type == "vtkDataObject*"));
        Assert.DoesNotContain(inspected.Functions, function => function.Name == "Update");
    }

    [Fact]
    public void InspectHeader_PrivateDeclarationIsNotReported()
    {
        var directory = CreateHeader("""
            class vtkDataObject {};
            class vtkBase
            {
            public:
                void AddInputData(vtkDataObject*);
                void Update();
            };
            """, "vtkBase.h");
        File.WriteAllText(Path.Combine(directory, "vtkDerived.h"), """
            #include "vtkBase.h"
            class vtkDerived : public vtkBase
            {
            public:
                void Render();
            private:
                void AddInputData(vtkDataObject*);
            };
            """);
        var inspector = new VtkClassInspector();

        var inspected = inspector.InspectHeader(directory, "vtkDerived.h", "vtkDerived");

        Assert.DoesNotContain(inspected.Functions, function => function.Name == "AddInputData");
        Assert.Contains(inspected.Functions, function => function.Name == "Render");
        Assert.DoesNotContain(inspected.Functions, function => function.Name == "Update");
    }

    [Fact]
    public void InspectHeader_OnlyTreatsStaticNewAsSupportedStaticFunction()
    {
        var directory = CreateHeader("""
            class vtkThing
            {
            public:
                static vtkThing* New();
                static void SetGlobalFlag(int value);
                void Update();
            };
            """);
        var inspector = new VtkClassInspector();

        var inspected = inspector.InspectHeader(directory, "vtkThing.h", "vtkThing");

        Assert.True(inspected.HasStaticNew);
        Assert.Contains(inspected.Functions, function => function.Name == "Update");
        Assert.DoesNotContain(inspected.Functions, function => function.Name == "New");
        Assert.DoesNotContain(inspected.Functions, function => function.Name == "SetGlobalFlag");
    }

    [Fact]
    public void InspectHeader_DoesNotReportConstructorsOrDestructors()
    {
        var directory = CreateHeader("""
            class vtkThing
            {
            public:
                vtkThing();
                ~vtkThing();
                void Update();
            };
            """);
        var inspector = new VtkClassInspector();

        var inspected = inspector.InspectHeader(directory, "vtkThing.h", "vtkThing");

        Assert.Contains(inspected.Functions, function => function.Name == "Update");
        Assert.DoesNotContain(inspected.Functions, function => function.Name == "vtkThing");
        Assert.DoesNotContain(inspected.Functions, function => function.Name == "~vtkThing");
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
