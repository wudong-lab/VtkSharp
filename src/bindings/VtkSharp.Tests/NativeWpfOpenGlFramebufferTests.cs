using System;
using System.IO;

namespace VtkSharp.Tests;

public sealed class NativeWpfOpenGlFramebufferTests
{
    [Fact]
    public void OpenGlFramebufferDetails_AreIsolatedFromD3DImageRender()
    {
        var root = FindRepositoryRoot();
        var nativeDirectory = Path.Combine(root.FullName, "src", "bindings", "VtkSharp.Wpf.Native", "src");
        var renderHeader = File.ReadAllText(Path.Combine(nativeDirectory, "VtkOpenGlD3DImageRender.h"));
        var renderSource = File.ReadAllText(Path.Combine(nativeDirectory, "VtkOpenGlD3DImageRender.cpp"));
        var framebufferHeader = File.ReadAllText(Path.Combine(nativeDirectory, "OpenGlFramebuffer.h"));
        var framebufferSource = File.ReadAllText(Path.Combine(nativeDirectory, "OpenGlFramebuffer.cpp"));

        Assert.DoesNotContain("GlGenFramebuffersProc", renderHeader);
        Assert.DoesNotContain("m_glGenFramebuffers", renderHeader);
        Assert.DoesNotContain("m_glTexture", renderHeader);
        Assert.DoesNotContain("m_framebuffer", renderHeader);
        Assert.DoesNotContain("m_glBindFramebuffer", renderSource);
        Assert.DoesNotContain("m_glFramebufferTexture2D", renderSource);
        Assert.DoesNotContain("m_glCheckFramebufferStatus", renderSource);
        Assert.DoesNotContain("m_glDeleteFramebuffers", renderSource);
        Assert.DoesNotContain("glGenTextures", renderSource);
        Assert.DoesNotContain("glBindTexture", renderSource);
        Assert.DoesNotContain("glTexParameteri", renderSource);
        Assert.DoesNotContain("glDeleteTextures", renderSource);

        Assert.Contains("class OpenGlFramebuffer", framebufferHeader);
        Assert.Contains("GlGenFramebuffersProc", framebufferHeader);
        Assert.Contains("bool Load()", framebufferHeader);
        Assert.Contains("bool Create()", framebufferHeader);
        Assert.Contains("GLuint GetTexture() const", framebufferHeader);
        Assert.Contains("bool RenderToTexture", framebufferHeader);
        Assert.Contains("void Release()", framebufferHeader);
        Assert.Contains("GLuint m_texture = 0", framebufferHeader);
        Assert.Contains("GLuint m_framebuffer = 0", framebufferHeader);
        Assert.Contains("GlGenFramebuffersProc glGenFramebuffers = nullptr", framebufferHeader);
        Assert.Contains("GlBindFramebufferProc glBindFramebuffer = nullptr", framebufferHeader);
        Assert.DoesNotContain("m_glGenFramebuffers", framebufferHeader);
        Assert.DoesNotContain("m_glBindFramebuffer", framebufferHeader);
        Assert.Contains("wglGetProcAddress", framebufferSource);
        Assert.Contains("glGenTextures", framebufferSource);
        Assert.Contains("glBindTexture", framebufferSource);
        Assert.Contains("glTexParameteri", framebufferSource);
        Assert.Contains("glDeleteTextures", framebufferSource);
    }

    [Fact]
    public void Release_DoesNotCallFramebufferDeleteFunction_WhenItWasNotLoaded()
    {
        var root = FindRepositoryRoot();
        var nativeDirectory = Path.Combine(root.FullName, "src", "bindings", "VtkSharp.Wpf.Native", "src");
        var framebufferSource = File.ReadAllText(Path.Combine(nativeDirectory, "OpenGlFramebuffer.cpp"));

        Assert.Contains("if (this->m_framebuffer && this->glDeleteFramebuffers)", framebufferSource);
    }

    [Fact]
    public void Create_ReportsTextureAndFramebufferCreationFailure()
    {
        var root = FindRepositoryRoot();
        var nativeDirectory = Path.Combine(root.FullName, "src", "bindings", "VtkSharp.Wpf.Native", "src");
        var renderSource = File.ReadAllText(Path.Combine(nativeDirectory, "VtkOpenGlD3DImageRender.cpp"));
        var framebufferSource = File.ReadAllText(Path.Combine(nativeDirectory, "OpenGlFramebuffer.cpp"));

        Assert.Contains("if (!this->glGenFramebuffers)", framebufferSource);
        Assert.Contains("if (!this->m_texture)", framebufferSource);
        Assert.Contains("if (!this->m_framebuffer)", framebufferSource);
        Assert.Contains("return false;", framebufferSource);
        Assert.Contains("return true;", framebufferSource);
        Assert.Contains("if (!this->m_openGlFramebuffer.Create())", renderSource);
        Assert.Contains("this->SetError(\"Failed to create shared OpenGL framebuffer.\");", renderSource);
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
