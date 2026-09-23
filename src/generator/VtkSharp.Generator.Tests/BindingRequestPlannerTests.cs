using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Vtk;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class BindingRequestPlannerTests
{
    [TestMethod]
    public void Build_HeaderFailureDoesNotDiscardOtherRequests()
    {
        var plan = BindingRequestPlanner.Build(new BindingRequestDocument
        {
            Requests = [new() { Class = "vtkBase", ClassOnly = true }, new() { Class = "vtkDerived", ClassOnly = true }],
        }, [], Hierarchy(), [], name => name == "vtkBase"
            ? throw new InvalidOperationException("Malformed header") : new InspectedClass(name, []));
        Assert.AreSequenceEqual(["inspection-failed", "ready"], plan.Diagnostics.Select(item => item.Status));
        Assert.AreEqual("vtkDerived", Enumerable.Single(plan.Candidate.Requirements).Class);
    }

    [TestMethod]
    public void Build_AlreadyExportedOverloadSetDoesNotRequireSelection()
    {
        var functions = new[] { Function("Set", "int"), Function("Set", "double") };
        var formal = new WhitelistDocument
        {
            Module = "vtkTest", Classes = [new() { Name = "vtkBase", Header = "vtkBase.h", Functions = functions.Select(CandidateWhitelistService.ToWhitelistFunction).ToList() }],
        };
        var plan = Plan(new BindingRequest { Class = "vtkBase", Methods = ["Set"] }, functions, formal: [formal]);
        Assert.IsFalse(plan.HasUnresolved);
        foreach (var item in plan.Diagnostics) Assert.AreEqual("already-exported", item.Status);
        Assert.IsEmpty(plan.Candidate.Requirements);
    }

    [TestMethod]
    public void Build_ResolvesBaseAndKeepsRequestedReceiverWithoutExportingOtherMethods()
    {
        var plan = Plan(new BindingRequest { Class = "vtkDerived", Methods = ["Update"] });
        Assert.IsFalse(plan.HasUnresolved);
        Assert.AreEqual("vtkBase", Enumerable.Single(plan.Diagnostics).DeclaringClass);
        Assert.IsEmpty(plan.Candidate.Requirements.Single(item => item.Class == "vtkDerived").Functions);
        Assert.AreEqual("Update", Enumerable.Single(plan.Candidate.Requirements.Single(item => item.Class == "vtkBase").Functions).Name);
    }

    [TestMethod]
    public void Build_AmbiguousNameRequiresExplicitSignature()
    {
        var functions = new[] { Function("Set", "int"), Function("Set", "double") };
        var ambiguous = Plan(new BindingRequest { Class = "vtkBase", Methods = ["Set"] }, functions);
        var diagnostic = Enumerable.Single(ambiguous.Diagnostics);
        Assert.AreEqual("ambiguous", diagnostic.Status);
        Assert.IsEmpty(ambiguous.Candidate.Requirements);
        var exact = Plan(new BindingRequest { Class = "vtkBase", Signatures = [diagnostic.Signatures![1]] }, functions);
        Assert.IsFalse(exact.HasUnresolved);
        Assert.AreEqual("double", Enumerable.Single(Enumerable.Single(exact.Candidate.Requirements).Functions).Parameters[0].Type);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Build_DoesNotSelectHiddenBaseOverload(bool publicDeclaration)
    {
        var derived = publicDeclaration ? new[] { Function("Set", "int") } : [];
        var plan = Plan(new BindingRequest { Class = "vtkDerived", Signatures = ["vtkBase::void Set(double)"] },
            [Function("Set", "double")], derived, ["Set"]);
        Assert.IsTrue(plan.HasUnresolved);
        Assert.AreEqual("unsupported", Enumerable.Single(plan.Diagnostics).Status);
        Assert.IsEmpty(plan.Candidate.Requirements);
    }

    [TestMethod]
    public void Build_ReportsMetadataAndUnsupportedSeparatelyFromMissing()
    {
        var plan = Plan(new BindingRequest { Class = "vtkBase", Methods = ["Pointer", "Reference", "Missing"] },
            [Function("Pointer", "double*"), Function("Reference", "int&")]);
        Assert.AreSequenceEqual(["needs-metadata", "unsupported", "not-found"], plan.Diagnostics.Select(item => item.Status));
        Assert.IsEmpty(plan.Candidate.Requirements);
    }

    [TestMethod]
    public void Build_AlreadyExportedPointerDoesNotNeedMetadataAgain()
    {
        var function = CandidateWhitelistService.ToWhitelistFunction(Function("Pointer", "double*"));
        var formal = new WhitelistDocument
        {
            Module = "vtkTest", Classes = [new WhitelistClass { Name = "vtkBase", Header = "vtkBase.h", Functions = [function] }],
        };
        var plan = Plan(new BindingRequest { Class = "vtkDerived", Methods = ["Pointer"] }, [Function("Pointer", "double*")], formal: [formal]);
        Assert.AreEqual("already-exported", Enumerable.Single(plan.Diagnostics).Status);
        Assert.AreEqual("vtkDerived", Enumerable.Single(plan.Candidate.Requirements).Class);
    }

    [TestMethod]
    public void Build_DeduplicatesAndCachesInspectionWithinBatch()
    {
        var count = 0;
        var request = new BindingRequest { Class = "vtkBase", Methods = ["Update", "Update"] };
        var plan = BindingRequestPlanner.Build(new BindingRequestDocument { Requests = [request, request] }, [], Hierarchy(), [], _ =>
        {
            count++;
            return new InspectedClass("vtkBase", [Function("Update")]);
        });
        Assert.AreEqual(1, count);
        Enumerable.Single(Enumerable.Single(plan.Candidate.Requirements).Functions);
    }

    [TestMethod]
    public void Build_ClassOnlyDoesNotExportFunctions()
    {
        var plan = Plan(new BindingRequest { Class = "vtkBase", ClassOnly = true });
        Assert.IsEmpty(Enumerable.Single(plan.Candidate.Requirements).Functions);
        var invalid = Plan(new BindingRequest { Class = "vtkBase" });
        Assert.AreEqual("invalid-request", Enumerable.Single(invalid.Diagnostics).Status);
        Assert.IsEmpty(invalid.Candidate.Requirements);
    }

    [TestMethod]
    public void Build_MultipleInheritanceDoesNotGuessBase()
    {
        var plan = BindingRequestPlanner.Build(new BindingRequestDocument
        {
            Requests = [new BindingRequest { Class = "vtkDerived", Methods = ["Update"] }],
        }, [], Hierarchy(), [], name => new InspectedClass(name, [], HasMultipleBaseClasses: true));
        Assert.AreEqual("ambiguous", Enumerable.Single(plan.Diagnostics).Status);
    }

    private static BindingRequestPlan Plan(BindingRequest request, IReadOnlyList<InspectedFunction>? baseFunctions = null,
        IReadOnlyList<InspectedFunction>? derivedFunctions = null, IReadOnlyList<string>? declaredNames = null,
        IReadOnlyList<WhitelistDocument>? formal = null)
        => BindingRequestPlanner.Build(new BindingRequestDocument { Requests = [request] }, formal ?? [], Hierarchy(), ["vtkObject"],
            name => name == "vtkBase"
                ? new InspectedClass(name, baseFunctions ?? [Function("Update"), Function("Other")])
                : new InspectedClass(name, derivedFunctions ?? [], DeclaredMemberNames: declaredNames));

    internal static Dictionary<string, VtkHierarchyEntry> Hierarchy() => new(StringComparer.Ordinal)
    {
        ["vtkDerived"] = new("vtkDerived", "vtkBase", "vtkDerived.h", "vtkTest"),
        ["vtkBase"] = new("vtkBase", "vtkObject", "vtkBase.h", "vtkTest"),
    };

    private static InspectedFunction Function(string name, string? parameter = null)
        => new(name, $"void {name}({parameter})", "void", parameter is null ? [] : [new InspectedParameter(parameter, "value")], true);
}
