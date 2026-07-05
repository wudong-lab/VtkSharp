namespace VtkSharp;

public unsafe partial class vtkWindow
{
    public vtkImageData GetRgbImageData()
    {
        using var windowToImageFilter = vtkWindowToImageFilter.New();
        windowToImageFilter.SetInput(this);
        windowToImageFilter.SetScale(1, 1);
        windowToImageFilter.SetInputBufferTypeToRGB();
        windowToImageFilter.ReadFrontBufferOff();
        windowToImageFilter.Update();

        return vtkImageData.Register(windowToImageFilter.GetOutput());
    }

    public vtkImageData GetRgbaImageData()
    {
        using var windowToImageFilter = vtkWindowToImageFilter.New();
        windowToImageFilter.SetInput(this);
        windowToImageFilter.SetScale(1, 1);
        windowToImageFilter.SetInputBufferTypeToRGBA();
        windowToImageFilter.ReadFrontBufferOff();
        windowToImageFilter.Update();

        return vtkImageData.Register(windowToImageFilter.GetOutput());
    }
}