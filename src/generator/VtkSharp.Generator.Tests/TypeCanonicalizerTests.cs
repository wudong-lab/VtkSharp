using VtkSharp.Generator.Core.Types;

namespace VtkSharp.Generator.Tests;

public sealed class TypeCanonicalizerTests
{
    [Theory]
    [InlineData("vtkMapper *", "vtkMapper*")]
    [InlineData("vtkMapper const *", "const vtkMapper*")]
    [InlineData("char const *", "const char*")]
    [InlineData("double const[3]", "const double[3]")]
    [InlineData("double [3]", "double[3]")]
    [InlineData("HWND__ *", "HWND")]
    public void Canonicalize_NormalizesSupportedSpelling(string input, string expected)
    {
        var canonicalizer = new TypeCanonicalizer();
        Assert.Equal(expected, canonicalizer.Canonicalize(input).Text);
    }
}
