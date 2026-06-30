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
            "VtkOpenGLD3DImageRenderControl.cs"));

        Assert.DoesNotContain(LegacyBridgePrefix, nativeExport);
        Assert.DoesNotContain(LegacyBridgePrefix, wpfControl);
        Assert.DoesNotContain(LegacyTargetPrefix, nativeExport);
        Assert.DoesNotContain(LegacyTargetPrefix, wpfControl);
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
        var managedNames = GetDllImportNames(wpfControl);

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
