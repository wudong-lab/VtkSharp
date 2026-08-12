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
}
