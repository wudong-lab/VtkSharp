using System.Text.Json;
using VtkSharp.Generator.Core.Configuration;
using VtkSharp.Generator.Core.Exporting;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VtkSharp.Generator.Core.Whitelist;

public sealed class CandidateWhitelistService
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public int Diff(string configPath, string candidatePath, string format, TextWriter output, bool summary = false)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        var plan = Prepare(workspace, LoadCandidateFile(candidatePath));
        WriteDiff(plan, format, output, summary);
        return plan.Conflicts.Count == 0 ? 0 : 1;
    }

    public int Create(
        string configPath, string className, string outputPath, string sourceKind,
        string? sourceName, string? sourceOriginal, bool supportedOnly, IReadOnlyList<string>? methods,
        bool skipMissingMethods, TextWriter output, TextWriter error, bool classOnly = false)
    {
        if (classOnly && methods is { Count: > 0 })
        {
            error.WriteLine("--class-only and --methods are mutually exclusive.");
            return 1;
        }
        var workspace = GeneratorWorkspace.Load(configPath);
        if (workspace.IncludeDirectory is null)
        {
            error.WriteLine("VTK include directory was not found. Set VTK_ROOT or vtk.includeDirectory in local config.");
            return 1;
        }
        if (workspace.Config.Binding.ManualBindingClasses.Contains(className))
        {
            error.WriteLine($"unsupported: '{className}' is a manual binding class.");
            return 1;
        }
        var entries = workspace.LoadHierarchyEntries();
        if (!entries.TryGetValue(className, out var entry))
        {
            error.WriteLine($"not-found: class '{className}' was not found in the VTK hierarchy.");
            return 1;
        }
        var inspected = new VtkClassInspector().InspectHeader(workspace.IncludeDirectory, entry.Header, className);
        var functions = classOnly ? [] : inspected.Functions.ToList();
        if (methods is { Count: > 0 })
        {
            var methodSet = methods.ToHashSet(StringComparer.Ordinal);
            var missing = methodSet.Where(name => !functions.Any(function => function.Name == name)).ToList();
            foreach (var name in missing) error.WriteLine($"not-found: '{className}.{name}' is not directly declared; use plan-bindings to resolve its declaring class.");
            if (missing.Count > 0 && !skipMissingMethods) return 1;
            functions = functions.Where(function => methodSet.Contains(function.Name)).ToList();
        }
        if (supportedOnly)
        {
            foreach (var function in functions)
            {
                var eligibility = FunctionEligibility.Evaluate(function);
                if (eligibility.Status != "ready")
                    error.WriteLine($"{eligibility.Status}: {className}::{function.CanonicalSignature}: {eligibility.Reason}");
            }
            functions = functions.Where(function => FunctionEligibility.Evaluate(function).Status == "ready").ToList();
        }
        if (!classOnly && functions.Count == 0)
        {
            error.WriteLine("No functions selected. Use --class-only explicitly to request an empty wrapper.");
            return 1;
        }
        WriteCandidate(outputPath, new CandidateDocument
        {
            Status = "proposed",
            Source = new CandidateSource { Kind = sourceKind, Name = sourceName ?? "", Original = sourceOriginal ?? "" },
            Requirements = [new CandidateRequirement
            {
                Module = entry.Module, Class = className, Header = entry.Header,
                Functions = functions.Select(ToWhitelistFunction).ToList(),
            }],
        });
        output.WriteLine($"Candidate written to: {Path.GetFullPath(outputPath)}");
        return 0;
    }

    public int Merge(string configPath, string candidatePath, TextWriter output)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        var plan = Prepare(workspace, LoadCandidateFile(candidatePath));
        if (plan.Conflicts.Count > 0)
        {
            WriteDiff(plan, "text", output, summary: true);
            return 1;
        }
        if (plan.Added.Count == 0 && plan.AddedClasses.Count == 0)
        {
            output.WriteLine("No new entries to merge.");
            return 0;
        }
        if (workspace.IncludeDirectory is null)
        {
            output.WriteLine("VTK include directory was not found; merge requires validation before writing.");
            return 1;
        }
        // 写入前校验完整合并结果，防止缺少互操作元数据的候选污染正式白名单。
        var inspector = new VtkClassInspector();
        var inspected = plan.Documents.SelectMany(document => document.Classes)
            .ToDictionary(item => item.Name, item => inspector.InspectHeader(workspace.IncludeDirectory, item.Header, item.Name), StringComparer.Ordinal);
        var resolver = workspace.LoadHierarchyResolver();
        var diagnostics = plan.Documents.SelectMany(document => new WhitelistValidator().Validate(document, inspected, resolver).Diagnostics).ToList();
        if (diagnostics.Count > 0)
        {
            foreach (var diagnostic in diagnostics) output.WriteLine(diagnostic.Message);
            return 1;
        }
        new WhitelistWriter().WriteDirectory(workspace.WhitelistDirectory, plan.Documents);
        output.WriteLine($"Merged {plan.AddedClasses.Count} class(es), {plan.Added.Count} function(s), including dependencies.");
        output.WriteLine("Review the changes with git diff before committing.");
        return 0;
    }

    internal static CandidateMergePlan Prepare(GeneratorWorkspace workspace, CandidateDocument candidate)
        => CandidateMergePlan.Build(workspace.LoadWhitelist(), candidate, workspace.LoadHierarchyEntries(), workspace.Config.Binding.ManualBindingClasses);

    internal static void WriteDiff(CandidateMergePlan plan, string format, TextWriter output, bool summary)
    {
        if (format == "json")
        {
            var entries = plan.Documents.SelectMany(document => document.Classes.SelectMany(item => item.Functions.Select(function => new
            {
                id = CandidateMergePlan.FunctionId(item.Name, function),
                path = $"{document.Module}/{item.Name}",
                signature = $"{function.Name}({string.Join(",", function.Parameters.Select(parameter => parameter.Type))})->{function.Return.Type}",
            }))).DistinctBy(item => item.id).ToDictionary(item => item.id, StringComparer.Ordinal);
            output.WriteLine(JsonSerializer.Serialize(new
            {
                addedClasses = plan.AddedClasses, added = plan.Added.Select(id => entries[id]),
                unchanged = (summary ? [] : plan.Unchanged).Select(id => entries[id]), unchangedCount = plan.Unchanged.Count,
                conflicts = plan.Conflicts,
            }, JsonOptions));
            return;
        }
        output.WriteLine($"Added: {plan.AddedClasses.Count} class(es), {plan.Added.Count} function(s); already present: {plan.Unchanged.Count}; conflicts: {plan.Conflicts.Count}.");
        foreach (var item in plan.AddedClasses) output.WriteLine($"  + {item.Module}/{item.Class} [{string.Join(", ", item.Reasons)}]");
        foreach (var item in plan.Added) output.WriteLine($"  + {item}");
        if (!summary) foreach (var item in plan.Unchanged) output.WriteLine($"    {item}");
        foreach (var conflict in plan.Conflicts) output.WriteLine($"  ! {conflict}");
    }

    internal static CandidateDocument LoadCandidateFile(string path)
    {
        using var reader = File.OpenText(path);
        return new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties().Build().Deserialize<CandidateDocument>(reader);
    }

    internal static void WriteCandidate(string path, CandidateDocument candidate)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
            .WithIndentedSequences().ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull).DisableAliases().Build();
        File.WriteAllText(path, serializer.Serialize(candidate));
    }

    internal static WhitelistFunction ToWhitelistFunction(InspectedFunction function)
        => new()
        {
            Name = function.Name, CppSignature = function.CppSignature,
            Return = new WhitelistReturn { Type = function.ReturnType },
            Parameters = function.Parameters.Select(parameter => new WhitelistParameter { Type = parameter.Type, Name = parameter.Name }).ToList(),
        };
}
