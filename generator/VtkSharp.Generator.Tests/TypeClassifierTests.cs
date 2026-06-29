using VtkSharp.Generator.Core.Generation;

namespace VtkSharp.Generator.Tests;

public sealed class TypeClassifierTests
{
    [Theory]
    [InlineData("vtkTypeBool*")]
    [InlineData("const vtkTypeUInt32*")]
    [InlineData("vtkIdType*")]
    [InlineData("vtkMTimeType*")]
    public void TryGetVtkClassPointerName_ReturnsFalseForVtkScalarPointers(string type)
    {
        var result = TypeClassifier.TryGetVtkClassPointerName(type, out var className);

        Assert.False(result);
        Assert.Equal("", className);
    }

    [Theory]
    [InlineData("vtkMapper*", "vtkMapper")]
    [InlineData("const vtkAlgorithmOutput*", "vtkAlgorithmOutput")]
    public void TryGetVtkClassPointerName_ReturnsTrueForVtkObjectPointers(string type, string expectedClassName)
    {
        var result = TypeClassifier.TryGetVtkClassPointerName(type, out var className);

        Assert.True(result);
        Assert.Equal(expectedClassName, className);
    }
}
