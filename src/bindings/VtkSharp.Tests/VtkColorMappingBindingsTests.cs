using System;

namespace VtkSharp.Tests;

public sealed class VtkColorMappingBindingsTests
{
    [Fact]
    public void ColorTransferFunction_InterpolatesRgbControlPoints()
    {
        using var transferFunction = vtkColorTransferFunction.New();
        transferFunction.SetColorSpaceToRGB();
        transferFunction.AddRGBPoint(0.0, 0.0, 0.0, 1.0);
        transferFunction.AddRGBPoint(1.0, 1.0, 0.0, 0.0);

        Span<double> rgb = stackalloc double[3];
        transferFunction.GetColor(0.5, rgb);

        Assert.Equal(2, transferFunction.GetSize());
        Assert.Equal(0.5, rgb[0], 12);
        Assert.Equal(0.0, rgb[1], 12);
        Assert.Equal(0.5, rgb[2], 12);
    }

    [Fact]
    public void DiscretizableColorTransferFunction_BuildsRequestedNumberOfColors()
    {
        using var transferFunction = vtkDiscretizableColorTransferFunction.New();
        transferFunction.AddRGBPoint(0.0, 0.0, 0.0, 1.0);
        transferFunction.AddRGBPoint(1.0, 1.0, 0.0, 0.0);
        transferFunction.SetDiscretize(true);
        transferFunction.SetNumberOfValues(4);
        transferFunction.Build();

        Assert.True(transferFunction.GetDiscretize());
        Assert.Equal(4, transferFunction.GetNumberOfValues());
        Assert.Equal(4, transferFunction.GetNumberOfAvailableColors());
    }

    [Fact]
    public void ColorSeries_CreatesOwnedOrdinalLookupTable()
    {
        using var colorSeries = vtkColorSeries.New();
        using var lookupTable = colorSeries.CreateLookupTable(0);

        Assert.True(lookupTable.OwnsReference);
        Assert.Equal(1, lookupTable.ReferenceCount);
        Assert.Equal(colorSeries.GetNumberOfColors(), lookupTable.GetNumberOfTableValues());
    }

    [Fact]
    public void ColorSeries_ReturnsUnsignedByteColors()
    {
        using var colorSeries = vtkColorSeries.New();

        var first = colorSeries.GetColor(0);
        var repeated = colorSeries.GetColorRepeating(colorSeries.GetNumberOfColors());

        Assert.Equal(first.R, repeated.R);
        Assert.Equal(first.G, repeated.G);
        Assert.Equal(first.B, repeated.B);
    }

    [Fact]
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

    [Theory]
    [InlineData("Custom scheme")]
    [InlineData("自定义配色 🎨")]
    public void ColorSeries_RoundTripsColorSchemeName(string name)
    {
        using var colorSeries = vtkColorSeries.New();

        colorSeries.SetColorSchemeName(name);

        Assert.Equal(name, colorSeries.GetColorSchemeName());
    }

    private static void AssertColor(VtkColor3ub color, byte r, byte g, byte b)
    {
        Assert.Equal(r, color.R);
        Assert.Equal(g, color.G);
        Assert.Equal(b, color.B);
    }
}
