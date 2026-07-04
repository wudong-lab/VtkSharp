using VtkSharp.Generator.Core.Exporting;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Vtk;

namespace VtkSharp.Generator.Tests;

public sealed class ExportInventoryServiceTests
{
    [Fact]
    public void BuildTypeInventory_ExcludesExportedBaseFunctionsFromAvailableList()
    {
        var chain = new[] { "vtkActor", "vtkProp3D", "vtkObject" };
        var propBoundsId = ExportInventoryService.CreateFunctionId("vtkProp3D", "double*", "GetBounds", []);

        var inventory = ExportInventoryService.BuildTypeInventoryForTests(
            "vtkActor",
            chain,
            CreateInspectedClasses(),
            CreateHierarchyEntries(),
            [propBoundsId]);

        Assert.Contains(inventory.AlreadyExported.Single(group => group.DeclaringTypeName == "vtkProp3D").Functions,
            function => function.FunctionName == "GetBounds");
        Assert.DoesNotContain(inventory.AvailableToAdd.SelectMany(group => group.Functions),
            function => function.Id == propBoundsId);
    }

    [Fact]
    public void BuildTypeInventory_GroupsFunctionsFromSelectedTypeToBaseTypes()
    {
        var chain = new[] { "vtkActor", "vtkProp3D", "vtkObject" };

        var inventory = ExportInventoryService.BuildTypeInventoryForTests(
            "vtkActor",
            chain,
            CreateInspectedClasses(),
            CreateHierarchyEntries(),
            exportedIds: []);

        Assert.Equal(["vtkActor", "vtkProp3D"], inventory.AvailableToAdd.Select(group => group.DeclaringTypeName));
    }

    [Fact]
    public void BuildTypeInventory_DoesNotAllowManualBindingClassFunctionsAsAvailable()
    {
        var chain = new[] { "vtkActor", "vtkObject" };

        var inventory = ExportInventoryService.BuildTypeInventoryForTests(
            "vtkActor",
            chain,
            CreateInspectedClasses(),
            CreateHierarchyEntries(),
            exportedIds: [],
            hiddenTypeNames: ["vtkObject"]);

        Assert.DoesNotContain(inventory.AvailableToAdd.SelectMany(group => group.Functions),
            function => function.DeclaringTypeName == "vtkObject");
        Assert.Contains(inventory.Unsupported.Single(group => group.DeclaringTypeName == "vtkObject").Functions,
            function => function.Reason == "'vtkObject' is a manual binding class.");
    }

    [Fact]
    public void BuildTypeInventory_DoesNotAllowPrimitivePointerParametersWithoutMetadata()
    {
        var chain = new[] { "vtkActor" };
        var inspectedClasses = new Dictionary<string, InspectedClass>(StringComparer.Ordinal)
        {
            ["vtkActor"] = new("vtkActor",
            [
                new InspectedFunction("GetPosition", "void GetPosition(double * data)", "void", [new InspectedParameter("double*", "data")], true),
            ]),
        };

        var inventory = ExportInventoryService.BuildTypeInventoryForTests(
            "vtkActor",
            chain,
            inspectedClasses,
            CreateHierarchyEntries(),
            exportedIds: []);

        Assert.Empty(inventory.AvailableToAdd);
        Assert.Contains(inventory.Unsupported.Single().Functions,
            function => function.Reason == "Parameter 'data' (double*) requires direction and length metadata.");
    }

    private static IReadOnlyDictionary<string, InspectedClass> CreateInspectedClasses()
        => new Dictionary<string, InspectedClass>(StringComparer.Ordinal)
        {
            ["vtkActor"] = new("vtkActor",
            [
                new InspectedFunction("SetMapper", "void SetMapper(vtkMapper * mapper)", "void", [new InspectedParameter("vtkMapper*", "mapper")], true),
            ]),
            ["vtkProp3D"] = new("vtkProp3D",
            [
                new InspectedFunction("GetBounds", "double * GetBounds()", "double*", [], true),
            ]),
            ["vtkObject"] = new("vtkObject",
            [
                new InspectedFunction("Modified", "void Modified()", "void", [], true),
            ]),
        };

    private static IReadOnlyDictionary<string, VtkHierarchyEntry> CreateHierarchyEntries()
        => new Dictionary<string, VtkHierarchyEntry>(StringComparer.Ordinal)
        {
            ["vtkActor"] = new("vtkActor", "vtkProp3D", "vtkActor.h", "vtkRenderingCore"),
            ["vtkProp3D"] = new("vtkProp3D", "vtkObject", "vtkProp3D.h", "vtkRenderingCore"),
            ["vtkObject"] = new("vtkObject", "vtkObjectBase", "vtkObject.h", "vtkCommonCore"),
        };
}
