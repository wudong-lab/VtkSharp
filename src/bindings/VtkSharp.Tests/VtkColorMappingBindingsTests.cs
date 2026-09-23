using System;

namespace VtkSharp.Tests;

[TestClass]
public sealed class VtkColorMappingBindingsTests
{
    [TestMethod]
    public void ColorTransferFunction_InterpolatesRgbControlPoints()
    {
        using var transferFunction = vtkColorTransferFunction.New();
        transferFunction.SetColorSpaceToRGB();
        transferFunction.AddRGBPoint(0.0, 0.0, 0.0, 1.0);
        transferFunction.AddRGBPoint(1.0, 1.0, 0.0, 0.0);

        Span<double> rgb = stackalloc double[3];
        transferFunction.GetColor(0.5, rgb);

        Assert.AreEqual(2, transferFunction.GetSize());
        Assert.AreEqual(0.5, rgb[0], 12);
        Assert.AreEqual(0.0, rgb[1], 12);
        Assert.AreEqual(0.5, rgb[2], 12);
    }

    [TestMethod]
    public void DiscretizableColorTransferFunction_BuildsRequestedNumberOfColors()
    {
        using var transferFunction = vtkDiscretizableColorTransferFunction.New();
        transferFunction.AddRGBPoint(0.0, 0.0, 0.0, 1.0);
        transferFunction.AddRGBPoint(1.0, 1.0, 0.0, 0.0);
        transferFunction.SetDiscretize(true);
        transferFunction.SetNumberOfValues(4);
        transferFunction.Build();

        Assert.IsTrue(transferFunction.GetDiscretize());
        Assert.AreEqual(4, transferFunction.GetNumberOfValues());
        Assert.AreEqual(4, transferFunction.GetNumberOfAvailableColors());
    }

    [TestMethod]
    public void ColorSeries_CreatesOwnedOrdinalLookupTable()
    {
        using var colorSeries = vtkColorSeries.New();
        using var lookupTable = colorSeries.CreateLookupTable(0);

        Assert.IsTrue(lookupTable.OwnsReference);
        Assert.AreEqual(1, lookupTable.ReferenceCount);
        Assert.AreEqual(colorSeries.GetNumberOfColors(), lookupTable.GetNumberOfTableValues());
    }

    [TestMethod]
    public void ColorSeries_ReturnsUnsignedByteColors()
    {
        using var colorSeries = vtkColorSeries.New();

        var first = colorSeries.GetColor(0);
        var repeated = colorSeries.GetColorRepeating(colorSeries.GetNumberOfColors());

        Assert.AreEqual(first.R, repeated.R);
        Assert.AreEqual(first.G, repeated.G);
        Assert.AreEqual(first.B, repeated.B);
    }

    [TestMethod]
    public void ColorSeries_AcceptsUnsignedByteColors()
    {
        using var colorSeries = vtkColorSeries.New();
        colorSeries.SetNumberOfColors(1);

        colorSeries.SetColor(0, new VtkColor3ub(10, 20, 30));
        colorSeries.AddColor(new VtkColor3ub(40, 50, 60));
        colorSeries.InsertColor(1, new VtkColor3ub(70, 80, 90));

        AssertColor(colorSeries.GetColor(0), 10, 20, 30);
        AssertColor(colorSeries.GetColor(1), 70, 80, 90);
        AssertColor(colorSeries.GetColor(2), 40, 50, 60);
    }

    [TestMethod]
    [DataRow("Custom scheme")]
    [DataRow("自定义配色 🎨")]
    public void ColorSeries_RoundTripsColorSchemeName(string name)
    {
        using var colorSeries = vtkColorSeries.New();

        colorSeries.SetColorSchemeName(name);

        Assert.AreEqual(name, colorSeries.GetColorSchemeName());
    }

    private static void AssertColor(VtkColor3ub color, byte r, byte g, byte b)
    {
        Assert.AreEqual(r, color.R);
        Assert.AreEqual(g, color.G);
        Assert.AreEqual(b, color.B);
    }
}
