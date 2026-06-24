using VtkSharp.Generator.Core.Vtk;

namespace VtkSharp.Generator.Tests;

public sealed class VtkHierarchyResolverTests
{
    [Fact]
    public void GetBaseClassName_ReturnsHierarchyBaseClass()
    {
        var resolver = new VtkHierarchyResolver(new Dictionary<string, VtkHierarchyEntry>(StringComparer.Ordinal)
        {
            ["vtkActor"] = new("vtkActor", "vtkProp3D", "vtkActor.h", "vtkRenderingCore"),
        });

        Assert.Equal("vtkProp3D", resolver.GetBaseClassName("vtkActor"));
    }

    [Fact]
    public void GetBaseClassName_FallsBackToVtkObject()
    {
        var resolver = new VtkHierarchyResolver(new Dictionary<string, VtkHierarchyEntry>(StringComparer.Ordinal));

        Assert.Equal("vtkObject", resolver.GetBaseClassName("vtkMissing"));
    }
}
