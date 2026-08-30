using System.Text.Json;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

public sealed class CandidateWorkflowTests
{
    [Fact]
    public void DiffJson_KeepsPathAndSignatureFieldsAlongsideStableId()
    {
        var fixture = new Fixture();
        CandidateWhitelistService.WriteCandidate(fixture.Candidate,
            CandidateMergePlanTests.Candidate("vtkBase", CandidateMergePlanTests.Function("Update")));
        var output = new StringWriter();
        Assert.Equal(0, new CandidateWhitelistService().Diff(fixture.Config, fixture.Candidate, "json", output));
        using var json = JsonDocument.Parse(output.ToString());
        var entry = json.RootElement.GetProperty("added")[0];
        Assert.Equal("vtkTest/vtkBase", entry.GetProperty("path").GetString());
        Assert.Equal("Update()->void", entry.GetProperty("signature").GetString());
        Assert.Equal("vtkBase::void Update()", entry.GetProperty("id").GetString());
    }

    [Fact]
    public void ClassOnly_RoundTripsWindowsSourceAndMergeIsIdempotent()
    {
        var fixture = new Fixture();
        var service = new CandidateWhitelistService();
        Assert.Equal(0, service.Create(fixture.Config, "vtkDerived", fixture.Candidate, "manual", "source: test",
            @"D:\Reference\test.cpp", true, null, false, TextWriter.Null, TextWriter.Null, classOnly: true));
        var candidate = CandidateWhitelistService.LoadCandidateFile(fixture.Candidate);
        Assert.Equal(@"D:\Reference\test.cpp", candidate.Source!.Original);
        Assert.Empty(Assert.Single(candidate.Requirements).Functions);
        var preview = new StringWriter();
        Assert.Equal(0, service.Diff(fixture.Config, fixture.Candidate, "json", preview));
        using var json = JsonDocument.Parse(preview.ToString());
        Assert.Equal(2, json.RootElement.GetProperty("addedClasses").GetArrayLength());
        Assert.Empty(Directory.GetFiles(fixture.Whitelist));
        Assert.Equal(0, service.Merge(fixture.Config, fixture.Candidate, TextWriter.Null));
        var file = Path.Combine(fixture.Whitelist, "vtkTest.yml");
        var expected = File.ReadAllBytes(file);
        Assert.Equal(0, service.Merge(fixture.Config, fixture.Candidate, TextWriter.Null));
        Assert.Equal(expected, File.ReadAllBytes(file));
    }

    [Fact]
    public void SupportedOnly_ReportsMetadataInsteadOfMissingAndDoesNotWriteEmptyCandidate()
    {
        var fixture = new Fixture();
        var error = new StringWriter();
        Assert.Equal(1, new CandidateWhitelistService().Create(fixture.Config, "vtkBase", fixture.Candidate, "manual",
            null, null, true, ["SetValues"], false, TextWriter.Null, error));
        Assert.Contains("needs-metadata", error.ToString());
        Assert.DoesNotContain("not-found", error.ToString());
        Assert.False(File.Exists(fixture.Candidate));
    }

    [Fact]
    public void Merge_InvalidCandidateDoesNotWriteAnyWhitelistFiles()
    {
        var fixture = new Fixture();
        CandidateWhitelistService.WriteCandidate(fixture.Candidate,
            CandidateMergePlanTests.Candidate("vtkBase", CandidateMergePlanTests.Function("SetValues", "double*")));
        Assert.Equal(1, new CandidateWhitelistService().Merge(fixture.Config, fixture.Candidate, TextWriter.Null));
        Assert.Empty(Directory.GetFiles(fixture.Whitelist));
    }

    [Fact]
    public void Planner_WritesPartialCandidateAndDetailedReportButReturnsFailureForUnresolved()
    {
        var fixture = new Fixture();
        var requests = Path.Combine(fixture.Root, "requests.json");
        var report = Path.Combine(fixture.Root, "report.json");
        File.WriteAllText(requests, """
            {"requests":[{"class":"vtkDerived","methods":["Update","Missing"]}]}
            """);
        Assert.Equal(1, new BindingRequestPlanner().Run(fixture.Config, requests, fixture.Candidate, report, false, TextWriter.Null));
        Assert.Empty(Directory.GetFiles(fixture.Whitelist));
        var candidate = CandidateWhitelistService.LoadCandidateFile(fixture.Candidate);
        Assert.Equal("Update", Assert.Single(candidate.Requirements.Single(item => item.Class == "vtkBase").Functions).Name);
        using var json = JsonDocument.Parse(File.ReadAllText(report));
        Assert.False(json.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal(2, json.RootElement.GetProperty("diagnostics").GetArrayLength());
    }

    [Fact]
    public void Planner_ConsumesReferenceScannerShapeAndRejectsWarnings()
    {
        var fixture = new Fixture();
        var scan = Path.Combine(fixture.Root, "scan.json");
        File.WriteAllText(scan, """
            {"ReferenceDirectory":"D:/Reference","Files":2,"Classes":[
              {"Name":"vtkBase","Files":["vtkBase_export_gen.cpp"],"Methods":["Update"]},
              {"Name":"vtkDerived","Files":["vtkDerived_export_gen.cpp"],"Methods":[]}
            ],"Warnings":[]}
            """);
        var requests = BindingRequestPlanner.LoadRequests(scan, true);
        Assert.True(requests.Requests[0].AllOverloads);
        Assert.True(requests.Requests[1].ClassOnly);
        File.WriteAllText(scan, """{"Classes":[],"Warnings":["No VTK class include found"]}""");
        Assert.Throws<InvalidOperationException>(() => BindingRequestPlanner.LoadRequests(scan, true));
    }

    [Fact]
    public void Planner_RejectsMisspelledRequestFields()
    {
        var fixture = new Fixture();
        var requests = Path.Combine(fixture.Root, "requests.json");
        File.WriteAllText(requests, """{"requests":[{"class":"vtkBase","method":["Update"]}]}""");
        Assert.Throws<JsonException>(() => BindingRequestPlanner.LoadRequests(requests, false));
    }

    private sealed class Fixture
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "VtkSharp.Generator.Tests", Guid.NewGuid().ToString("N"));
        public string Config => Path.Combine(Root, "generator.yml");
        public string Candidate => Path.Combine(Root, "candidate.yml");
        public string Whitelist => Path.Combine(Root, "whitelist");

        public Fixture()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(Whitelist);
            var include = Directory.CreateDirectory(Path.Combine(Root, "include")).FullName;
            var hierarchy = Directory.CreateDirectory(Path.Combine(Root, "hierarchy")).FullName;
            File.WriteAllText(Config, $$"""
                vtk:
                  includeDirectory: '{{include}}'
                  hierarchyDirectory: '{{hierarchy}}'
                binding:
                  manualBindingClasses: [vtkObject]
                paths:
                  whitelistDirectory: whitelist
                """);
            File.WriteAllText(Path.Combine(hierarchy, "vtkTest-hierarchy.txt"), """
                vtkBase : vtkObject ; vtkBase.h ; vtkTest
                vtkDerived : vtkBase ; vtkDerived.h ; vtkTest
                """);
            File.WriteAllText(Path.Combine(include, "vtkBase.h"), """
                #pragma once
                class vtkObject {};
                class vtkBase : public vtkObject {
                public:
                    static vtkBase* New();
                    void Update();
                    void SetValues(double* values);
                };
                """);
            File.WriteAllText(Path.Combine(include, "vtkDerived.h"), """
                #include "vtkBase.h"
                class vtkDerived : public vtkBase {
                public:
                    static vtkDerived* New();
                };
                """);
        }
    }
}
