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
}
