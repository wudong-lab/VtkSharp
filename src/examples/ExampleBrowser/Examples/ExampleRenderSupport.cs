using System.Diagnostics;
using VtkSharp;

namespace VtkSharp.ExampleBrowser.Examples;

internal static class ExampleRenderSupport
{
    public static void Finish(vtkRenderWindow window, vtkRenderWindowInteractor interactor,
        string exampleName, string? screenshotPath)
    {
        window.Render();
        if (screenshotPath is not null)
        {
            using var image = window.GetRgbImageData();
            using var writer = vtkPNGWriter.New();
            writer.SetInputData(image);
            writer.SetFileName(screenshotPath);
            writer.Write();
            return;
        }

        Debug.WriteLine($"{exampleName} example running. Close the window to exit.");
        interactor.Start();
    }
}
