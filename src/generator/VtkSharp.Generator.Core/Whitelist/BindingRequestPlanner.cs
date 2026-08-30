using System.Text.Json;
using VtkSharp.Generator.Core.Configuration;
using VtkSharp.Generator.Core.Exporting;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Vtk;

namespace VtkSharp.Generator.Core.Whitelist;

public sealed class BindingRequestPlanner
{
    public int Inspect(string configPath, string className, string method, string format, TextWriter output)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        if (workspace.IncludeDirectory is null)
        {
            output.WriteLine("VTK include directory was not found.");
            return 1;
        }
        var hierarchy = workspace.LoadHierarchyEntries();
        var inspector = new VtkClassInspector();
        var plan = Build(new BindingRequestDocument
        {
            Requests = [new BindingRequest { Class = className, Methods = [method], AllOverloads = true }],
        }, workspace.LoadWhitelist(), hierarchy, workspace.Config.Binding.ManualBindingClasses,
            name => inspector.InspectHeader(workspace.IncludeDirectory, hierarchy[name].Header, name));
        if (format == "json") output.WriteLine(JsonSerializer.Serialize(plan.Diagnostics, CandidateWhitelistService.JsonOptions));
        else foreach (var item in plan.Diagnostics)
            output.WriteLine($"{item.Status}: {string.Join(", ", item.Signatures ?? [])} {item.Reason}");
        return plan.HasUnresolved ? 1 : 0;
    }

    public int Run(string configPath, string requestsPath, string outputPath, string reportPath, bool referenceScan, TextWriter output)
    {
        if (new[] { requestsPath, outputPath, reportPath }.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
            throw new ArgumentException("Requests, candidate and report paths must be different.");
        var requests = LoadRequests(requestsPath, referenceScan);
        var workspace = GeneratorWorkspace.Load(configPath);
        if (workspace.IncludeDirectory is null)
        {
            output.WriteLine("VTK include directory was not found.");
            return 1;
        }
        var hierarchy = workspace.LoadHierarchyEntries();
        var formal = workspace.LoadWhitelist();
        var inspector = new VtkClassInspector();
        var plan = Build(requests, formal, hierarchy, workspace.Config.Binding.ManualBindingClasses,
            className => inspector.InspectHeader(workspace.IncludeDirectory, hierarchy[className].Header, className));
        var merge = CandidateMergePlan.Build(formal, plan.Candidate, hierarchy, workspace.Config.Binding.ManualBindingClasses);
        CandidateWhitelistService.WriteCandidate(outputPath, plan.Candidate);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);
        File.WriteAllText(reportPath, JsonSerializer.Serialize(new
        {
            ok = !plan.HasUnresolved && merge.Conflicts.Count == 0,
            diagnostics = plan.Diagnostics, enumDiagnostics = plan.EnumDiagnostics, addedEnums = merge.AddedEnums,
            addedClasses = merge.AddedClasses, added = merge.Added, conflicts = merge.Conflicts,
        }, CandidateWhitelistService.JsonOptions));
        foreach (var group in plan.Diagnostics.GroupBy(item => item.Status).OrderBy(group => group.Key, StringComparer.Ordinal))
            output.WriteLine($"{group.Key}: {group.Count()}");
        foreach (var item in plan.Diagnostics.Where(item => item.Status is not ("ready" or "already-exported")))
            output.WriteLine($"  {item.Class}.{item.Request}: {item.Status} — {item.Reason}");
        foreach (var conflict in merge.Conflicts) output.WriteLine($"  conflict: {conflict}");
        foreach (var diagnostic in plan.EnumDiagnostics) output.WriteLine(diagnostic);
        foreach (var item in merge.AddedEnums) output.WriteLine($"Enum group: {item} (public get/set types change)");
        output.WriteLine($"Candidate: {Path.GetFullPath(outputPath)}");
        output.WriteLine($"Report: {Path.GetFullPath(reportPath)}");
        return plan.HasUnresolved || merge.Conflicts.Count > 0 ? 1 : 0;
    }

    internal static BindingRequestPlan Build(
        BindingRequestDocument requests, IReadOnlyList<WhitelistDocument> formal,
        IReadOnlyDictionary<string, VtkHierarchyEntry> hierarchy, IReadOnlyCollection<string> manualClasses,
        Func<string, InspectedClass> inspect)
    {
        var exported = formal.SelectMany(document => document.Classes.SelectMany(item => item.Functions
            .Select(function => CandidateMergePlan.FunctionId(item.Name, function)))).ToHashSet(StringComparer.Ordinal);
        var formalClasses = formal.SelectMany(document => document.Classes).Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        var requirements = new Dictionary<string, CandidateRequirement>(StringComparer.Ordinal);
        var cache = new Dictionary<string, InspectedClass>(StringComparer.Ordinal);
        var inspectionErrors = new Dictionary<string, string>(StringComparer.Ordinal);
        var diagnostics = new List<BindingRequestDiagnostic>();

        foreach (var request in requests.Requests)
        {
            if (request.ClassOnly && (request.Methods.Count > 0 || request.Signatures.Count > 0) ||
                !request.ClassOnly && request.Methods.Count == 0 && request.Signatures.Count == 0)
            {
                diagnostics.Add(new(request.Class, "<class>", "invalid-request", Reason: "Specify classOnly or methods/signatures; an empty selection never means all methods."));
                continue;
            }
            if (!hierarchy.ContainsKey(request.Class))
            {
                diagnostics.Add(new(request.Class, "<class>", "not-found", Reason: "Class not found in VTK hierarchy."));
                continue;
            }
            if (request.ClassOnly)
            {
                if (manualClasses.Contains(request.Class) || formalClasses.Contains(request.Class))
                    diagnostics.Add(new(request.Class, "<class>", "already-exported"));
                else
                {
                    if (GetInspected(request.Class, request.Class) is null) continue;
                    AddClass(request.Class);
                    diagnostics.Add(new(request.Class, "<class>", "ready"));
                }
                continue;
            }
            foreach (var method in request.Methods.Distinct(StringComparer.Ordinal))
                Resolve(request, method, null);
            foreach (var signature in request.Signatures.Distinct(StringComparer.Ordinal))
            {
                var separator = signature.IndexOf("::", StringComparison.Ordinal);
                var paren = signature.IndexOf('(');
                var nameStart = paren > 0 ? signature.LastIndexOf(' ', paren) : -1;
                if (separator < 1 || paren < 0 || nameStart < separator + 2)
                    diagnostics.Add(new(request.Class, signature, "invalid-request", Reason: "Use an exact signature ID from the report: vtkClass::returnType Method(types)."));
                else
                    Resolve(request, signature[(nameStart + 1)..paren], signature);
            }
        }
        return new(new CandidateDocument
        {
            Status = "proposed", Source = requests.Source ?? new CandidateSource { Kind = "manual", Name = "plan-bindings" },
            Requirements = requirements.Values.OrderBy(item => item.Module, StringComparer.Ordinal).ThenBy(item => item.Class, StringComparer.Ordinal)
                .Select(item => item with { Functions = item.Functions.OrderBy(function => CandidateMergePlan.FunctionId(item.Class, function), StringComparer.Ordinal).ToList() }).ToList(),
        }, diagnostics) { EnumDiagnostics = cache.Values.SelectMany(c => c.EnumDiagnostics ?? []).ToList() };

        InspectedClass? GetInspected(string className, string requestedClass)
        {
            if (cache.TryGetValue(className, out var item)) return item;
            if (!inspectionErrors.ContainsKey(className))
            {
                try
                {
                    cache.Add(className, item = inspect(className));
                    return item;
                }
                catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
                {
                    // 批量输入中的单个头文件失败不应丢失其他类型的规划结果。
                    inspectionErrors.Add(className, ex.Message);
                }
            }
            diagnostics.Add(new(requestedClass, "<class>", "inspection-failed", className, inspectionErrors[className]));
            return null;
        }

        CandidateRequirement AddClass(string className)
        {
            if (!requirements.TryGetValue(className, out var item))
            {
                var entry = hierarchy[className];
                requirements.Add(className, item = new CandidateRequirement { Class = className, Module = entry.Module, Header = entry.Header });
            }
            return item;
        }

        void Resolve(BindingRequest request, string method, string? signature)
        {
            var current = request.Class;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (!string.IsNullOrEmpty(current) && visited.Add(current))
            {
                if (!hierarchy.TryGetValue(current, out var entry)) break;
                var inspected = GetInspected(current, request.Class);
                if (inspected is null) return;
                var matches = inspected.Functions.Where(function => function.Name == method).ToList();
                // 同名 private/static 成员也会阻止继续向基类查找，不能仅凭可导出列表判断隐藏。
                if (matches.Count > 0 || inspected.DeclaredMemberNames?.Contains(method) == true)
                {
                    var ids = matches.Select(function => CandidateMergePlan.FunctionId(current, function.ReturnType, function.Name, function.Parameters.Select(parameter => parameter.Type))).ToList();
                    if (signature is not null)
                        matches = matches.Where((_, index) => ids[index] == signature).ToList();
                    if (matches.Count == 0)
                    {
                        diagnostics.Add(new(request.Class, signature ?? method, "unsupported", current,
                            "The nearest declaration has no matching exportable signature; hidden base overloads are not searched.", ids));
                        return;
                    }
                    if (signature is null && matches.Count > 1 && !request.AllOverloads && !ids.All(exported.Contains))
                    {
                        diagnostics.Add(new(request.Class, method, "ambiguous", current, "Select a signature ID or explicitly set allOverloads: true.", ids));
                        return;
                    }
                    foreach (var function in matches)
                    {
                        var id = CandidateMergePlan.FunctionId(current, function.ReturnType, function.Name, function.Parameters.Select(parameter => parameter.Type));
                        var eligibility = manualClasses.Contains(current)
                            ? new FunctionEligibility("unsupported", "Manual binding class; inspect its managed API instead.")
                            : exported.Contains(id) ? new FunctionEligibility("already-exported") : FunctionEligibility.Evaluate(function);
                        diagnostics.Add(new(request.Class, signature ?? method, eligibility.Status, current, eligibility.Reason, [id]));
                        if (eligibility.Status is "ready" or "already-exported" &&
                            !formalClasses.Contains(request.Class) && !manualClasses.Contains(request.Class))
                            AddClass(request.Class);
                        if (eligibility.Status == "ready")
                        {
                            var item = AddClass(current);
                            if (!item.Functions.Any(existing => CandidateMergePlan.FunctionId(current, existing) == id))
                                item.Functions.Add(CandidateWhitelistService.ToWhitelistFunction(function));
                        }
                        if (eligibility.Status is "ready" or "already-exported" &&
                            inspected.EnumProperties?.Any(p => p.Methods.Contains(function.Name)) == true)
                            EnumCandidateExpansion.Expand(AddClass(current), inspected, [function.Name]);
                    }
                    return;
                }
                if (inspected.HasMultipleBaseClasses)
                {
                    diagnostics.Add(new(request.Class, signature ?? method, "ambiguous", current, "Multiple inheritance requires an explicit declaring-class request."));
                    return;
                }
                current = entry.BaseClassName;
            }
            diagnostics.Add(new(request.Class, signature ?? method, "not-found", Reason: "No declaration found in the supported inheritance chain."));
        }
    }

    internal static BindingRequestDocument LoadRequests(string path, bool referenceScan)
    {
        var json = File.ReadAllText(path);
        if (!referenceScan)
        {
            var options = new JsonSerializerOptions(CandidateWhitelistService.JsonOptions)
            {
                UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
            };
            var requests = JsonSerializer.Deserialize<BindingRequestDocument>(json, options);
            return requests is { Requests.Count: > 0 } ? requests : throw new InvalidOperationException("No binding requests supplied.");
        }
        var scan = JsonSerializer.Deserialize<ReferenceScan>(json, CandidateWhitelistService.JsonOptions)
            ?? throw new InvalidOperationException("Empty reference scan.");
        if (scan.Warnings.Count > 0)
            throw new InvalidOperationException($"Resolve reference scan warnings first: {string.Join(Environment.NewLine, scan.Warnings)}");
        return new BindingRequestDocument
        {
            Source = new CandidateSource { Kind = "manual", Name = "from-reference-exports", Original = Path.GetFullPath(path) },
            Requests = scan.Classes.Select(item => new BindingRequest
            {
                Class = item.Name, Methods = item.Methods, ClassOnly = item.Methods.Count == 0, AllOverloads = true,
            }).ToList(),
        };
    }

    private sealed record ReferenceScan
    {
        public List<ReferenceClass> Classes { get; init; } = [];
        public List<string> Warnings { get; init; } = [];
    }

    private sealed record ReferenceClass
    {
        public string Name { get; init; } = "";
        public List<string> Methods { get; init; } = [];
    }
}
