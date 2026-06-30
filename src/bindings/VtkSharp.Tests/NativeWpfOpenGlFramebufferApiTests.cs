using System;
using System.IO;

namespace VtkSharp.Tests;

public sealed class NativeWpfOpenGlFramebufferApiTests
{
    [Fact]
    public void OpenGlFramebufferDetails_AreIsolatedFromD3DImageRender()
    {
        var root = FindRepositoryRoot();
        var nativeDirectory = Path.Combine(root.FullName, "src", "native", "src", "wpf");
        var renderHeader = File.ReadAllText(Path.Combine(nativeDirectory, "VtkOpenGlD3DImageRender.h"));
        var renderSource = File.ReadAllText(Path.Combine(nativeDirectory, "VtkOpenGlD3DImageRender.cpp"));
        var apiHeader = File.ReadAllText(Path.Combine(nativeDirectory, "OpenGlFramebufferApi.h"));
        var apiSource = File.ReadAllText(Path.Combine(nativeDirectory, "OpenGlFramebufferApi.cpp"));

        Assert.DoesNotContain("GlGenFramebuffersProc", renderHeader);
        Assert.DoesNotContain("m_glGenFramebuffers", renderHeader);
        Assert.DoesNotContain("m_glBindFramebuffer", renderSource);
        Assert.DoesNotContain("m_glFramebufferTexture2D", renderSource);
        Assert.DoesNotContain("m_glCheckFramebufferStatus", renderSource);
        Assert.DoesNotContain("m_glDeleteFramebuffers", renderSource);

        Assert.Contains("class OpenGlFramebufferApi", apiHeader);
        Assert.Contains("GlGenFramebuffersProc", apiHeader);
        Assert.Contains("bool Load()", apiHeader);
        Assert.Contains("void CreateFramebuffer", apiHeader);
        Assert.Contains("bool RenderToTexture", apiHeader);
        Assert.Contains("void DeleteFramebuffer", apiHeader);
        Assert.Contains("wglGetProcAddress", apiSource);
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
