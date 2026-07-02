using System;
using System.IO;

namespace VtkSharp.Tests;

public sealed class NativeWpfWglContextTests
{
    [Fact]
    public void MakeCurrent_ReportsWhetherContextWasMadeCurrent()
    {
        var root = FindRepositoryRoot();
        var nativeDirectory = Path.Combine(root.FullName, "src", "bindings", "VtkSharp.Wpf.Native", "src");
        var contextHeader = File.ReadAllText(Path.Combine(nativeDirectory, "WglContext.h"));
        var contextSource = File.ReadAllText(Path.Combine(nativeDirectory, "WglContext.cpp"));
        var renderSource = File.ReadAllText(Path.Combine(nativeDirectory, "VtkOpenGlD3DImageRender.cpp"));

        Assert.Contains("bool MakeCurrent() const", contextHeader);
        Assert.Contains("if (!this->m_deviceContext || !this->m_glContext)", contextSource);
        Assert.Contains("return ::wglMakeCurrent(this->m_deviceContext, this->m_glContext) == TRUE;", contextSource);
        Assert.Contains("const bool isCurrent = this->m_wglContext.MakeCurrent();", renderSource);
        Assert.Contains("if (isCurrent)", renderSource);
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
