using VtkSharp.Generator.Core.Vtk;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class VtkHierarchyReaderTests
{
    [TestMethod]
    public void ReadFile_ParsesClassLine()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "hierarchy", "vtkRenderingCore-hierarchy.txt");
        var reader = new VtkHierarchyReader();

        var entries = reader.ReadFile(path);

        var actor = Enumerable.Single(entries, entry => entry.ClassName == "vtkActor");
        Assert.AreEqual("vtkProp3D", actor.BaseClassName);
        Assert.AreEqual("vtkActor.h", actor.Header);
        Assert.AreEqual("vtkRenderingCore", actor.Module);
    }

    [TestMethod]
    public void ReadFile_MapsConcreteAosArrayToManagedDataArrayBase()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "vtkDoubleArray : vtkAOSDataArrayTemplate<double> ; vtkDoubleArray.h ; vtkCommonCore");
        var reader = new VtkHierarchyReader();

        var entry = Enumerable.Single(reader.ReadFile(path));

        Assert.AreEqual("vtkDoubleArray", entry.ClassName);
        Assert.AreEqual("vtkDataArray", entry.BaseClassName);
    }
}
