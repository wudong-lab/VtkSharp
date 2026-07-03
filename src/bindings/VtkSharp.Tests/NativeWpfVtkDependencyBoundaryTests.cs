using System;
using System.IO;

namespace VtkSharp.Tests;

public sealed class NativeWpfVtkDependencyBoundaryTests
{
    [Fact]
    public void WpfNative_DoesNotIncludeOrLinkVtkDirectly()
    {
        var root = FindRepositoryRoot();
        var wpfNativeDirectory = Path.Combine(root.FullName, "src", "bindings", "VtkSharp.Wpf.Native");
        var nativeCMake = File.ReadAllText(Path.Combine(wpfNativeDirectory, "CMakeLists.txt"));

        foreach (var file in Directory.EnumerateFiles(Path.Combine(wpfNativeDirectory, "src"), "*.*", SearchOption.AllDirectories))
        {
            if (!file.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase) &&
                !file.EndsWith(".h", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            Assert.DoesNotContain("#include <vtk", text);
            Assert.DoesNotContain("vtkSmartPointer", text);
        }

        Assert.DoesNotContain("${VTKSHARP_VTK_TARGETS}", nativeCMake);
        Assert.Contains("${VTKSHARP_NATIVE_TARGET}", nativeCMake);
    }

    [Fact]
    public void NativeCore_ExportsExternalOpenGlRenderContextBridge()
    {
        var root = FindRepositoryRoot();
        var header = File.ReadAllText(Path.Combine(
            root.FullName,
            "src",
            "bindings",
            "VtkSharp.Native",
            "include",
            "VtkSharpExternalOpenGlRenderContext.h"));
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "src",
            "bindings",
            "VtkSharp.Native",
            "src",
            "VtkSharpExtension",
            "VtkSharpExternalOpenGlRenderContext.cpp"));

        Assert.Contains("struct VtkSharpExternalOpenGlRenderContext", header);
        Assert.Contains("VtkSharpExternalOpenGlRenderContext_New", header);
        Assert.Contains("VtkSharpExternalOpenGlRenderContext_Delete", header);
        Assert.Contains("VtkSharpExternalOpenGlRenderContext_GetRenderWindow", header);
        Assert.Contains("VtkSharpExternalOpenGlRenderContext_GetRenderer", header);
        Assert.Contains("VtkSharpExternalOpenGlRenderContext_InitializeFromCurrentContext", header);
        Assert.Contains("VtkSharpExternalOpenGlRenderContext_SetSize", header);
        Assert.Contains("VtkSharpExternalOpenGlRenderContext_Render", header);

        Assert.Contains("vtkGenericOpenGLRenderWindow", source);
        Assert.Contains("vtkCallbackCommand", source);
        Assert.Contains("SetFrameBlitModeToBlitToCurrent", source);
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "bindings", "VtkSharp.Native", "CMakeLists.txt")))
                return directory;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find VtkSharp repository root.");
    }
}
