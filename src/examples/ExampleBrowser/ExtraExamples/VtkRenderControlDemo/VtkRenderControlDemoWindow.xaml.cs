using System.Windows;

namespace VtkSharp.ExampleBrowser.ExtraExamples.VtkRenderControlDemo;

public partial class VtkRenderControlDemoWindow : Window
{
    public VtkRenderControlDemoWindow()
    {
        this.InitializeComponent();
    }

    private void ButtonRender_OnClick(object sender, RoutedEventArgs e)
    {
        if (!this.VtkRenderControl.IsVtkRenderInitialized) return;

        using var cone = vtkConeSource.New();

        using var mapper = vtkPolyDataMapper.New();
        mapper.SetInputConnection(cone.GetOutputPort());

        using var actor = vtkActor.New();
        actor.SetMapper(mapper);
        actor.GetProperty().SetColor(VtkColor3d.Red);

        var renderer = this.VtkRenderControl.Renderer!;
        renderer.SetBackground(VtkColor3d.Black);
        renderer.AddActor(actor);
        renderer.ResetCamera();

        this.VtkRenderControl.Render();
    }
}