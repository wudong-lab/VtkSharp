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
        var interopHeader = File.ReadAllText(Path.Combine(nativeDirectory, "WglDxInteropApi.h"));
        var interopSource = File.ReadAllText(Path.Combine(nativeDirectory, "WglDxInteropApi.cpp"));

        Assert.DoesNotContain("m_dxInteropDevice", renderHeader);
        Assert.DoesNotContain("m_dxInteropObject", renderHeader);
        Assert.DoesNotContain("m_openDevice", renderSource);
        Assert.DoesNotContain("m_closeDevice", renderSource);
        Assert.DoesNotContain("m_registerObject", renderSource);
        Assert.DoesNotContain("m_unregisterObject", renderSource);
        Assert.DoesNotContain("m_lockObjects", renderSource);
        Assert.DoesNotContain("m_unlockObjects", renderSource);

        Assert.Contains("bool OpenDevice", interopHeader);
        Assert.Contains("bool RegisterObject", interopHeader);
        Assert.Contains("bool LockObject", interopHeader);
        Assert.Contains("void UnlockObject", interopHeader);
        Assert.Contains("void UnregisterObject", interopHeader);
        Assert.Contains("void CloseDevice", interopHeader);
        Assert.Contains("const char* GetLastError() const", interopHeader);
        Assert.Contains("HANDLE m_device = nullptr", interopHeader);
        Assert.Contains("HANDLE m_object = nullptr", interopHeader);
        Assert.Contains("m_openDevice", interopSource);
        Assert.Contains("m_registerObject", interopSource);
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
}
