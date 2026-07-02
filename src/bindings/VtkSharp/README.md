# VtkSharp

Unofficial .NET bindings for [VTK](https://vtk.org/) (Visualization Toolkit).

## Features

- C ABI shim + C# P/Invoke approach — no C++/CLI required
- Multi-target: `netstandard2.0` and `net8.0`
- Generated bindings with hand-written sugar for common patterns
- Bundled native `VtkSharp.Native.dll` for Windows x64

## Getting Started

```csharp
using VtkSharp;

// Create a renderer
var renderer = vtkRenderer.New();
renderer.SetBackground(0.1, 0.2, 0.4);

// Create a render window
var renderWindow = vtkRenderWindow.New();
renderWindow.AddRenderer(renderer);
```

## License

BSD 3-Clause. Not affiliated with or endorsed by Kitware.
