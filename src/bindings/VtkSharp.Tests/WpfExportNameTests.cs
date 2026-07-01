using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace VtkSharp.Tests;

public sealed class WpfExportNameTests
{
    private const string ExpectedPrefix = "VtkOpenGlD3DImageRender_";
    private const string LegacyBridgePrefix = "VtkWpfOpenGLD3DImageRenderBridge_";
    private const string LegacyTargetPrefix = "VtkWpfD3DImageOpenGLRenderTarget";

    [Fact]
    public void WpfD3DImageInterop_UsesNormalizedExportNames()
    {
        var root = FindRepositoryRoot();
        var nativeExport = File.ReadAllText(Path.Combine(
            root.FullName,
            "src",
            "native",
            "src",
            "wpf",
            "VtkOpenGlD3DImageRender_export.cpp"));
        var wpfControl = File.ReadAllText(Path.Combine(
            root.FullName,
            "src",
            "bindings",
            "VtkSharp.Wpf",
            "VtkOpenGlD3DImageRenderControl.cs"));
        var managedWrapper = File.ReadAllText(Path.Combine(
            root.FullName,
            "src",
            "bindings",
            "VtkSharp.Wpf",
            "VtkOpenGlD3DImageRender.cs"));

        Assert.DoesNotContain(LegacyBridgePrefix, nativeExport);
        Assert.DoesNotContain(LegacyBridgePrefix, wpfControl);
        Assert.DoesNotContain(LegacyBridgePrefix, managedWrapper);
        Assert.DoesNotContain(LegacyTargetPrefix, nativeExport);
        Assert.DoesNotContain(LegacyTargetPrefix, wpfControl);
        Assert.DoesNotContain(LegacyTargetPrefix, managedWrapper);
        Assert.False(File.Exists(Path.Combine(
            root.FullName,
            "src",
            "native",
            "src",
            "wpf",
            "VtkWpfD3DImageOpenGLRenderTarget.cpp")));
        Assert.False(File.Exists(Path.Combine(
            root.FullName,
            "src",
            "native",
            "src",
            "wpf",
            "VtkWpfD3DImageOpenGLRenderTarget.h")));

        var nativeNames = GetExportNames(nativeExport);
        var managedNames = GetDllImportNames(managedWrapper);

        Assert.Equal(
            new[]
            {
                "VtkOpenGlD3DImageRender_New",
                "VtkOpenGlD3DImageRender_Delete",
                "VtkOpenGlD3DImageRender_GetRenderWindow",
                "VtkOpenGlD3DImageRender_GetRenderer",
                "VtkOpenGlD3DImageRender_SetSize",
                "VtkOpenGlD3DImageRender_Render",
                "VtkOpenGlD3DImageRender_GetBackBuffer",
                "VtkOpenGlD3DImageRender_GetLastError",
            },
            nativeNames);
        Assert.Equal(nativeNames, managedNames);
        Assert.Contains("sealed class VtkOpenGlD3DImageRender : IDisposable", managedWrapper);
        Assert.Contains("public bool SetSize(int width, int height)", managedWrapper);
        Assert.Contains("public bool Render()", managedWrapper);
        Assert.Contains("VTKSHARP_API bool VtkOpenGlD3DImageRender_SetSize", nativeExport);
        Assert.Contains("VTKSHARP_API bool VtkOpenGlD3DImageRender_Render", nativeExport);
        Assert.Contains("[return: MarshalAs(UnmanagedType.U1)]", managedWrapper);
        Assert.Contains("if (!this._render.SetSize(pixelSize.Width, pixelSize.Height))", wpfControl);
        Assert.Contains("renderFailure = this.GetRenderError(\"Failed to resize the VTK D3DImage render target.\");", wpfControl);
        Assert.Contains("if (!this._render.Render())", wpfControl);
        Assert.Contains("renderFailure = this.GetRenderError(\"Failed to render the VTK scene.\");", wpfControl);
        Assert.Contains("public void Dispose()", managedWrapper);
        Assert.DoesNotContain("DllImport", wpfControl);
        Assert.DoesNotContain("nint _bridge", wpfControl);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "native", "CMakeLists.txt")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find VtkSharp repository root.");
    }

    private static string[] GetExportNames(string text)
    {
        return Regex.Matches(text, $@"VTKSHARP_API\s+[\w:\*\s]+?\s+({ExpectedPrefix}\w+)\s*\(")
            .Select(match => match.Groups[1].Value)
            .ToArray();
    }

    private static string[] GetDllImportNames(string text)
    {
        return Regex.Matches(text, $@"private\s+static\s+extern\s+\w+\s+({ExpectedPrefix}\w+)\s*\(")
            .Select(match => match.Groups[1].Value)
            .ToArray();
    }
}
