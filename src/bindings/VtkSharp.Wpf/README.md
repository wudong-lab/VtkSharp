# VtkSharp.Wpf

WPF integration for VtkSharp. Provides an `HwndHost`-based VTK render window.

## Features

- `VtkRenderHost` control for embedding VTK in WPF applications
- Windows-only (requires `net10.0-windows`)

## Getting Started

```xml
<!-- In your .csproj -->
<PackageReference Include="VtkSharp.Wpf" />
```

```xaml
<Window x:Class="MyApp.MainWindow"
        xmlns:vtk="clr-namespace:VtkSharp.Wpf;assembly=VtkSharp.Wpf">
    <vtk:VtkRenderHost x:Name="RenderHost" />
</Window>
```

## License

BSD 3-Clause. Not affiliated with or endorsed by Kitware.
