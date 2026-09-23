using VtkSharp.Generator.Core.Inspection;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class VtkClassInspectorTests
{
    [TestMethod]
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

        Assert.IsTrue(inspected.HasStaticNew);
        Assert.Contains(function => function.Name == "Update", inspected.Functions);
    }

    [TestMethod]
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

        Assert.IsFalse(inspected.HasStaticNew);
    }

    [TestMethod]
    public void InspectHeader_ProvidesVectorForVtkHeadersThatUseItTransitively()
    {
        var directory = CreateHeader("""
            class vtkThing
            {
            public:
                void SetValues(std::vector<int> values);
            };
            """);
        var inspector = new VtkClassInspector();

        var inspected = inspector.InspectHeader(directory, "vtkThing.h", "vtkThing");

        Assert.Contains(function => function.Name == "SetValues", inspected.Functions);
    }

    [TestMethod]
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

        Assert.Contains(function => function.Name == "Render", inspected.Functions);
        Assert.DoesNotContain(function => function.Name == "Update", inspected.Functions);
        Assert.AreEqual("vtkBase", inspected.BaseClassName);
    }

    [TestMethod]
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

        Assert.Contains(function =>
            function.Name == "AddInputData" &&
            function.Parameters.SequenceEqual([new InspectedParameter("vtkPolyData*", "_arg1")]), inspected.Functions);
        Assert.DoesNotContain(function =>
            function.Name == "AddInputData" &&
            function.Parameters.Any(parameter => parameter.Type == "vtkDataObject*"), inspected.Functions);
        Assert.DoesNotContain(function => function.Name == "Update", inspected.Functions);
    }

    [TestMethod]
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

        Assert.DoesNotContain(function => function.Name == "AddInputData", inspected.Functions);
        Assert.Contains("AddInputData", inspected.DeclaredMemberNames!);
        Assert.Contains(function => function.Name == "Render", inspected.Functions);
        Assert.DoesNotContain(function => function.Name == "Update", inspected.Functions);
    }

    [TestMethod]
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

        Assert.IsTrue(inspected.HasStaticNew);
        Assert.Contains(function => function.Name == "Update", inspected.Functions);
        Assert.DoesNotContain(function => function.Name == "New", inspected.Functions);
        Assert.DoesNotContain(function => function.Name == "SetGlobalFlag", inspected.Functions);
        Assert.Contains("SetGlobalFlag", inspected.DeclaredMemberNames!);
    }

    [TestMethod]
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

        Assert.Contains(function => function.Name == "Update", inspected.Functions);
        Assert.DoesNotContain(function => function.Name == "vtkThing", inspected.Functions);
        Assert.DoesNotContain(function => function.Name == "~vtkThing", inspected.Functions);
    }

    [TestMethod]
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

        Assert.AreSequenceEqual(["vtkMapper", "vtkProperty"], inspected.Dependencies);
        Assert.Contains(function =>
            function.Name == "SetMapper" &&
            function.CanonicalSignature == "void SetMapper(vtkMapper* mapper)" &&
            function.DependencyTypes!.SequenceEqual(["vtkMapper"]), inspected.Functions);
        Assert.Contains(function =>
            function.Name == "GetProperty" &&
            function.CanonicalSignature == "vtkProperty* GetProperty()" &&
            function.DependencyTypes!.SequenceEqual(["vtkProperty"]), inspected.Functions);
    }

    [TestMethod]
    public void InspectHeader_ReportsTypeSupportUsingBindingTypeMapper()
    {
        var directory = CreateHeader("""
            class vtkStdString {};
            class vtkColor3ub {};
            class vtkColor4ub {};
            class vtkThing
            {
            public:
                void SetName(vtkStdString const& name);
                vtkStdString GetName();
                vtkColor3ub GetColor();
                vtkColor4ub GetUnsupportedColor();
                void SetValue(int value);
            };
            """);
        var inspector = new VtkClassInspector();

        var inspected = inspector.InspectHeader(directory, "vtkThing.h", "vtkThing");

        Assert.IsTrue(Enumerable.Single(inspected.Functions, function => function.Name == "SetName").IsSupported);
        Assert.IsTrue(Enumerable.Single(inspected.Functions, function => function.Name == "SetValue").IsSupported);
        Assert.IsTrue(Enumerable.Single(inspected.Functions, function => function.Name == "GetColor").IsSupported);
        Assert.IsTrue(Enumerable.Single(inspected.Functions, function => function.Name == "GetName").IsSupported);
        Assert.IsFalse(Enumerable.Single(inspected.Functions, function => function.Name == "GetUnsupportedColor").IsSupported);
    }

    [TestMethod]
    public void InspectHeader_ReturnsFinalClassAfterInspectFileCachedRawClass()
    {
        var directory = CreateHeader("""
            class vtkBase {};
            class vtkMapper;
            class vtkActor : public vtkBase
            {
            public:
                void SetMapper(vtkMapper * mapper);
            };
            """, "vtkActor.h");
        var inspector = new VtkClassInspector();

        inspector.InspectFile(directory, "vtkActor.h");
        var inspected = inspector.InspectHeader(directory, "vtkActor.h", "vtkActor");

        Assert.AreEqual("vtkBase", inspected.BaseClassName);
        Assert.AreSequenceEqual(["vtkMapper"], inspected.Dependencies);
        Assert.IsNull(inspected.BaseClassNames);
    }

    private static string CreateHeader(string text, string fileName = "vtkThing.h")
    {
        var directory = Path.Combine(Path.GetTempPath(), "VtkSharp.Generator.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, fileName), text);
        return directory;
    }
}
