using System.Diagnostics;
using System.Windows;
using VtkSharp.Wpf;

namespace VtkSharp.ExampleBrowser.ExtraExamples.VtkRenderControlDemo;

public partial class VtkRenderControlDemoWindow : Window
{
    public VtkRenderControlDemoWindow()
    {
        this.InitializeComponent();
    }

    private void VtkRenderControl1_OnVtkRenderInitialized(object? sender, VtkRenderInitializedEventArgs e) { }

    private void VtkRenderControl1_OnVtkRenderFailed(object? sender, VtkRenderFailedEventArgs e)
    {
        Debug.WriteLine($"VtkRenderControl1 failed: {e.Message}");
    }

    private void ButtonRender1_OnClick(object sender, RoutedEventArgs e)
    {
        using var cone = vtkConeSource.New();

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(cone.GetOutputPort());

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        actor.GetProperty().SetColor(VtkColor3d.LimeGreen);

        var renderer = this.VtkRenderControl1.Renderer!;
        renderer.AddActor(actor);
        renderer.ResetCamera();

        this.VtkRenderControl1.Render();
    }

    private void VtkRenderControl2_OnVtkRenderInitialized(object? sender, VtkRenderInitializedEventArgs e) { }

    private void VtkRenderControl2_OnVtkRenderFailed(object? sender, VtkRenderFailedEventArgs e)
    {
        Debug.WriteLine($"VtkRenderControl2 failed: {e.Message}");
    }

    private void ButtonRender2_OnClick(object sender, RoutedEventArgs e)
    {
        using var cone = vtkConeSource.New();

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(cone.GetOutputPort());

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        actor.GetProperty().SetColor(VtkColor3d.LimeGreen);

        var renderer = this.VtkRenderControl2.Renderer!;
        renderer.AddActor(actor);
        renderer.ResetCamera();

        this.VtkRenderControl2.Render();
    }
}