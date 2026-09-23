using VtkSharp.Generator.Core.Types;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class TypeCanonicalizerTests
{
    [TestMethod]
    [DataRow("vtkMapper *", "vtkMapper*")]
    [DataRow("vtkMapper const *", "const vtkMapper*")]
    [DataRow("char const *", "const char*")]
    [DataRow("double const[3]", "const double[3]")]
    [DataRow("double [3]", "double[3]")]
    [DataRow("HWND__ *", "HWND")]
    [DataRow("vtkColor3ub const&", "vtkColor3ub")]
    public void Canonicalize_NormalizesSupportedSpelling(string input, string expected)
    {
        var canonicalizer = new TypeCanonicalizer();
        Assert.AreEqual(expected, canonicalizer.Canonicalize(input).Text);
    }
}
