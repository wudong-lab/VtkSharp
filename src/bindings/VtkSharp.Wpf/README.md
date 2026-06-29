# VtkSharp.Wpf

WPF integration for VtkSharp.

## Recommended Control

Use `VtkOpenGLD3DImageRenderControl` for new WPF integration work. It renders VTK with `vtkGenericOpenGLRenderWindow` into a shared OpenGL/D3D9Ex texture displayed by WPF `D3DImage`, so VTK content participates in the WPF visual tree without HWND airspace issues or CPU frame readback.

Windows-only. The OpenGL/D3D9Ex backend requires a GPU/driver path that supports `WGL_NV_DX_interop`.

## Diagnostic Controls

The following controls are retained for comparison, fallback experiments, and backend diagnostics:

- `VtkRenderHost`: existing `HwndHost`-based VTK host. It is useful as a compatibility baseline but has WPF airspace limitations.
- `VtkNativeRenderControl`: offscreen VTK render plus CPU copy into `WriteableBitmap`.
- `VtkD3DImageRenderControl`: offscreen VTK render plus CPU upload into a D3D9Ex surface shown through `D3DImage`.
- `VtkOpenGLD3DImageProbeControl`: OpenGL-to-D3D9Ex probe that does not render VTK content.

## Getting Started

```xml
<!-- In your .csproj -->
<PackageReference Include="VtkSharp.Wpf" />
```

```xaml
<Window x:Class="MyApp.MainWindow"
        xmlns:vtk="clr-namespace:VtkSharp.Wpf;assembly=VtkSharp.Wpf">
    <vtk:VtkOpenGLD3DImageRenderControl x:Name="RenderControl" />
</Window>
```

## License

BSD 3-Clause. Not affiliated with or endorsed by Kitware.
