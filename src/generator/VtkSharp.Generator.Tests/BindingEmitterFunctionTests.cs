using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

public sealed class BindingEmitterFunctionTests
{
    [Fact]
    public void CSharpEmitter_EmitsVoidMethodWithVtkObjectParameter()
    {
        var emitter = new CSharpBindingEmitter();

        var text = emitter.Emit("VtkSharp", "vtkAlgorithm", "vtkObject", hasStaticNew: true,
        [
            new WhitelistFunction
            {
                Name = "SetInputConnection",
                CppSignature = "void SetInputConnection(vtkAlgorithmOutput* input)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters = [new WhitelistParameter { Type = "vtkAlgorithmOutput*", Name = "input" }],
            },
        ]);

        Assert.Contains("public new void SetInputConnection(vtkAlgorithmOutput input)", text);
        Assert.Contains("public new static vtkAlgorithm Register(vtkAlgorithm sourceObject)", text);
        Assert.Contains("vtkAlgorithm_SetInputConnection(this.NativePointer, input.NativePointer);", text);
        Assert.Contains("private static extern void vtkAlgorithm_SetInputConnection(nint self, nint input);", text);
        Assert.Contains("#region Interop", text);
    }

    [Fact]
    public void CSharpEmitter_EmitsVtkObjectReturnMethod()
    {
        var emitter = new CSharpBindingEmitter();

        var text = emitter.Emit("VtkSharp", "vtkAlgorithm", "vtkObject", hasStaticNew: true,
        [
            new WhitelistFunction
            {
                Name = "GetOutputPort",
                CppSignature = "vtkAlgorithmOutput* GetOutputPort()",
                Return = new WhitelistReturn { Type = "vtkAlgorithmOutput*" },
                Parameters = [],
            },
        ]);

        Assert.Contains("public new vtkAlgorithmOutput GetOutputPort()", text);
        Assert.Contains("return vtkAlgorithmOutput.WeakReference(vtkAlgorithm_GetOutputPort(this.NativePointer));", text);
        Assert.Contains("private static extern nint vtkAlgorithm_GetOutputPort(nint self);", text);
    }

    [Fact]
    public void CSharpEmitter_EmitsIntParameters()
    {
        var emitter = new CSharpBindingEmitter();

        var text = emitter.Emit("VtkSharp", "vtkWindow", "vtkObject", hasStaticNew: false,
        [
            new WhitelistFunction
            {
                Name = "SetSize",
                CppSignature = "void SetSize(int width, int height)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters =
                [
                    new WhitelistParameter { Type = "int", Name = "width" },
                    new WhitelistParameter { Type = "int", Name = "height" },
                ],
            },
        ]);

        Assert.Contains("public new void SetSize(int width, int height)", text);
        Assert.DoesNotContain("public new static vtkWindow New()", text);
        Assert.DoesNotContain("vtkWindow_New", text);
        Assert.Contains("vtkWindow_SetSize(this.NativePointer, width, height);", text);
        Assert.Contains("private static extern void vtkWindow_SetSize(nint self, int width, int height);", text);
    }

    [Fact]
    public void CppEmitter_EmitsFunctionExports()
    {
        var emitter = new CppExportEmitter();

        var text = emitter.Emit("vtkAlgorithm", ["vtkAlgorithmOutput"], hasStaticNew: true,
        [
            new WhitelistFunction
            {
                Name = "SetInputConnection",
                CppSignature = "void SetInputConnection(vtkAlgorithmOutput* input)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters = [new WhitelistParameter { Type = "vtkAlgorithmOutput*", Name = "input" }],
            },
            new WhitelistFunction
            {
                Name = "GetOutputPort",
                CppSignature = "vtkAlgorithmOutput* GetOutputPort()",
                Return = new WhitelistReturn { Type = "vtkAlgorithmOutput*" },
                Parameters = [],
            },
        ]);

        Assert.Contains("VTKSHARP_API void vtkAlgorithm_SetInputConnection(vtkAlgorithm* self, vtkAlgorithmOutput* input)", text);
        Assert.Contains("self->SetInputConnection(input);", text);
        Assert.Contains("VTKSHARP_API vtkAlgorithmOutput* vtkAlgorithm_GetOutputPort(vtkAlgorithm* self)", text);
        Assert.Contains("return self->GetOutputPort();", text);
    }

    [Fact]
    public void CppEmitter_SkipsNewWhenClassHasNoStaticNew()
    {
        var emitter = new CppExportEmitter();

        var text = emitter.Emit("vtkWindow", [], hasStaticNew: false,
        [
            new WhitelistFunction
            {
                Name = "Render",
                CppSignature = "void Render()",
                Return = new WhitelistReturn { Type = "void" },
                Parameters = [],
            },
        ]);

        Assert.DoesNotContain("vtkWindow_New", text);
        Assert.Contains("VTKSHARP_API void vtkWindow_Render(vtkWindow* self)", text);
    }

    [Fact]
    public void CppEmitter_EmitsDistinctIncludes()
    {
        var emitter = new CppExportEmitter();

        var text = emitter.Emit("vtkAlgorithm", ["vtkAlgorithmOutput", "vtkAlgorithmOutput"], hasStaticNew: true,
        [
            new WhitelistFunction
            {
                Name = "GetOutputPort",
                CppSignature = "vtkAlgorithmOutput* GetOutputPort()",
                Return = new WhitelistReturn { Type = "vtkAlgorithmOutput*" },
                Parameters = [],
            },
        ]);

        Assert.Equal(1, CountOccurrences(text, "#include <vtkAlgorithmOutput.h>"));
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var startIndex = 0;
        while (true)
        {
            var index = text.IndexOf(value, startIndex, StringComparison.Ordinal);
            if (index < 0)
                return count;

            count++;
            startIndex = index + value.Length;
        }
    }
}
