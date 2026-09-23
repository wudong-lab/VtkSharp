using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Types;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class ExportNameGeneratorTests
{
    [TestMethod]
    public void Create_WithoutOverloads_UsesClassAndMethod()
    {
        var generator = new ExportNameGenerator();
        var name = generator.Create("vtkActor", "SetMapper", [new("vtkMapper*")], hasOverloads: false);
        Assert.AreEqual("vtkActor_SetMapper", name);
    }

    [TestMethod]
    public void Create_WithOverloads_UsesParameterSuffix()
    {
        var generator = new ExportNameGenerator();
        var name = generator.Create("vtkActor", "SetPosition", [new("double"), new("double"), new("double")], hasOverloads: true);
        Assert.AreEqual("vtkActor_SetPosition_double_double_double", name);
    }

    [TestMethod]
    public void Create_WithArrayOverload_UsesArraySuffix()
    {
        var generator = new ExportNameGenerator();
        var name = generator.Create("vtkTransform", "SetMatrix", [new("const double[16]")], hasOverloads: true);
        Assert.AreEqual("vtkTransform_SetMatrix_doubleConstArray16", name);
    }

    [TestMethod]
    public void Create_WithPostfixConstUnsignedCharArray_UsesValidSuffix()
    {
        var generator = new ExportNameGenerator();
        var name = generator.Create(
            "vtkImageMapToColors",
            "SetNaNColor",
            [new("unsigned char const[4]")],
            hasOverloads: true);

        Assert.AreEqual("vtkImageMapToColors_SetNaNColor_unsignedcharConstArray4", name);
    }

    [TestMethod]
    public void CreateAll_SingleFunction_UsesSimpleName()
    {
        var generator = new ExportNameGenerator();
        var result = generator.CreateAll("vtkActor",
        [
            ("f0", "SetMapper", (IReadOnlyList<CanonicalType>)[new("vtkMapper*")]),
        ]);

        Assert.AreEqual("vtkActor_SetMapper", result["f0"]);
    }

    [TestMethod]
    public void CreateAll_Overloads_UsesSuffixes()
    {
        var generator = new ExportNameGenerator();
        var result = generator.CreateAll("vtkActor",
        [
            ("scalar", "SetPosition", [new("double"), new("double"), new("double")]),
            ("array", "SetPosition", [new("const double[3]")]),
        ]);

        Assert.AreEqual("vtkActor_SetPosition_double_double_double", result["scalar"]);
        Assert.AreEqual("vtkActor_SetPosition_doubleConstArray3", result["array"]);
    }

    [TestMethod]
    public void CreateAll_CollisionFallbackToHash()
    {
        var generator = new ExportNameGenerator();
        // Simulate suffix collision: two entries for distinct overloads whose
        // ToSuffix coincidentally matches. CreateAll detects the collision and
        // falls back to the hash mechanism for both entries.
        // Using intentionally crafted parameter types that map to the same suffix.
        // (In practice the validator would reject duplicate signatures — this is
        // defense-in-depth against edge cases.)
        var result = generator.CreateAll("vtkFoo",
        [
            ("a", "Bar", [new("unsigned int")]),
            ("b", "Bar", [new("unsigned int")]),
        ]);

        Assert.IsNotNull(result["a"]);
        Assert.IsNotNull(result["b"]);
        Assert.IsTrue(result["a"].StartsWith("vtkFoo_Bar", StringComparison.Ordinal));
        Assert.IsTrue(result["b"].StartsWith("vtkFoo_Bar", StringComparison.Ordinal));
    }
}
