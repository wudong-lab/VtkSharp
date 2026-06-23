using VtkSharp.Generator.Core.Vtk;

namespace VtkSharp.Generator.Tests;

public sealed class VtkHierarchyReaderTests
{
    [Fact]
    public void ReadFile_ParsesClassLine()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "hierarchy", "vtkRenderingCore-hierarchy.txt");
        var reader = new VtkHierarchyReader();

        var entries = reader.ReadFile(path);

        var actor = Assert.Single(entries, entry => entry.ClassName == "vtkActor");
        Assert.Equal("vtkProp3D", actor.BaseClassName);
        Assert.Equal("vtkActor.h", actor.Header);
        Assert.Equal("vtkRenderingCore", actor.Module);
    }
}
