using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class CandidateMergePlanTests
{
    [TestMethod]
    [DataRow("vtkStdString")]
    [DataRow("vtkColor3d")]
    [DataRow("vtkIdType")]
    public void Build_DoesNotRequireWrappersForMappedValues(string type)
    {
        var plan = Build([], Candidate("vtkBase", Function("Get") with { Return = new() { Type = type } }));
        Assert.IsEmpty(plan.Conflicts);
        Assert.AreEqual("vtkBase", Enumerable.Single(plan.AddedClasses).Class);
    }

    [TestMethod]
    public void Build_PreviewsEmptyClassAndBaseChainWithoutChangingInput()
    {
        var candidate = Candidate("vtkDerived");
        var plan = Build([], candidate);
        Assert.IsEmpty(plan.Conflicts);
        Assert.AreSequenceEqual(["vtkBase", "vtkDerived"], plan.AddedClasses.Select(item => item.Class));
        Assert.Contains("base-of:vtkDerived", plan.AddedClasses[0].Reasons);
        Assert.Contains("explicit-request", plan.AddedClasses[1].Reasons);
        Assert.IsEmpty(plan.Added);
        Enumerable.Single(candidate.Requirements);
        var again = Build(plan.Documents, candidate);
        Assert.IsEmpty(again.AddedClasses);
        Assert.IsEmpty(again.Added);
    }

    [TestMethod]
    public void Build_PreviewsSignatureDependencyAndRejectsUnknownDependency()
    {
        var function = Function("Set", "vtkDerived *");
        var plan = Build([], Candidate("vtkBase", function));
        Assert.Contains("signature-of:vtkBase", plan.AddedClasses.Single(item => item.Class == "vtkDerived").Reasons);
        var missing = Build([], Candidate("vtkBase", Function("Set", "vtkMissing*")));
        Assert.Contains(item => item.Contains("vtkMissing", StringComparison.Ordinal), missing.Conflicts);
    }

    [TestMethod]
    public void Build_CanonicalizesIdentityAndPreservesExistingMetadata()
    {
        var existing = Function("Set", "double *") with
        {
            Parameters = [new WhitelistParameter { Type = "double *", Name = "values", Direction = "out", Length = new() { Kind = "fixed", Value = 3 } }],
        };
        var formal = Formal(existing);
        var plan = Build(formal, Candidate("vtkBase", Function("Set", "double*")));
        Assert.IsEmpty(plan.Conflicts);
        Assert.IsEmpty(plan.Added);
        Enumerable.Single(plan.Unchanged);
        Assert.AreEqual("out", plan.Documents[0].Classes[0].Functions[0].Parameters[0].Direction);
        Assert.AreEqual("double *", formal[0].Classes[0].Functions[0].Parameters[0].Type);
    }

    [TestMethod]
    public void Build_ReportsExplicitMetadataConflict()
    {
        var existing = Function("Get") with { Return = new() { Type = "vtkBase*", Ownership = "borrowed" } };
        var requested = existing with { Return = existing.Return with { Ownership = "owned" } };
        var plan = Build(Formal(existing), Candidate("vtkBase", requested));
        Enumerable.Single(plan.Conflicts);
        Assert.AreEqual("borrowed", plan.Documents[0].Classes[0].Functions[0].Return.Ownership);
    }

    [TestMethod]
    public void Build_LengthParameterRenameKeepsSameContract()
    {
        WhitelistFunction Create(string countName) => Function("Set") with
        {
            Parameters =
            [
                new() { Type = "int", Name = countName },
                new() { Type = "double*", Name = "values", Direction = "in", Length = new() { Kind = "parameter", Name = countName } },
            ],
        };
        Assert.IsEmpty(Build(Formal(Create("count")), Candidate("vtkBase", Create("size"))).Conflicts);
    }

    [TestMethod]
    public void Build_RejectsWrongModuleAndManualClass()
    {
        var wrong = Candidate("vtkBase") with { Requirements = [new() { Class = "vtkBase", Module = "vtkWrong", Header = "vtkBase.h" }] };
        Enumerable.Single(Build([], wrong).Conflicts);
        Enumerable.Single(Build([], Candidate("vtkObject")).Conflicts);
    }

    private static CandidateMergePlan Build(IReadOnlyList<WhitelistDocument> formal, CandidateDocument candidate)
        => CandidateMergePlan.Build(formal, candidate, BindingRequestPlannerTests.Hierarchy(), ["vtkObject"]);

    internal static CandidateDocument Candidate(string className, params WhitelistFunction[] functions)
        => new()
        {
            Status = "proposed", Source = new() { Kind = "manual" },
            Requirements = [new() { Class = className, Header = $"{className}.h", Module = "vtkTest", Functions = functions.ToList() }],
        };

    private static IReadOnlyList<WhitelistDocument> Formal(WhitelistFunction function)
        => [new() { Module = "vtkTest", Classes = [new() { Name = "vtkBase", Header = "vtkBase.h", Functions = [function] }] }];

    internal static WhitelistFunction Function(string name, string? parameter = null)
        => new()
        {
            Name = name, CppSignature = $"void {name}({parameter})", Return = new() { Type = "void" },
            Parameters = parameter is null ? [] : [new() { Type = parameter, Name = "value" }],
        };
}
