using VtkSharp.Generator.Core.Generation;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class TypeClassifierTests
{
    [TestMethod]
    [DataRow("vtkTypeBool*")]
    [DataRow("const vtkTypeUInt32*")]
    [DataRow("vtkIdType*")]
    [DataRow("vtkMTimeType*")]
    public void TryGetVtkClassPointerName_ReturnsFalseForVtkScalarPointers(string type)
    {
        var result = TypeClassifier.TryGetVtkClassPointerName(type, out var className);

        Assert.IsFalse(result);
        Assert.AreEqual("", className);
    }

    [TestMethod]
    [DataRow("vtkMapper*", "vtkMapper")]
    [DataRow("const vtkAlgorithmOutput*", "vtkAlgorithmOutput")]
    public void TryGetVtkClassPointerName_ReturnsTrueForVtkObjectPointers(string type, string expectedClassName)
    {
        var result = TypeClassifier.TryGetVtkClassPointerName(type, out var className);

        Assert.IsTrue(result);
        Assert.AreEqual(expectedClassName, className);
    }
}
