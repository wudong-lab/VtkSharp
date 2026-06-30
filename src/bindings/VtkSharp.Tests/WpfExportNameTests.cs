using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace VtkSharp.Tests;

public sealed class WpfExportNameTests
{
    private const string ExpectedPrefix = "VtkWpfD3DImageOpenGLRenderTarget_";
    private const string LegacyPrefix = "VtkWpfOpenGLD3DImageRenderBridge_";

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
            "VtkWpfD3DImageOpenGLRenderTarget_export.cpp"));
        var wpfControl = File.ReadAllText(Path.Combine(
            root.FullName,
            "src",
            "bindings",
            "VtkSharp.Wpf",
            "VtkOpenGLD3DImageRenderControl.cs"));

        Assert.DoesNotContain(LegacyPrefix, nativeExport);
        Assert.DoesNotContain(LegacyPrefix, wpfControl);

        var nativeNames = GetExportNames(nativeExport);
        var managedNames = GetDllImportNames(wpfControl);

        Assert.Equal(
            new[]
            {
                "VtkWpfD3DImageOpenGLRenderTarget_New",
                "VtkWpfD3DImageOpenGLRenderTarget_Delete",
                "VtkWpfD3DImageOpenGLRenderTarget_GetRenderWindow",
                "VtkWpfD3DImageOpenGLRenderTarget_GetRenderer",
                "VtkWpfD3DImageOpenGLRenderTarget_SetSize",
                "VtkWpfD3DImageOpenGLRenderTarget_Render",
                "VtkWpfD3DImageOpenGLRenderTarget_GetBackBuffer",
                "VtkWpfD3DImageOpenGLRenderTarget_GetLastError",
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
