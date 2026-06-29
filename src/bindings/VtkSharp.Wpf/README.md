# VtkSharp.Wpf

WPF integration for VtkSharp.

## Render Controls

Use `VtkOpenGLD3DImageRenderControl` for new WPF integration work. It renders VTK with `vtkGenericOpenGLRenderWindow` into a shared OpenGL/D3D9Ex texture displayed by WPF `D3DImage`, so VTK content participates in the WPF visual tree without HWND airspace issues or CPU frame readback.

Windows-only. The OpenGL/D3D9Ex backend requires a GPU/driver path that supports `WGL_NV_DX_interop`.

`VtkRenderHost` is the legacy `HwndHost`-based VTK host. Keep using it only when HWND hosting is acceptable; it has WPF airspace limitations.

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
