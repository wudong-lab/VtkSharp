using System;
using System.IO;
using System.Text.RegularExpressions;

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

    [Fact]
    public void D3DImageRender_MakesContextCurrentBeforeInteropResourceCreation()
    {
        var root = FindRepositoryRoot();
        var nativeDirectory = Path.Combine(root.FullName, "src", "bindings", "VtkSharp.Wpf.Native", "src");
        var renderSource = File.ReadAllText(Path.Combine(nativeDirectory, "VtkOpenGlD3DImageRender.cpp"));

        var createBody = GetMethodBody(renderSource, "CreateInteropResource");
        Assert.Contains("if (!this->m_wglContext.MakeCurrent())", createBody);
        Assert.True(
            createBody.IndexOf("if (!this->m_wglContext.MakeCurrent())", StringComparison.Ordinal) <
            createBody.IndexOf("this->ReleaseInteropResource();", StringComparison.Ordinal));
        Assert.True(
            createBody.IndexOf("if (!this->m_wglContext.MakeCurrent())", StringComparison.Ordinal) <
            createBody.IndexOf("this->m_openGlFramebuffer.Create()", StringComparison.Ordinal));
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

    private static string GetMethodBody(string sourceText, string methodName)
    {
        var methodMatch = Regex.Match(sourceText, $@"\b(?:bool|void)\s+VtkOpenGlD3DImageRender::{Regex.Escape(methodName)}\s*\(");
        Assert.True(methodMatch.Success, $"Could not find method '{methodName}'.");

        var bodyStart = sourceText.IndexOf('{', methodMatch.Index);
        Assert.True(bodyStart >= 0, $"Could not find method body for '{methodName}'.");

        var depth = 0;
        for (var i = bodyStart; i < sourceText.Length; i++)
        {
            if (sourceText[i] == '{')
            {
                depth++;
            }
            else if (sourceText[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return sourceText.Substring(bodyStart, i - bodyStart + 1);
                }
            }
        }

        throw new InvalidOperationException($"Could not parse method body for '{methodName}'.");
    }
}
