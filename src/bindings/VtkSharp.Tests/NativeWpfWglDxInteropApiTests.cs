using System;
using System.IO;

namespace VtkSharp.Tests;

public sealed class NativeWpfWglDxInteropApiTests
{
    [Fact]
    public void WglDxInteropHandles_AreOwnedByWglDxInteropApi()
    {
        var root = FindRepositoryRoot();
        var nativeDirectory = Path.Combine(root.FullName, "src", "native", "src", "wpf");
        var renderHeader = File.ReadAllText(Path.Combine(nativeDirectory, "VtkOpenGlD3DImageRender.h"));
        var renderSource = File.ReadAllText(Path.Combine(nativeDirectory, "VtkOpenGlD3DImageRender.cpp"));
        var interopHeader = File.ReadAllText(Path.Combine(nativeDirectory, "WglDxInterop.h"));
        var interopSource = File.ReadAllText(Path.Combine(nativeDirectory, "WglDxInterop.cpp"));

        Assert.DoesNotContain("m_dxInteropDevice", renderHeader);
        Assert.DoesNotContain("m_dxInteropObject", renderHeader);
        Assert.DoesNotContain("m_openDevice", renderSource);
        Assert.DoesNotContain("m_closeDevice", renderSource);
        Assert.DoesNotContain("m_registerObject", renderSource);
        Assert.DoesNotContain("m_unregisterObject", renderSource);
        Assert.DoesNotContain("m_lockObjects", renderSource);
        Assert.DoesNotContain("m_unlockObjects", renderSource);

        Assert.Contains("bool OpenDevice", interopHeader);
        Assert.Contains("bool SetResourceShareHandle", interopHeader);
        Assert.Contains("bool RegisterObject", interopHeader);
        Assert.Contains("bool LockObject", interopHeader);
        Assert.Contains("void UnlockObject", interopHeader);
        Assert.Contains("void UnregisterObject", interopHeader);
        Assert.Contains("void CloseDevice", interopHeader);
        Assert.Contains("const char* GetLastError() const", interopHeader);
        Assert.Contains("HANDLE m_device = nullptr", interopHeader);
        Assert.Contains("HANDLE m_object = nullptr", interopHeader);
        Assert.Contains("wglDXOpenDeviceNV", interopSource);
        Assert.Contains("wglDXRegisterObjectNV", interopSource);
    }

    [Fact]
    public void CreateInteropResource_ReleasesPartialResources_WhenResourceCreationFails()
    {
        var root = FindRepositoryRoot();
        var nativeDirectory = Path.Combine(root.FullName, "src", "native", "src", "wpf");
        var renderSource = File.ReadAllText(Path.Combine(nativeDirectory, "VtkOpenGlD3DImageRender.cpp"));

        Assert.Contains("if (!this->m_d3DRenderTarget.CreateTexture", renderSource);
        Assert.Contains("if (!this->m_wglDxInterop.SetResourceShareHandle", renderSource);
        Assert.Contains("if (!this->m_wglDxInterop.RegisterObject", renderSource);
        Assert.True(
            CountOccurrences(renderSource, "this->ReleaseInteropResource();") >= 4,
            "CreateInteropResource should release old resources first and clean up each failed creation step.");
    }

    [Fact]
    public void SetResourceShareHandle_ReportsFailureReason()
    {
        var root = FindRepositoryRoot();
        var nativeDirectory = Path.Combine(root.FullName, "src", "native", "src", "wpf");
        var interopSource = File.ReadAllText(Path.Combine(nativeDirectory, "WglDxInterop.cpp"));

        Assert.Contains("this->SetError(\"wglDXSetResourceShareHandleNV is not available.\");", interopSource);
        Assert.Contains("this->SetError(\"wglDXSetResourceShareHandleNV failed.\");", interopSource);
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

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
