using System;
using System.IO;

namespace VtkSharp.Tests;

public sealed class NativeWpfD3DImageRenderTargetTests
{
    [Fact]
    public void Direct3DDetails_AreIsolatedFromOpenGlD3DImageRender()
    {
        var root = FindRepositoryRoot();
        var nativeDirectory = Path.Combine(root.FullName, "src", "bindings", "VtkSharp.Wpf.Native", "src");
        var renderHeader = File.ReadAllText(Path.Combine(nativeDirectory, "VtkOpenGlD3DImageRender.h"));
        var renderSource = File.ReadAllText(Path.Combine(nativeDirectory, "VtkOpenGlD3DImageRender.cpp"));
        var targetHeader = File.ReadAllText(Path.Combine(nativeDirectory, "D3DImageRenderTarget.h"));
        var targetSource = File.ReadAllText(Path.Combine(nativeDirectory, "D3DImageRenderTarget.cpp"));

        Assert.DoesNotContain("m_direct3D", renderHeader);
        Assert.DoesNotContain("m_d3DDevice", renderHeader);
        Assert.DoesNotContain("m_texture", renderHeader);
        Assert.DoesNotContain("m_surface", renderHeader);
        Assert.DoesNotContain("ReleaseCom", renderSource);
        Assert.DoesNotContain("CheckHr", renderHeader);
        Assert.DoesNotContain("CheckHr", renderSource);
        Assert.DoesNotContain("Direct3DCreate9Ex", renderSource);
        Assert.DoesNotContain("CreateDeviceEx", renderSource);
        Assert.DoesNotContain("->CreateTexture", renderSource);
        Assert.DoesNotContain("GetSurfaceLevel", renderSource);

        Assert.Contains("class D3DImageRenderTarget", targetHeader);
        Assert.Contains("IDirect3DDevice9Ex* GetDevice() const", targetHeader);
        Assert.Contains("IDirect3DTexture9* GetTexture() const", targetHeader);
        Assert.Contains("IDirect3DSurface9* GetSurface() const", targetHeader);
        Assert.Contains("HANDLE GetShareHandle() const", targetHeader);
        Assert.Contains("bool CheckHr", targetHeader);
        Assert.Contains("ReleaseCom", targetSource);
        Assert.Contains("Direct3DCreate9Ex", targetSource);
        Assert.Contains("CreateDeviceEx", targetSource);
        Assert.Contains("CreateTexture", targetSource);
        Assert.Contains("GetSurfaceLevel", targetSource);
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
