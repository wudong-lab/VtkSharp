using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

public sealed class WhitelistValidatorTests
{
    [Fact]
    public void Validate_SucceedsWhenFunctionSignatureMatches()
    {
        var document = CreateDocument("void", "vtkAlgorithmOutput*");
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkAlgorithm"] = new(
                "vtkAlgorithm",
                [
                    new InspectedFunction(
                        "SetInputConnection",
                        "void SetInputConnection(vtkAlgorithmOutput* input)",
                        "void",
                        [new InspectedParameter("vtkAlgorithmOutput*", "input")],
                        IsSupported: true),
                ]),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.True(result.Success);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Validate_ReportsDiagnosticWhenFunctionSignatureDoesNotMatch()
    {
        var document = CreateDocument("void", "vtkAlgorithmOutput*");
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkAlgorithm"] = new(
                "vtkAlgorithm",
                [
                    new InspectedFunction(
                        "SetInputConnection",
                        "void SetInputConnection(int input)",
                        "void",
                        [new InspectedParameter("int", "input")],
                        IsSupported: true),
                ]),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.False(result.Success);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Message == "Function 'vtkAlgorithm.SetInputConnection' was not found.");
    }

    private static WhitelistDocument CreateDocument(string returnType, string parameterType)
        => new()
        {
            Module = "vtkCommonExecutionModel",
            Classes =
            [
                new WhitelistClass
                {
                    Name = "vtkAlgorithm",
                    Header = "vtkAlgorithm.h",
                    Functions =
                    [
                        new WhitelistFunction
                        {
                            Name = "SetInputConnection",
                            CppSignature = "void SetInputConnection(vtkAlgorithmOutput* input)",
                            Return = new WhitelistReturn { Type = returnType },
                            Parameters = [new WhitelistParameter { Type = parameterType, Name = "input" }],
                        },
                    ],
                },
            ],
        };

    [Theory]
    [InlineData("void")]
    [InlineData("char")]
    [InlineData("int")]
    [InlineData("unsigned int")]
    [InlineData("long long")]
    [InlineData("unsigned long long")]
    [InlineData("double")]
    [InlineData("float")]
    [InlineData("bool")]
    [InlineData("vtkTypeBool")]
    [InlineData("vtkIdType")]
    [InlineData("const char*")]
    [InlineData("char*")]
    [InlineData("void*")]
    [InlineData("vtkMapper*")]
    [InlineData("const vtkMapper*")]
    [InlineData("HWND")]
    [InlineData("HDC")]
    [InlineData("HGLRC")]
    public void Validate_AcceptsKnownScalarAndPointerTypes(string type)
    {
        var document = CreateDocument("void", type);
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkAlgorithm"] = new("vtkAlgorithm", [
                new InspectedFunction("SetInputConnection", "", "void", [new InspectedParameter(type, "input")], IsSupported: true),
            ]),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.DoesNotContain(result.Diagnostics, d => d.Message.Contains("unsupported type"));
    }

    [Fact]
    public void Validate_AcceptsFixedArray()
    {
        var type = "const double[3]";
        var document = CreateDocument("void", type);
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkAlgorithm"] = new("vtkAlgorithm", [
                new InspectedFunction("SetInputConnection", "", "void", [new InspectedParameter(type, "input")], IsSupported: true),
            ]),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.DoesNotContain(result.Diagnostics, d => d.Message.Contains("unsupported"));
    }

    [Fact]
    public void Validate_ReportsUnsupportedFixedArrayElementType()
    {
        var type = "const long long[3]";
        var document = CreateDocument("void", type);
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkAlgorithm"] = new("vtkAlgorithm", [
                new InspectedFunction("SetInputConnection", "", "void", [new InspectedParameter(type, "input")], IsSupported: true),
            ]),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.Contains(result.Diagnostics, d => d.Message.Contains("unsupported") && d.Message.Contains(type));
    }

    [Fact]
    public void Validate_ReportsUnsupportedType()
    {
        var type = "unsigned long";
        var document = CreateDocument("void", type);
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkAlgorithm"] = new("vtkAlgorithm", [
                new InspectedFunction("SetInputConnection", "", "void", [new InspectedParameter(type, "input")], IsSupported: true),
            ]),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.Contains(result.Diagnostics, d => d.Message.Contains("unsupported type"));
    }

    [Fact]
    public void Validate_ReportsNonPointerVtkClassName()
    {
        var type = "vtkMapper";
        var document = CreateDocument("void", type);
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkAlgorithm"] = new("vtkAlgorithm", [
                new InspectedFunction("SetInputConnection", "", "void", [new InspectedParameter(type, "input")], IsSupported: true),
            ]),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.Contains(result.Diagnostics, d => d.Message.Contains("without pointer"));
    }

    [Fact]
    public void Validate_ReportsPrimitivePointerWithoutMetadata()
    {
        var type = "double*";
        var document = CreateDocument("void", type);
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkAlgorithm"] = new("vtkAlgorithm", [
                new InspectedFunction("SetInputConnection", "", "void", [new InspectedParameter(type, "input")], IsSupported: true),
            ]),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.Contains(result.Diagnostics, d => d.Message.Contains("direction") && d.Message.Contains("length"));
    }

    [Fact]
    public void Validate_AcceptsPrimitivePointerWithMetadata()
    {
        var type = "double*";
        var document = new WhitelistDocument
        {
            Module = "vtkCommonExecutionModel",
            Classes =
            [
                new WhitelistClass
                {
                    Name = "vtkAlgorithm",
                    Header = "vtkAlgorithm.h",
                    Functions =
                    [
                        new WhitelistFunction
                        {
                            Name = "SetInputConnection",
                            CppSignature = "void SetInputConnection(double* input)",
                            Return = new WhitelistReturn { Type = "void" },
                            Parameters = [new WhitelistParameter { Type = type, Name = "input", Direction = "in", Length = new WhitelistLength { Kind = "fixed", Value = 3 } }],
                        },
                    ],
                },
            ],
        };
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkAlgorithm"] = new("vtkAlgorithm", [
                new InspectedFunction("SetInputConnection", "", "void", [new InspectedParameter(type, "input")], IsSupported: true),
            ]),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.DoesNotContain(result.Diagnostics, d => d.Message.Contains("direction") || d.Message.Contains("unsupported"));
    }

    [Fact]
    public void Validate_ChecksReturnType()
    {
        var type = "unsigned long";
        var document = new WhitelistDocument
        {
            Module = "vtkCommonExecutionModel",
            Classes =
            [
                new WhitelistClass
                {
                    Name = "vtkAlgorithm",
                    Header = "vtkAlgorithm.h",
                    Functions =
                    [
                        new WhitelistFunction
                        {
                            Name = "SetInputConnection",
                            CppSignature = "unsigned long SetInputConnection()",
                            Return = new WhitelistReturn { Type = type },
                            Parameters = [],
                        },
                    ],
                },
            ],
        };
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkAlgorithm"] = new("vtkAlgorithm", [
                new InspectedFunction("SetInputConnection", "", "void", [], IsSupported: true),
            ]),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.Contains(result.Diagnostics, d => d.Message.Contains("return") && d.Message.Contains(type));
    }
}
