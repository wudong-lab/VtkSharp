using VtkSharp.Generator.Core.Exporting;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Vtk;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class ExportInventoryServiceTests
{
    [TestMethod]
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

        Assert.Contains(function => function.FunctionName == "GetBounds",
            inventory.AlreadyExported.Single(group => group.DeclaringTypeName == "vtkProp3D").Functions);
        Assert.DoesNotContain(function => function.Id == propBoundsId,
            inventory.AvailableToAdd.SelectMany(group => group.Functions));
    }

    [TestMethod]
    public void BuildTypeInventory_GroupsFunctionsFromSelectedTypeToBaseTypes()
    {
        var chain = new[] { "vtkActor", "vtkProp3D", "vtkObject" };

        var inventory = ExportInventoryService.BuildTypeInventoryForTests(
            "vtkActor",
            chain,
            CreateInspectedClasses(),
            CreateHierarchyEntries(),
            exportedIds: []);

        Assert.AreSequenceEqual(["vtkActor", "vtkProp3D"], inventory.AvailableToAdd.Select(group => group.DeclaringTypeName));
    }

    [TestMethod]
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

        Assert.DoesNotContain(function => function.DeclaringTypeName == "vtkObject",
            inventory.AvailableToAdd.SelectMany(group => group.Functions));
        Assert.Contains(function => function.Reason == "'vtkObject' is a manual binding class.",
            inventory.Unsupported.Single(group => group.DeclaringTypeName == "vtkObject").Functions);
    }

    [TestMethod]
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

        Assert.IsEmpty(inventory.AvailableToAdd);
        Assert.Contains(function => function.Reason == "Parameter 'data' (double*) requires direction and length metadata.",
            inventory.Unsupported.Single().Functions);
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
