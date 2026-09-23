using VtkSharp.Generator.Core.Vtk;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class VtkHierarchyResolverTests
{
    [TestMethod]
    public void GetBaseClassName_ReturnsHierarchyBaseClass()
    {
        var resolver = new VtkHierarchyResolver(new Dictionary<string, VtkHierarchyEntry>(StringComparer.Ordinal)
        {
            ["vtkActor"] = new("vtkActor", "vtkProp3D", "vtkActor.h", "vtkRenderingCore"),
        });

        Assert.AreEqual("vtkProp3D", resolver.GetBaseClassName("vtkActor"));
    }

    [TestMethod]
    public void GetBaseClassName_FallsBackToVtkObject()
    {
        var resolver = new VtkHierarchyResolver(new Dictionary<string, VtkHierarchyEntry>(StringComparer.Ordinal));

        Assert.AreEqual("vtkObject", resolver.GetBaseClassName("vtkMissing"));
    }
}
