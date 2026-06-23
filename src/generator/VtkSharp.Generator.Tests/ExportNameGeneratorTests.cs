using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Types;

namespace VtkSharp.Generator.Tests;

public sealed class ExportNameGeneratorTests
{
    [Fact]
    public void Create_WithoutOverloads_UsesClassAndMethod()
    {
        var generator = new ExportNameGenerator();
        var name = generator.Create("vtkActor", "SetMapper", [new("vtkMapper*")], hasOverloads: false);
        Assert.Equal("vtkActor_SetMapper", name);
    }

    [Fact]
    public void Create_WithOverloads_UsesParameterSuffix()
    {
        var generator = new ExportNameGenerator();
        var name = generator.Create("vtkActor", "SetPosition", [new("double"), new("double"), new("double")], hasOverloads: true);
        Assert.Equal("vtkActor_SetPosition_double_double_double", name);
    }

    [Fact]
    public void Create_WithArrayOverload_UsesArraySuffix()
    {
        var generator = new ExportNameGenerator();
        var name = generator.Create("vtkTransform", "SetMatrix", [new("const double[16]")], hasOverloads: true);
        Assert.Equal("vtkTransform_SetMatrix_doubleConstArray16", name);
    }
}
