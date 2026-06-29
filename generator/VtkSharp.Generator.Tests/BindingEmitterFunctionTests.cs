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

    [Fact]
    public void Emitters_UseExportNameGeneratorForOverloads()
    {
        var functions = new[]
        {
            new WhitelistFunction
            {
                Name = "SetPosition",
                CppSignature = "void SetPosition(double x, double y, double z)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters =
                [
                    new WhitelistParameter { Type = "double", Name = "x" },
                    new WhitelistParameter { Type = "double", Name = "y" },
                    new WhitelistParameter { Type = "double", Name = "z" },
                ],
            },
            new WhitelistFunction
            {
                Name = "SetPosition",
                CppSignature = "void SetPosition(const double[3] position)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters = [new WhitelistParameter { Type = "const double[3]", Name = "position" }],
            },
        };

        var csharp = new CSharpBindingEmitter().Emit("VtkSharp", "vtkActor", "vtkProp3D", hasStaticNew: false, functions);
        var cpp = new CppExportEmitter().Emit("vtkActor", [], hasStaticNew: false, functions);

        Assert.Contains("vtkActor_SetPosition_double_double_double(this.NativePointer, x, y, z);", csharp);
        Assert.Contains("vtkActor_SetPosition_doubleConstArray3(this.NativePointer, positionPtr);", csharp);
        Assert.Contains("VTKSHARP_API void vtkActor_SetPosition_double_double_double(vtkActor* self, double x, double y, double z)", cpp);
        Assert.Contains("VTKSHARP_API void vtkActor_SetPosition_doubleConstArray3(vtkActor* self, const double* position)", cpp);
    }

    [Fact]
    public void CSharpEmitter_EmitsExpandedScalarStringPointerAndArrayMappings()
    {
        var text = new CSharpBindingEmitter().Emit("VtkSharp", "vtkThing", "vtkObject", hasStaticNew: false,
        [
            new WhitelistFunction
            {
                Name = "SetVisible",
                CppSignature = "void SetVisible(bool value)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters = [new WhitelistParameter { Type = "bool", Name = "value" }],
            },
            new WhitelistFunction
            {
                Name = "HasViewProp",
                CppSignature = "vtkTypeBool HasViewProp()",
                Return = new WhitelistReturn { Type = "vtkTypeBool" },
                Parameters = [],
            },
            new WhitelistFunction
            {
                Name = "GetId",
                CppSignature = "vtkIdType GetId()",
                Return = new WhitelistReturn { Type = "vtkIdType" },
                Parameters = [],
            },
            new WhitelistFunction
            {
                Name = "GetSeed",
                CppSignature = "vtkTypeUInt32 GetSeed()",
                Return = new WhitelistReturn { Type = "vtkTypeUInt32" },
                Parameters = [],
            },
            new WhitelistFunction
            {
                Name = "SetName",
                CppSignature = "void SetName(const char* name)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters = [new WhitelistParameter { Type = "const char*", Name = "name" }],
            },
            new WhitelistFunction
            {
                Name = "GetName",
                CppSignature = "const char* GetName()",
                Return = new WhitelistReturn { Type = "const char*" },
                Parameters = [],
            },
            new WhitelistFunction
            {
                Name = "GetKeyCode",
                CppSignature = "char GetKeyCode()",
                Return = new WhitelistReturn { Type = "char" },
                Parameters = [],
            },
            new WhitelistFunction
            {
                Name = "GetData",
                CppSignature = "void* GetData()",
                Return = new WhitelistReturn { Type = "void*" },
                Parameters = [],
            },
            new WhitelistFunction
            {
                Name = "SetOrigin",
                CppSignature = "void SetOrigin(const double[3] origin)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters = [new WhitelistParameter { Type = "const double[3]", Name = "origin" }],
            },
        ]);

        Assert.Contains("public new void SetVisible(bool value)", text);
        Assert.Contains("[MarshalAs(UnmanagedType.U1)] bool value", text);
        Assert.Contains("[return: MarshalAs(UnmanagedType.U4)]", text);
        Assert.Contains("public new bool HasViewProp()", text);
        Assert.Contains("public new long GetId()", text);
        Assert.Contains("public new uint GetSeed()", text);
        Assert.Contains("public new void SetName(string name)", text);
        Assert.Contains("[LibraryImport(InteropInfo.NativeLibraryName, StringMarshalling = StringMarshalling.Utf8)]", text);
        Assert.Contains("private static partial void vtkThing_SetName(nint self, string name)", text);
        Assert.Contains("[DllImport(InteropInfo.NativeLibraryName)]", text);
        Assert.Contains("private static extern void vtkThing_SetName(nint self, byte[] name)", text);
        Assert.Contains("VtkString.ToNullTerminatedUtf8(name)", text);
        Assert.Contains("public new string GetName()", text);
        Assert.Contains("return VtkString.FromUtf8Pointer(vtkThing_GetName(this.NativePointer))", text);
        Assert.Contains("public new char GetKeyCode()", text);
        Assert.Contains("return (char)vtkThing_GetKeyCode(this.NativePointer)", text);
        Assert.Contains("private static extern byte vtkThing_GetKeyCode(nint self);", text);
        Assert.Contains("#if NET10_0_OR_GREATER", text);
        Assert.Contains("#else", text);
        Assert.Contains("#endif", text);
        Assert.Contains("public new nint GetData()", text);
        Assert.Contains("public new void SetOrigin(ReadOnlySpan<double> origin)", text);
        Assert.Contains("fixed (double* originPtr = origin)", text);
    }

    [Fact]
    public void CppEmitter_EmitsExpandedScalarPointerAndArrayMappings()
    {
        var text = new CppExportEmitter().Emit("vtkThing", [], hasStaticNew: false,
        [
            new WhitelistFunction
            {
                Name = "SetOrigin",
                CppSignature = "void SetOrigin(const double[3] origin)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters = [new WhitelistParameter { Type = "const double[3]", Name = "origin" }],
            },
            new WhitelistFunction
            {
                Name = "GetData",
                CppSignature = "void* GetData()",
                Return = new WhitelistReturn { Type = "void*" },
                Parameters = [],
            },
            new WhitelistFunction
            {
                Name = "GetId",
                CppSignature = "vtkIdType GetId()",
                Return = new WhitelistReturn { Type = "vtkIdType" },
                Parameters = [],
            },
        ]);

        Assert.Contains("VTKSHARP_API void vtkThing_SetOrigin(vtkThing* self, const double* origin)", text);
        Assert.Contains("VTKSHARP_API void* vtkThing_GetData(vtkThing* self)", text);
        Assert.Contains("VTKSHARP_API vtkIdType vtkThing_GetId(vtkThing* self)", text);
    }

    [Fact]
    public void CppEmitter_EmitsVtkTypeUInt32Mapping()
    {
        var text = new CppExportEmitter().Emit("vtkFoo", [], hasStaticNew: false,
        [
            new WhitelistFunction
            {
                Name = "GetSeed",
                CppSignature = "vtkTypeUInt32 GetSeed()",
                Return = new WhitelistReturn { Type = "vtkTypeUInt32" },
                Parameters = [],
            },
        ]);

        Assert.Contains("VTKSHARP_API vtkTypeUInt32 vtkFoo_GetSeed(vtkFoo* self)", text);
    }

    [Fact]
    public void CSharpEmitter_EmitsReadOnlySpanForPointerWithInDirectionAndFixedLength()
    {
        var text = new CSharpBindingEmitter().Emit("VtkSharp", "vtkFoo", "vtkObject", hasStaticNew: false,
        [
            new WhitelistFunction
            {
                Name = "SetPosition",
                CppSignature = "void SetPosition(double* position)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters =
                [
                    new WhitelistParameter
                    {
                        Type = "double*",
                        Name = "position",
                        Direction = "in",
                        Length = new WhitelistLength { Kind = "fixed", Value = 3 },
                    },
                ],
            },
        ]);

        Assert.Contains("public new void SetPosition(ReadOnlySpan<double> position)", text);
        Assert.Contains("fixed (double* positionPtr = position)", text);
        Assert.Contains("vtkFoo_SetPosition(this.NativePointer, positionPtr);", text);
        Assert.Contains("private static extern void vtkFoo_SetPosition(nint self, double* position);", text);
    }

    [Fact]
    public void CSharpEmitter_EmitsSpanForPointerWithOutDirection()
    {
        var text = new CSharpBindingEmitter().Emit("VtkSharp", "vtkFoo", "vtkObject", hasStaticNew: false,
        [
            new WhitelistFunction
            {
                Name = "GetBounds",
                CppSignature = "void GetBounds(double* bounds)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters =
                [
                    new WhitelistParameter
                    {
                        Type = "double*",
                        Name = "bounds",
                        Direction = "out",
                        Length = new WhitelistLength { Kind = "fixed", Value = 6 },
                    },
                ],
            },
        ]);

        Assert.Contains("public new void GetBounds(Span<double> bounds)", text);
        Assert.Contains("fixed (double* boundsPtr = bounds)", text);
    }

    [Fact]
    public void CSharpEmitter_EmitsReadOnlySpanForPointerWithParameterLength()
    {
        var text = new CSharpBindingEmitter().Emit("VtkSharp", "vtkFoo", "vtkObject", hasStaticNew: false,
        [
            new WhitelistFunction
            {
                Name = "SetArray",
                CppSignature = "void SetArray(double* values, vtkIdType count)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters =
                [
                    new WhitelistParameter
                    {
                        Type = "double*",
                        Name = "values",
                        Direction = "in",
                        Length = new WhitelistLength { Kind = "parameter", Name = "count" },
                    },
                    new WhitelistParameter { Type = "vtkIdType", Name = "count" },
                ],
            },
        ]);

        Assert.Contains("public new void SetArray(ReadOnlySpan<double> values, long count)", text);
        Assert.Contains("fixed (double* valuesPtr = values)", text);
        Assert.Contains("vtkFoo_SetArray(this.NativePointer, valuesPtr, count);", text);
    }

    [Fact]
    public void CSharpEmitter_KeepsRawPointerForPointerWithoutMetadata()
    {
        var text = new CSharpBindingEmitter().Emit("VtkSharp", "vtkFoo", "vtkObject", hasStaticNew: false,
        [
            new WhitelistFunction
            {
                Name = "GetData",
                CppSignature = "double* GetData()",
                Return = new WhitelistReturn { Type = "double*" },
                Parameters = [],
            },
        ]);

        Assert.Contains("internal new double* GetData_Internal()", text);
    }

    [Fact]
    public void CppEmitter_EmitsPrimitivePointerType()
    {
        var text = new CppExportEmitter().Emit("vtkFoo", [], hasStaticNew: false,
        [
            new WhitelistFunction
            {
                Name = "SetPosition",
                CppSignature = "void SetPosition(double* position)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters =
                [
                    new WhitelistParameter
                    {
                        Type = "double*",
                        Name = "position",
                        Direction = "in",
                        Length = new WhitelistLength { Kind = "fixed", Value = 3 },
                    },
                ],
            },
        ]);

        Assert.Contains("VTKSHARP_API void vtkFoo_SetPosition(vtkFoo* self, double* position)", text);
        Assert.Contains("self->SetPosition(position);", text);
    }

    [Fact]
    public void CppEmitter_EmitsPrimitivePointerWithOtherParameters()
    {
        var text = new CppExportEmitter().Emit("vtkFoo", [], hasStaticNew: false,
        [
            new WhitelistFunction
            {
                Name = "SetArray",
                CppSignature = "void SetArray(double* values, vtkIdType count)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters =
                [
                    new WhitelistParameter
                    {
                        Type = "double*",
                        Name = "values",
                        Direction = "in",
                        Length = new WhitelistLength { Kind = "parameter", Name = "count" },
                    },
                    new WhitelistParameter { Type = "vtkIdType", Name = "count" },
                ],
            },
        ]);

        Assert.Contains("VTKSHARP_API void vtkFoo_SetArray(vtkFoo* self, double* values, vtkIdType count)", text);
    }

    [Fact]
    public void ExportNameGenerator_UsesDoublePtrSuffixForOverload()
    {
        var functions = new[]
        {
            new WhitelistFunction
            {
                Name = "SetPosition",
                CppSignature = "void SetPosition(double x, double y, double z)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters =
                [
                    new WhitelistParameter { Type = "double", Name = "x" },
                    new WhitelistParameter { Type = "double", Name = "y" },
                    new WhitelistParameter { Type = "double", Name = "z" },
                ],
            },
            new WhitelistFunction
            {
                Name = "SetPosition",
                CppSignature = "void SetPosition(double* position)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters =
                [
                    new WhitelistParameter
                    {
                        Type = "double*",
                        Name = "position",
                        Direction = "in",
                        Length = new WhitelistLength { Kind = "fixed", Value = 3 },
                    },
                ],
            },
        };

        var csharp = new CSharpBindingEmitter().Emit("VtkSharp", "vtkActor", "vtkProp3D", hasStaticNew: false, functions);

        Assert.Contains("vtkActor_SetPosition_double_double_double(this.NativePointer, x, y, z);", csharp);
        Assert.Contains("vtkActor_SetPosition_doublePtr(this.NativePointer, positionPtr);", csharp);
    }

    [Theory]
    [InlineData("HWND")]
    [InlineData("HDC")]
    [InlineData("HGLRC")]
    public void CSharpEmitter_MapsWin32HandlesToNint(string type)
    {
        var text = new CSharpBindingEmitter().Emit("VtkSharp", "vtkFoo", "vtkObject", hasStaticNew: false,
        [
            new WhitelistFunction
            {
                Name = "SetHandle",
                CppSignature = $"void SetHandle({type} h)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters = [new WhitelistParameter { Type = type, Name = "h" }],
            },
        ]);

        Assert.Contains($"public new void SetHandle(nint h)", text);
        Assert.Contains($"private static extern void vtkFoo_SetHandle(nint self, nint h);", text);
    }

    [Theory]
    [InlineData("HWND")]
    [InlineData("HDC")]
    [InlineData("HGLRC")]
    public void CppEmitter_MapsWin32HandlesToVoidPointer(string type)
    {
        var text = new CppExportEmitter().Emit("vtkFoo", [], hasStaticNew: false,
        [
            new WhitelistFunction
            {
                Name = "SetHandle",
                CppSignature = $"void SetHandle({type} h)",
                Return = new WhitelistReturn { Type = "void" },
                Parameters = [new WhitelistParameter { Type = type, Name = "h" }],
            },
        ]);

        Assert.Contains($"VTKSHARP_API void vtkFoo_SetHandle(vtkFoo* self, void* h)", text);
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
