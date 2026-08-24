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

    [Fact]
    public void ReadFile_MapsConcreteAosArrayToManagedDataArrayBase()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "vtkDoubleArray : vtkAOSDataArrayTemplate<double> ; vtkDoubleArray.h ; vtkCommonCore");
        var reader = new VtkHierarchyReader();

        var entry = Assert.Single(reader.ReadFile(path));

        Assert.Equal("vtkDoubleArray", entry.ClassName);
        Assert.Equal("vtkDataArray", entry.BaseClassName);
    }
}
