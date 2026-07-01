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
        var wpfControl = ReadWpfControlText();
        var managedWrapper = File.ReadAllText(Path.Combine(
            root.FullName,
            "src",
            "bindings",
            "VtkSharp.Wpf",
            "VtkOpenGlD3DImageRender.cs"));

        Assert.DoesNotContain(LegacyBridgePrefix, nativeExport);
        Assert.DoesNotContain(LegacyBridgePrefix, wpfControl);
        Assert.DoesNotContain(LegacyBridgePrefix, managedWrapper);
        Assert.DoesNotContain(LegacyTargetPrefix, nativeExport);
        Assert.DoesNotContain(LegacyTargetPrefix, wpfControl);
        Assert.DoesNotContain(LegacyTargetPrefix, managedWrapper);
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
        var managedNames = GetDllImportNames(managedWrapper);

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
        Assert.Contains("sealed class VtkOpenGlD3DImageRender : IDisposable", managedWrapper);
        Assert.Contains("public bool SetSize(int width, int height)", managedWrapper);
        Assert.Contains("public bool Render()", managedWrapper);
        Assert.Contains("VTKSHARP_API bool VtkOpenGlD3DImageRender_SetSize", nativeExport);
        Assert.Contains("VTKSHARP_API bool VtkOpenGlD3DImageRender_Render", nativeExport);
        Assert.Contains("[return: MarshalAs(UnmanagedType.U1)]", managedWrapper);
        Assert.Contains("if (!this._render.SetSize(pixelSize.Width, pixelSize.Height))", wpfControl);
        Assert.Contains("renderFailure = this.GetRenderError(\"Failed to resize the VTK D3DImage render target.\");", wpfControl);
        Assert.Contains("if (!this._render.Render())", wpfControl);
        Assert.Contains("renderFailure = this.GetRenderError(\"Failed to render the VTK scene.\");", wpfControl);
        Assert.Contains("public void Dispose()", managedWrapper);
        Assert.DoesNotContain("DllImport", wpfControl);
        Assert.DoesNotContain("nint _bridge", wpfControl);
    }

    [Fact]
    public void WpfD3DImageControl_DisposesTimerAndCursorObserversBeforeNativeObjects()
    {
        var wpfControl = ReadWpfControlText();

        Assert.Contains("this.DetachTimerObservers();", wpfControl);
        Assert.Contains("this.DetachCursorObserver();", wpfControl);

        var disposeBody = GetMethodBody(wpfControl, "DisposeVtkRender");
        Assert.True(
            disposeBody.IndexOf("this.DetachTimerObservers();", StringComparison.Ordinal) <
            disposeBody.IndexOf("this.Interactor?.Dispose();", StringComparison.Ordinal));
        Assert.True(
            disposeBody.IndexOf("this.DetachCursorObserver();", StringComparison.Ordinal) <
            disposeBody.IndexOf("this.Interactor?.Dispose();", StringComparison.Ordinal));
    }

    [Fact]
    public void WpfD3DImageControl_CanKeepNativeResourcesWhenUnloaded()
    {
        var wpfControl = ReadWpfControlText();

        Assert.Contains("public static readonly DependencyProperty DisposeOnUnloadProperty", wpfControl);
        Assert.Contains("public bool DisposeOnUnload", wpfControl);
        Assert.Contains("private void SuspendVtkRender()", wpfControl);

        var unloadedBody = GetMethodBody(wpfControl, "OnUnloaded");
        Assert.Contains("if (this.DisposeOnUnload)", unloadedBody);
        Assert.Contains("this.DisposeVtkRender();", unloadedBody);
        Assert.Contains("this.SuspendVtkRender();", unloadedBody);

        var suspendBody = GetMethodBody(wpfControl, "SuspendVtkRender");
        Assert.Contains("this.StopPlatformTimers();", suspendBody);
    }

    [Fact]
    public void WpfD3DImageControl_ReleasesActiveMouseButtonWhenCaptureIsLost()
    {
        var wpfControl = ReadWpfControlText();

        var lostCaptureBody = GetMethodBody(wpfControl, "OnLostMouseCapture");
        Assert.Contains("this._activeMouseButton is not { } activeButton", lostCaptureBody);
        Assert.Contains("this.InvokeMouseButtonEvent(activeButton, pressed: false", lostCaptureBody);
        Assert.Contains("this._activeMouseButton = null;", lostCaptureBody);
    }

    [Fact]
    public void WpfD3DImageControl_ReportsRenderFailures()
    {
        var wpfControl = ReadWpfControlText();

        Assert.Contains("public event EventHandler<VtkRenderFailedEventArgs>? RenderFailed;", wpfControl);
        Assert.Contains("renderFailure = this.GetRenderError(\"Failed to resize the VTK D3DImage render target.\");", wpfControl);
        Assert.Contains("renderFailure = this.GetRenderError(\"The VTK D3DImage render target did not provide a back buffer.\");", wpfControl);
        Assert.Contains("renderFailure = this.GetRenderError(\"Failed to render the VTK scene.\");", wpfControl);
        Assert.Contains("this.OnRenderFailed(renderFailure);", wpfControl);
    }

    [Fact]
    public void WpfD3DImageControl_SyncsDpiAndCursorState()
    {
        var wpfControl = ReadWpfControlText();

        Assert.Contains("protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)", wpfControl);
        Assert.Contains("this._cursorChangedObserver = renderWindow.AddObserver(vtkCommand.CursorChangedEvent", wpfControl);
        Assert.Contains("this.SyncCursor();", wpfControl);
        Assert.Contains("private static Cursor MapVtkCursor(int vtkCursor)", wpfControl);
    }

    [Fact]
    public void WpfD3DImageControl_ClampsPixelCoordinatesToRenderTarget()
    {
        var wpfControl = ReadWpfControlText();

        var getPixelPositionBody = GetMethodBody(wpfControl, "GetPixelPosition");
        Assert.Contains("var pixelSize = this.GetPixelSize(new Size(this.ActualWidth, this.ActualHeight));", getPixelPositionBody);
        Assert.Contains("ClampPixelCoordinate((int)Math.Round(position.X * transform.M11), pixelSize.Width)", getPixelPositionBody);
        Assert.Contains("ClampPixelCoordinate((int)Math.Round(position.Y * transform.M22), pixelSize.Height)", getPixelPositionBody);
        Assert.Contains("private static int ClampPixelCoordinate(int value, int length)", wpfControl);
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

    private static string ReadRepositoryText(params string[] pathParts)
    {
        return File.ReadAllText(Path.Combine(new[] { FindRepositoryRoot().FullName }.Concat(pathParts).ToArray()));
    }

    private static string ReadWpfControlText()
    {
        var directory = Path.Combine(FindRepositoryRoot().FullName, "src", "bindings", "VtkSharp.Wpf");
        return string.Join(
            Environment.NewLine,
            Directory
                .EnumerateFiles(directory, "VtkOpenGlD3DImageRenderControl*.cs")
                .OrderBy(Path.GetFileName)
                .Select(File.ReadAllText));
    }

    private static string GetMethodBody(string sourceText, string methodName)
    {
        var methodMatch = Regex.Match(sourceText, $@"\b(?:private|protected|public)\s+(?:override\s+)?[\w<>\?]+\s+{Regex.Escape(methodName)}\s*\(");
        Assert.True(methodMatch.Success, $"Could not find method '{methodName}'.");
        var methodIndex = methodMatch.Index;

        var bodyStart = sourceText.IndexOf('{', methodIndex);
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
