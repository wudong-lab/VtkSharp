using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

[Example("ImageIteratorDemo", "ImageData",
    Description = "Demonstrates the use of vtkImageIterator to set values in an image.",
    SourceFiles = new[] { "Examples/ImageData/ImageIteratorDemo/ImageIteratorDemo.cs" })]
internal class ImageIteratorDemo : IExample
{
    public void Run()
    {
        // vtkImageIterator is an efficient way to access the regions of a
        // vtkImageData. A span in the vtkImageData is a continuous segment
        // of pixels. The NextSpan() method increments the iterator to the
        // next continuous segment.
        //
        // VTK example source: https://examples.vtk.org/site/Cxx/ImageData/ImageIteratorDemo/

        using var colors = vtkNamedColors.New();

        // Create an image data
        using var imageData = vtkImageData.New();

        // Specify the size of the image data
        imageData.SetDimensions(100, 200, 30);
        imageData.AllocateScalars(3, 3); // VTK_UNSIGNED_CHAR = 3, 3 components (RGB)

        // Fill every entry of the image data with "Banana" color (workaround:
        // GetScalarPointer returns void* which is unsupported by the binding
        // generator; use SetScalarComponentFromDouble instead).
        Span<double> bananaRgba = stackalloc double[4];
        colors.GetColor("Banana", bananaRgba);
        double bananaR = bananaRgba[0];
        double bananaG = bananaRgba[1];
        double bananaB = bananaRgba[2];

        for (int z = 0; z < 30; z++)
        {
            for (int y = 0; y < 200; y++)
            {
                for (int x = 0; x < 100; x++)
                {
                    imageData.SetScalarComponentFromDouble(x, y, z, 0, bananaR);
                    imageData.SetScalarComponentFromDouble(x, y, z, 1, bananaG);
                    imageData.SetScalarComponentFromDouble(x, y, z, 2, bananaB);
                }
            }
        }

        // Define the extent to be modified
        int xMin = 20, xMax = 50;
        int yMin = 30, yMax = 60;
        int zMin = 10, zMax = 20;

        // Set the entries in the region to "Tomato" color (workaround:
        // vtkImageIterator is a C++ template class and cannot be bound;
        // iterate manually with nested loops instead).
        Span<double> tomatoRgba = stackalloc double[4];
        colors.GetColor("Tomato", tomatoRgba);
        double tomatoR = tomatoRgba[0];
        double tomatoG = tomatoRgba[1];
        double tomatoB = tomatoRgba[2];

        int counter = 0;
        for (int z = zMin; z <= zMax; z++)
        {
            counter++;
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    imageData.SetScalarComponentFromDouble(x, y, z, 0, tomatoR);
                    imageData.SetScalarComponentFromDouble(x, y, z, 1, tomatoG);
                    imageData.SetScalarComponentFromDouble(x, y, z, 2, tomatoB);
                }
            }
        }

        Debug.WriteLine($"Number of spans (z-slices modified): {counter}");

        int pixelsModified = (xMax - xMin + 1) * (yMax - yMin + 1) * (zMax - zMin + 1);
        Debug.WriteLine($"Pixels modified in region:          {pixelsModified}");

        // Visualize
        using var imageViewer = vtkImageViewer2.New();
        imageViewer.SetInputData(imageData);

        using var style = vtkInteractorStyleImage.New();
        style.SetInteractionModeToImageSlicing();

        using var renderWindowInteractor = vtkRenderWindowInteractor.New();
        renderWindowInteractor.SetInteractorStyle(style);
        imageViewer.SetupInteractor(renderWindowInteractor);
        imageViewer.SetSlice((zMax - zMin) / 2 + zMin);

        var slateGrey = colors.GetColor3d("Slate_grey");
        imageViewer.GetRenderer().SetBackground(slateGrey.R, slateGrey.G, slateGrey.B);
        imageViewer.GetImageActor().InterpolateOff();

        imageViewer.Render();
        imageViewer.GetRenderer().ResetCamera();
        imageViewer.GetRenderWindow().SetWindowName("ImageIteratorDemo");

        imageViewer.Render();

        renderWindowInteractor.Start();
    }
}
