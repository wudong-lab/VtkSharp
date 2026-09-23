using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;
using VtkSharp.Generator.Core.Vtk;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class WhitelistValidatorTests
{
    [TestMethod]
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

        Assert.IsTrue(result.Success);
        Assert.IsEmpty(result.Diagnostics);
    }

    [TestMethod]
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

        Assert.IsFalse(result.Success);
        Assert.Contains(diagnostic => diagnostic.Message == "Function 'vtkAlgorithm.SetInputConnection' was not found.", result.Diagnostics);
    }

    [TestMethod]
    public void Validate_RejectsInheritedFunctionOnDerivedClass()
    {
        var document = new WhitelistDocument
        {
            Module = "vtkFiltersCore",
            Classes =
            [
                new WhitelistClass
                {
                    Name = "vtkDerived",
                    Header = "vtkDerived.h",
                    Functions =
                    [
                        new WhitelistFunction
                        {
                            Name = "Update",
                            CppSignature = "void Update()",
                            Return = new WhitelistReturn { Type = "void" },
                            Parameters = [],
                        },
                    ],
                },
            ],
        };
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkDerived"] = new("vtkDerived", []),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.IsFalse(result.Success);
        Assert.Contains(diagnostic => diagnostic.Message == "Function 'vtkDerived.Update' was not found.", result.Diagnostics);
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

    [TestMethod]
    [DataRow("void")]
    [DataRow("char")]
    [DataRow("int")]
    [DataRow("unsigned int")]
    [DataRow("unsigned long")]
    [DataRow("long long")]
    [DataRow("unsigned long long")]
    [DataRow("double")]
    [DataRow("float")]
    [DataRow("bool")]
    [DataRow("vtkTypeBool")]
    [DataRow("vtkTypeUInt32")]
    [DataRow("vtkIdType")]
    [DataRow("const char*")]
    [DataRow("char*")]
    [DataRow("void*")]
    [DataRow("vtkMapper*")]
    [DataRow("const vtkMapper*")]
    [DataRow("HWND")]
    [DataRow("HDC")]
    [DataRow("HGLRC")]
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

        Assert.DoesNotContain(d => d.Message.Contains("unsupported type"), result.Diagnostics);
    }

    [TestMethod]
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

        Assert.DoesNotContain(d => d.Message.Contains("unsupported"), result.Diagnostics);
    }

    [TestMethod]
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

        Assert.Contains(d => d.Message.Contains("unsupported") && d.Message.Contains(type), result.Diagnostics);
    }

    [TestMethod]
    public void Validate_ReportsUnsupportedType()
    {
        var type = "unsigned short";
        var document = CreateDocument("void", type);
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkAlgorithm"] = new("vtkAlgorithm", [
                new InspectedFunction("SetInputConnection", "", "void", [new InspectedParameter(type, "input")], IsSupported: true),
            ]),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.Contains(d => d.Message.Contains("unsupported type"), result.Diagnostics);
    }

    [TestMethod]
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

        Assert.Contains(d => d.Message.Contains("without pointer"), result.Diagnostics);
    }

    [TestMethod]
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

        Assert.Contains(d => d.Message.Contains("direction") && d.Message.Contains("length"), result.Diagnostics);
    }

    [TestMethod]
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

        Assert.DoesNotContain(d => d.Message.Contains("direction") || d.Message.Contains("unsupported"), result.Diagnostics);
    }

    [TestMethod]
    public void Validate_AcceptsConstVtkIdTypePointerWithMetadata()
    {
        var type = "const vtkIdType*";
        var document = new WhitelistDocument
        {
            Module = "vtkCommonDataModel",
            Classes =
            [
                new WhitelistClass
                {
                    Name = "vtkCellArray",
                    Header = "vtkCellArray.h",
                    Functions =
                    [
                        new WhitelistFunction
                        {
                            Name = "InsertNextCell",
                            CppSignature = "vtkIdType InsertNextCell(vtkIdType npts, vtkIdType const * pts)",
                            Return = new WhitelistReturn { Type = "vtkIdType" },
                            Parameters =
                            [
                                new WhitelistParameter { Type = "vtkIdType", Name = "npts" },
                                new WhitelistParameter { Type = type, Name = "pts", Direction = "in", Length = new WhitelistLength { Kind = "parameter", Name = "npts" } },
                            ],
                        },
                    ],
                },
            ],
        };
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkCellArray"] = new("vtkCellArray", [
                new InspectedFunction("InsertNextCell", "", "vtkIdType", [new InspectedParameter("vtkIdType", "npts"), new InspectedParameter(type, "pts")], IsSupported: true),
            ]),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.DoesNotContain(d => d.Message.Contains("direction") || d.Message.Contains("unsupported"), result.Diagnostics);
    }

    [TestMethod]
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

        Assert.Contains(d => d.Message.Contains("Function 'vtkAlgorithm.SetInputConnection' was not found."), result.Diagnostics);
        Assert.DoesNotContain(d => d.Message.Contains("return") && d.Message.Contains(type), result.Diagnostics);
    }

    [TestMethod]
    public void Validate_ReportsModuleMismatch()
    {
        var document = new WhitelistDocument
        {
            Module = "vtkCommonCore",
            Classes =
            [
                new WhitelistClass
                {
                    Name = "vtkActor",
                    Header = "vtkActor.h",
                    Functions = [],
                },
            ],
        };
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkActor"] = new("vtkActor", []),
        };
        var hierarchy = new Dictionary<string, VtkHierarchyEntry>(StringComparer.Ordinal)
        {
            ["vtkActor"] = new("vtkActor", "vtkProp3D", "vtkActor.h", "vtkRenderingCore"),
        };
        var resolver = new VtkHierarchyResolver(hierarchy);
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses, resolver);

        Assert.Contains(d => d.Message.Contains("belongs to module 'vtkRenderingCore'"), result.Diagnostics);
    }

    [TestMethod]
    public void Validate_ReportsHeaderMismatch()
    {
        var document = new WhitelistDocument
        {
            Module = "vtkRenderingCore",
            Classes =
            [
                new WhitelistClass
                {
                    Name = "vtkActor",
                    Header = "vtkFooBar.h",
                    Functions = [],
                },
            ],
        };
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkActor"] = new("vtkActor", []),
        };
        var hierarchy = new Dictionary<string, VtkHierarchyEntry>(StringComparer.Ordinal)
        {
            ["vtkActor"] = new("vtkActor", "vtkProp3D", "vtkActor.h", "vtkRenderingCore"),
        };
        var resolver = new VtkHierarchyResolver(hierarchy);
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses, resolver);

        Assert.Contains(d => d.Message.Contains("header") && d.Message.Contains("vtkFooBar.h"), result.Diagnostics);
    }

    [TestMethod]
    public void Validate_NoModuleDiagnosticsWithoutResolver()
    {
        // When no hierarchy resolver is passed, module/header checks are skipped.
        var document = new WhitelistDocument
        {
            Module = "vtkCommonCore",
            Classes =
            [
                new WhitelistClass
                {
                    Name = "vtkActor",
                    Header = "vtkFooBar.h",
                    Functions =
                    [
                        new WhitelistFunction
                        {
                            Name = "SetMapper",
                            CppSignature = "void SetMapper(vtkMapper* mapper)",
                            Return = new WhitelistReturn { Type = "void" },
                            Parameters = [new WhitelistParameter { Type = "vtkMapper*", Name = "mapper" }],
                        },
                    ],
                },
            ],
        };
        var inspectedClasses = new Dictionary<string, InspectedClass>
        {
            ["vtkActor"] = new("vtkActor", [
                new InspectedFunction("SetMapper", "", "void", [new InspectedParameter("vtkMapper*", "mapper")], IsSupported: true),
            ]),
        };
        var validator = new WhitelistValidator();

        var result = validator.Validate(document, inspectedClasses);

        Assert.DoesNotContain(d => d.Message.Contains("module") || d.Message.Contains("header"), result.Diagnostics);
    }
}
