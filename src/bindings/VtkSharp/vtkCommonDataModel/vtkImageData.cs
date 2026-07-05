using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace VtkSharp;

public unsafe partial class vtkImageData
{
    /*[LibraryImport(InteropInfo.NativeLibraryName, StringMarshalling = StringMarshalling.Utf8)]
    private static partial void vtkImageData_GetPixelComponentByDim1Index(nint self, int dim1Index, int colorComponentIndex, Span<double> resultData);

    [LibraryImport(InteropInfo.NativeLibraryName, StringMarshalling = StringMarshalling.Utf8)]
    private static partial void vtkImageData_GetPixelComponentByDim1IndexRange(nint self, int dim1StartIndex, int dim1EndIndex, int colorComponentIndex, Span<double> resultData);

    public Dimension GetDimensions()
    {
        var data = new Span<int>(this.GetDimensions_Internal(), 3);
        return new(data[0], data[1], data[2]);
    }

    public void GetPixelComponentByDim1Index(int dim1Index, int colorComponentIndex, Span<double> resultData)
    {
        vtkImageData_GetPixelComponentByDim1Index(this.NativePointer, dim1Index, colorComponentIndex, resultData);
    }

    public void GetPixelComponentByDim1Index(int dim1StartIndex, int dim1EndIndex, int colorComponentIndex, Span<double> resultData)
    {
        vtkImageData_GetPixelComponentByDim1IndexRange(this.NativePointer, dim1StartIndex, dim1EndIndex, colorComponentIndex, resultData);
    }

    public static vtkImageData Subtract(vtkImageData imageData1, vtkImageData imageData2)
    {
        //Debug.Assert(imageData1.GetScalarType() == imageData2.GetScalarType());
        //Debug.Assert(imageData1.GetScalarSize() == imageData2.GetScalarSize());
        //Debug.Assert(imageData1.GetNumberOfScalarComponents() == imageData2.GetNumberOfScalarComponents());
        //Debug.Assert(imageData1.GetDimensions().Equals(imageData2.GetDimensions()));

        //vtkNew<vtkImageMathematics> imageMathematics;
        using var imageMathematics = vtkImageMathematics.New();
        imageMathematics.SetInput1Data(imageData1);
        imageMathematics.SetInput2Data(imageData2);
        imageMathematics.SetOperationToSubtract();
        imageMathematics.Update();

        return vtkImageData.Register(imageMathematics.GetOutput());
    }*/
}