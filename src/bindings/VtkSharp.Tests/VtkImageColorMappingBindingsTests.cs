using System;
using System.Runtime.InteropServices;

namespace VtkSharp.Tests;

public sealed class VtkImageColorMappingBindingsTests
{
    [Fact]
    public void ImageData_RoundTripsOriginAndSpacing()
    {
        using var image = vtkImageData.New();
        Span<double> actual = stackalloc double[3];

        image.SetOrigin(1.25, -2.5, 3.75);
        image.GetOrigin(actual);
        Assert.Equal([1.25, -2.5, 3.75], actual.ToArray());

        image.SetOrigin(new double[] { 4, 5, 6 });
        image.GetOrigin(actual);
        Assert.Equal([4, 5, 6], actual.ToArray());

        image.SetSpacing(0.1, 0.2, 0.3);
        image.GetSpacing(actual);
        Assert.Equal([0.1, 0.2, 0.3], actual.ToArray());

        image.SetSpacing(new double[] { 0.4, 0.5, 0.6 });
        image.GetSpacing(actual);
        Assert.Equal([0.4, 0.5, 0.6], actual.ToArray());
    }

    [Fact]
    public void ImageMapToColors_MapsFloatScalarsToRgbaAndPreservesTransparentNan()
    {
        using var scalars = vtkFloatArray.New();
        scalars.SetNumberOfComponents(1);
        scalars.InsertNextTuple1(double.NaN);
        scalars.InsertNextTuple1(1);

        using var image = vtkImageData.New();
        image.SetDimensions(2, 1, 1);
        image.GetPointData().SetScalars(scalars);

        using var lookupTable = vtkLookupTable.New();
        lookupTable.SetNumberOfTableValues(2);
        lookupTable.SetTableRange(0, 1);
        lookupTable.SetTableValue(0, 0, 0, 1, 1);
        lookupTable.SetTableValue(1, 1, 0, 0, 1);
        lookupTable.SetNanColor(0, 0, 0, 0);
        lookupTable.Build();

        using var mapToColors = vtkImageMapToColors.New();
        mapToColors.SetInputData(image);
        mapToColors.SetLookupTable(lookupTable);
        mapToColors.SetOutputFormatToRGBA();
        mapToColors.SetActiveComponent(0);
        mapToColors.SetPassAlphaToOutput(false);
        mapToColors.SetNaNColor(new byte[] { 7, 8, 9, 10 });

        Span<byte> nanColor = stackalloc byte[4];
        mapToColors.GetNaNColor(nanColor);
        Assert.Equal([7, 8, 9, 10], nanColor.ToArray());
        Assert.Equal(0, mapToColors.GetActiveComponent());
        Assert.False(mapToColors.GetPassAlphaToOutput());
        Assert.False(mapToColors.GetLookupTable().OwnsReference);

        mapToColors.Update();
        using var output = vtkImageData.Register(mapToColors.GetOutput());
        var pointer = output.GetScalarPointer();

        Assert.Equal(4, output.GetNumberOfScalarComponents());
        Assert.Equal([0, 0, 0, 0], ReadRgba(pointer, pixelIndex: 0));
        Assert.Equal([255, 0, 0, 255], ReadRgba(pointer, pixelIndex: 1));
    }

    private static byte[] ReadRgba(nint pointer, int pixelIndex)
    {
        var offset = pixelIndex * 4;
        return
        [
            Marshal.ReadByte(pointer, offset),
            Marshal.ReadByte(pointer, offset + 1),
            Marshal.ReadByte(pointer, offset + 2),
            Marshal.ReadByte(pointer, offset + 3),
        ];
    }
}
