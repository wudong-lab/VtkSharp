using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Exporting;
using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Validation;
using VtkSharp.Generator.Core.Vtk;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

public sealed class EnumPropertyTests
{
    internal static InspectedClass Inspect(string body, string name = "vtkThing")
    {
        var directory = Path.Combine(Path.GetTempPath(), "vtksharp-enum-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, name + ".h"), body);
            return new VtkClassInspector().InspectHeader(directory, name + ".h", name);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void IntForwarders_ResolveMacroExpressionsAndKeepNativeSignatures()
    {
        var result = Inspect("""
            #define MODE_A (-3)
            #define MODE_B (MODE_A + 10)
            class vtkThing {
            public:
                void SetMode(int value);
                int GetMode();
                void SetModeToA() { this->SetMode(MODE_A); }
                void SetModeToB() { this->SetMode(MODE_B); }
            };
            """);
        Assert.Empty(result.EnumDiagnostics!);
        Assert.True(result.EnumProperties?.Count > 0, System.Text.Json.JsonSerializer.Serialize(result));
        var property = Assert.Single(result.EnumProperties!);
        Assert.Equal("Mode", property.Name);
        Assert.Equal(new[] { -3, 7 }, property.Values.Select(v => v.Value));
        Assert.Equal("MODE_A", property.Values[0].NativeExpression);
        Assert.Equal("int", result.Functions.Single(f => f.Name == "GetMode").ReturnType);
    }

    [Fact]
    public void NativeEnum_WithIntUnderlyingType_IsSupported()
    {
        var result = Inspect("""
            class vtkThing {
            public:
                enum class Choice : int { A = -1, B = 5, Alias = 5 };
                void SetMode(Choice value);
                Choice GetMode();
            };
            """);
        Assert.True(result.EnumProperties?.Count > 0, System.Text.Json.JsonSerializer.Serialize(result));
        var property = Assert.Single(result.EnumProperties!);
        Assert.Equal(3, property.Values.Count);
        Assert.All(result.Functions, f => Assert.Equal("ready", FunctionEligibility.Evaluate(f).Status));
    }

    [Theory]
    [InlineData("void SetModeToA() { SetMode(1); Update(); }", "")]
    [InlineData("void SetModeToA() { SetMode(1); }", "void SetMode(double value);")]
    [InlineData("void SetModeToA() { SetMode(0xFFFFFFFFu); }", "")]
    public void UnsupportedGroup_DoesNotDisableOrdinaryFunctions(string helper, string extra)
    {
        var result = Inspect("class vtkThing { public: void Update(); void SetMode(int v); int GetMode(); void SetModeToB() { SetMode(2); } " + helper + extra + " };");
        Assert.Empty(result.EnumProperties!);
        Assert.NotEmpty(result.EnumDiagnostics!);
        Assert.All(result.Functions, f => Assert.Equal("ready", FunctionEligibility.Evaluate(f).Status));
    }

    [Fact]
    public void OverrideAndExtension_AreNotAutomaticallyConverted()
    {
        var result = Inspect("""
            class vtkBase { public: virtual void SetMode(int); int GetMode(); void SetModeToA() { SetMode(1); } };
            class vtkThing : public vtkBase {
            public:
                void SetMode(int) override;
                int GetMode();
                void SetModeToB() { SetMode(2); }
            };
            """);
        Assert.Empty(result.EnumProperties!);
        Assert.Contains(result.EnumDiagnostics!, d => d.Contains("inherited"));
        Assert.All(result.Functions, f => Assert.Equal("ready", FunctionEligibility.Evaluate(f).Status));
    }

    private const string IntHeader = """
        #define FIRST (-1)
        #define SECOND (FIRST + 4)
        class vtkThing {
        public:
            void SetMode(int v);
            int GetMode();
            void SetModeToFirst() { SetMode(FIRST); }
            void SetModeToSecond() { SetMode(SECOND); }
        };
        """;

    private static Dictionary<string, VtkHierarchyEntry> Hierarchy => new()
    {
        ["vtkThing"] = new("vtkThing", "", "vtkThing.h", "vtkTest"),
    };

    [Fact]
    public void PlanningAlreadyExportedHelper_AddsWholeGroupAndEnumContract()
    {
        var inspected = Inspect(IntHeader);
        var helper = CandidateWhitelistService.ToWhitelistFunction(inspected.Functions.Single(f => f.Name == "SetModeToFirst"));
        List<WhitelistDocument> formal = [new() { Module = "vtkTest", Classes = [new() { Name = "vtkThing", Header = "vtkThing.h", Functions = [helper] }] }];
        var plan = BindingRequestPlanner.Build(new() { Requests = [new() { Class = "vtkThing", Methods = [helper.Name] }] },
            formal, Hierarchy, [], _ => inspected);
        Assert.False(plan.HasUnresolved);
        var requirement = Assert.Single(plan.Candidate.Requirements);
        Assert.Equal(4, requirement.Functions.Count);
        Assert.Single(requirement.EnumProperties!);
        var merge = CandidateMergePlan.Build(formal, plan.Candidate, Hierarchy, []);
        Assert.Empty(merge.Conflicts);
        Assert.Equal(3, merge.Added.Count);
        Assert.Equal("vtkThing.Mode", Assert.Single(merge.AddedEnums));
        Assert.Null(formal[0].Classes[0].EnumProperties);
        Assert.Single(formal[0].Classes[0].Functions);
        var again = CandidateMergePlan.Build(merge.Documents, plan.Candidate, Hierarchy, []);
        Assert.Empty(again.AddedEnums);
        Assert.Empty(again.Added);
    }

    [Fact]
    public void ContractChangeOrIncompleteGroup_IsRejected()
    {
        var inspected = Inspect(IntHeader);
        var requirement = new CandidateRequirement { Class = "vtkThing", Module = "vtkTest", Header = "vtkThing.h" };
        EnumCandidateExpansion.Expand(requirement, inspected, ["SetMode"]);
        var whitelistClass = new WhitelistClass { Name = "vtkThing", Header = "vtkThing.h", Functions = requirement.Functions, EnumProperties = requirement.EnumProperties };
        var document = new WhitelistDocument { Module = "vtkTest", Classes = [whitelistClass] };
        var validator = new WhitelistValidator();
        Assert.Empty(validator.Validate(document, new Dictionary<string, InspectedClass> { ["vtkThing"] = inspected }).Diagnostics);
        var changed = inspected with { EnumProperties = [] };
        Assert.Contains(validator.Validate(document, new Dictionary<string, InspectedClass> { ["vtkThing"] = changed }).Diagnostics,
            d => d.Message.Contains("refusing to fall back"));
        var original = requirement.EnumProperties![0];
        var candidate = new CandidateDocument { Requirements = [requirement with
        {
            EnumProperties = [original with { Values = original.Values.Select(v => v with { Value = v.Value + 1 }).ToList() }],
        }] };
        Assert.NotEmpty(CandidateMergePlan.Build([document], candidate, Hierarchy, []).Conflicts);
        whitelistClass.Functions.RemoveAt(0);
        Assert.Contains(validator.Validate(document, new Dictionary<string, InspectedClass> { ["vtkThing"] = inspected }).Diagnostics,
            d => d.Message.Contains("requires exactly one"));
    }

    [Fact]
    public void EmitIntEnum_PreservesNativeExportsAndUsesTypedPublicMethods()
    {
        var inspected = Inspect(IntHeader);
        var functions = inspected.Functions.Select(CandidateWhitelistService.ToWhitelistFunction).ToList();
        var managed = new CSharpBindingEmitter().Emit("VtkSharp", "vtkThing", "vtkObject", false, functions, inspected,
            enumProperties: inspected.EnumProperties);
        Assert.Contains("public enum Mode : int", managed);
        Assert.Contains("public new Mode GetMode()", managed);
        Assert.Contains("public new void SetMode(Mode v)", managed);
        Assert.Contains("private static extern int vtkThing_GetMode(nint self)", managed);
        Assert.Contains("Native: FIRST", managed);
        Assert.DoesNotContain("public new void SetMode(int", managed);
        var emitter = new CppExportEmitter();
        Assert.Equal(emitter.Emit("vtkThing", [], false, functions), emitter.Emit("vtkThing", [], false, functions, inspected.EnumProperties));
    }

    [Fact]
    public void EmitNativeEnum_UsesIntAbiAndExplicitNativeCasts()
    {
        var inspected = Inspect("class vtkThing { public: enum class Choice : int { A=-1, B=2 }; Choice GetMode(); void SetMode(Choice value); };");
        var functions = inspected.Functions.Select(CandidateWhitelistService.ToWhitelistFunction).ToList();
        var managed = new CSharpBindingEmitter().Emit("VtkSharp", "vtkThing", "vtkObject", false, functions, inspected,
            enumProperties: inspected.EnumProperties);
        Assert.Contains("SetMode(Mode value)", managed);
        var native = new CppExportEmitter().Emit("vtkThing", [], false, functions, inspected.EnumProperties);
        Assert.Contains("static_cast<vtkThing::Choice>(value)", native);
        Assert.Contains("static_cast<int>(self->GetMode())", native);
    }

    [Fact]
    public void GuiSelection_ExpandsAssociatedFunctions()
    {
        var inspected = Inspect(IntHeader);
        var property = Assert.Single(inspected.EnumProperties!);
        var selected = new ExportFunctionCandidate("vtkThing::void SetModeToFirst()", "vtkThing", "vtkThing", "vtkTest", "vtkThing.h",
            "vtkThing::void SetModeToFirst()", "SetModeToFirst", "void", [], ExportStatus.AvailableToAdd, true, null, property, inspected.Functions);
        var plan = new ExportInventoryService().CreatePlan([selected]);
        Assert.Equal(4, plan.Functions.Count);
        Assert.Contains(plan.Diagnostics, d => d.Contains("Enum group"));
    }

    [Theory]
    [InlineData("#define SECOND 4294967295u")]
    [InlineData("#define SECOND runtimeValue()\nint runtimeValue();")]
    public void UnresolvableOrOutOfRangeConstant_FallsBackWithoutChangingSignatures(string definition)
    {
        var result = Inspect(IntHeader.Replace("#define SECOND (FIRST + 4)", definition));
        Assert.Empty(result.EnumProperties!);
        Assert.NotEmpty(result.EnumDiagnostics!);
        Assert.All(result.Functions, f => Assert.Equal("ready", FunctionEligibility.Evaluate(f).Status));
    }

    [Fact]
    public void SingleDefaultSetter_DoesNotEstablishEnumSemantics()
    {
        var result = Inspect("class vtkThing { public: int GetCount(); void SetCount(int); void SetCountToDefault() { SetCount(1); } };");
        Assert.Empty(result.EnumProperties!);
        Assert.Contains(result.EnumDiagnostics!, d => d.Contains("lone reset/default"));
    }

    [Theory]
    [InlineData("Mode", "2D", "3D")]
    [InlineData("2DMode", "First", "Second")]
    public void InvalidManagedIdentifier_LeavesOrdinaryFunctionsExportable(string property, string first, string second)
    {
        var inspected = Inspect($"class vtkThing {{ public: int Get{property}(); void Set{property}(int); " +
            $"void Set{property}To{first}() {{ Set{property}(1); }} void Set{property}To{second}() {{ Set{property}(2); }} }};");
        Assert.Empty(inspected.EnumProperties!);
        Assert.Contains(inspected.EnumDiagnostics!, d => d.Contains("identifier"));
        Assert.All(inspected.Functions, f => Assert.Equal("ready", FunctionEligibility.Evaluate(f).Status));
    }

    [Fact]
    public void CSharpKeywords_AreEscapedWithoutRenaming()
    {
        var inspected = Inspect("class vtkThing { public: int Getevent(); void Setevent(int); " +
            "void SeteventTostring() { Setevent(1); } void SeteventToobject() { Setevent(2); } };");
        Assert.Single(inspected.EnumProperties!);
        var managed = new CSharpBindingEmitter().Emit("VtkSharp", "vtkThing", "vtkObject", false,
            inspected.Functions.Select(CandidateWhitelistService.ToWhitelistFunction).ToList(), inspected, enumProperties: inspected.EnumProperties);
        Assert.Contains("public enum @event", managed);
        Assert.Contains("@string = 1", managed);
        Assert.Contains("@object = 2", managed);
    }

    [Fact]
    public void EnumContract_RoundTripsThroughYamlAndAffectsFingerprint()
    {
        var inspected = Inspect(IntHeader);
        var requirement = new CandidateRequirement { Class = "vtkThing", Module = "vtkTest", Header = "vtkThing.h" };
        EnumCandidateExpansion.Expand(requirement, inspected, ["GetMode"]);
        var document = new WhitelistDocument { Module = "vtkTest", Classes = [new()
        {
            Name = "vtkThing", Header = "vtkThing.h", Functions = requirement.Functions, EnumProperties = requirement.EnumProperties,
        }] };
        var directory = Path.Combine(Path.GetTempPath(), "vtksharp-enum-yaml", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "vtkTest.yml");
            new WhitelistWriter().WriteFile(path, document);
            var restored = new WhitelistLoader().LoadFile(path);
            Assert.True(requirement.EnumProperties![0].SameContract(restored.Classes[0].EnumProperties![0]));
            var candidatePath = Path.Combine(directory, "candidate.yml");
            CandidateWhitelistService.WriteCandidate(candidatePath, new() { Requirements = [requirement] });
            var restoredCandidate = CandidateWhitelistService.LoadCandidateFile(candidatePath);
            Assert.True(requirement.EnumProperties[0].SameContract(restoredCandidate.Requirements[0].EnumProperties![0]));
            var ordinary = GenerationInputFingerprint.Compute("v1", "9.7", "VtkSharp", "native", "vtkTest", "vtkThing", "vtkThing.h", "vtkObject", "hash", requirement.Functions);
            var typed = GenerationInputFingerprint.Compute("v1", "9.7", "VtkSharp", "native", "vtkTest", "vtkThing", "vtkThing.h", "vtkObject", "hash", requirement.Functions, requirement.EnumProperties);
            Assert.NotEqual(ordinary, typed);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void ConstantEvaluation_UsesMacroStateAtTheMethodNotAtEndOfHeader()
    {
        var inspected = Inspect(IntHeader + "\n#undef FIRST\n#define FIRST 999\n");
        var property = Assert.Single(inspected.EnumProperties!);
        Assert.Equal(-1, property.Values.Single(v => v.Name == "First").Value);
        Assert.Equal(3, property.Values.Single(v => v.Name == "Second").Value);
    }
}
