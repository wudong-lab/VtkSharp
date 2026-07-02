using System.Text.Json;
using VtkSharp.Generator.Core.Configuration;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VtkSharp.Generator.Core.Whitelist;

public sealed class CandidateWhitelistService
{
    public int Diff(string configPath, string candidatePath, string format, TextWriter output)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        var formal = workspace.LoadWhitelist();
        var candidate = LoadCandidateFile(candidatePath);

        var formalFingerprints = BuildFingerprints(formal);
        var candidateFingerprints = BuildCandidateFingerprints(candidate);

        var added = candidateFingerprints.Keys.Except(formalFingerprints, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var unchanged = candidateFingerprints.Keys.Intersect(formalFingerprints, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

        if (format.Equals("json", StringComparison.OrdinalIgnoreCase))
        {
            var result = new
            {
                added = added.Select(FingerprintToJson),
                unchanged = unchanged.Select(FingerprintToJson),
            };
            output.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        output.WriteLine($"Candidate: {candidatePath}");
        output.WriteLine($"Formal:    {workspace.WhitelistDirectory}");
        output.WriteLine();
        output.WriteLine($"Added ({added.Count}):");
        foreach (var entry in added)
            output.WriteLine($"  + {entry}");
        output.WriteLine();
        output.WriteLine($"Already present ({unchanged.Count}):");
        foreach (var entry in unchanged)
            output.WriteLine($"    {entry}");

        return 0;
    }

    public int Create(
        string configPath,
        string className,
        string outputPath,
        string sourceKind,
        string? sourceName,
        string? sourceOriginal,
        bool supportedOnly,
        IReadOnlyList<string>? methods,
        bool skipMissingMethods,
        TextWriter output,
        TextWriter error)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        if (workspace.IncludeDirectory is null)
        {
            error.WriteLine("VTK include directory was not found. Set VTK_ROOT or vtk.includeDirectory in local config.");
            return 1;
        }

        var hierarchyResolver = workspace.LoadHierarchyResolver();
        var header = hierarchyResolver.GetHeader(className);
        var entries = workspace.LoadHierarchyEntries();
        var hierarchyModule = entries.TryGetValue(className, out var entry) ? entry.Module : "";
        if (string.IsNullOrWhiteSpace(hierarchyModule))
        {
            error.WriteLine($"Class '{className}' was not found in the VTK hierarchy.");
            return 1;
        }

        var inspected = new VtkClassInspector().InspectHeader(workspace.IncludeDirectory, header, className);

        using var writer = new StringWriter();
        writer.WriteLine("# yaml-language-server: $schema=../schemas/vtksharp.whitelist-candidate.schema.json");
        writer.WriteLine();
        writer.WriteLine("status: proposed");
        writer.WriteLine("source:");
        writer.WriteLine($"  kind: {sourceKind}");
        if (!string.IsNullOrWhiteSpace(sourceName))
            writer.WriteLine($"  name: {sourceName}");
        if (!string.IsNullOrWhiteSpace(sourceOriginal))
            writer.WriteLine($"  original: \"{sourceOriginal}\"");
        writer.WriteLine();
        writer.WriteLine("requirements:");
        writer.WriteLine($"  - module: {hierarchyModule}");
        writer.WriteLine($"    class: {className}");
        writer.WriteLine($"    header: {header}");
        writer.WriteLine("    functions:");

        var functions = supportedOnly
            ? inspected.Functions.Where(IsAllTypesSupported).ToList()
            : inspected.Functions;

        if (methods is { Count: > 0 })
        {
            var methodSet = methods.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var matched = functions.Where(f => methodSet.Contains(f.Name)).ToList();
            var notFound = methodSet.Where(m => !functions.Any(f => f.Name.Equals(m, StringComparison.OrdinalIgnoreCase))).ToList();
            if (notFound.Count > 0)
            {
                if (skipMissingMethods)
                {
                    foreach (var name in notFound)
                        error.WriteLine($"Warning: method '{name}' not found on '{className}' — skipped.");
                }
                else
                {
                    error.WriteLine($"Method(s) not found on '{className}': {string.Join(", ", notFound)}");
                    return 1;
                }
            }

            functions = matched;
        }

        if (functions.Count == 0)
        {
            writer.WriteLine("      []");
        }
        else
        {
            foreach (var function in functions)
            {
                writer.WriteLine($"      - name: {function.Name}");
                writer.WriteLine($"        cppSignature: \"{EscapeYaml(function.CppSignature)}\"");
                writer.WriteLine("        return:");
                writer.WriteLine($"          type: {function.ReturnType}");
                writer.WriteLine("        parameters:");
                if (function.Parameters.Count == 0)
                {
                    writer.WriteLine("          []");
                }
                else
                {
                    foreach (var parameter in function.Parameters)
                        writer.WriteLine($"          - {{ type: \"{EscapeYaml(parameter.Type)}\", name: {parameter.Name} }}");
                }
            }
        }

        var directory = Path.GetDirectoryName(outputPath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        File.WriteAllText(outputPath, writer.ToString(), System.Text.Encoding.UTF8);
        output.WriteLine($"Candidate written to: {Path.GetFullPath(outputPath)}");
        return 0;
    }

    public int Merge(string configPath, string candidatePath, TextWriter output)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        var formal = workspace.LoadWhitelist();
        var candidate = LoadCandidateFile(candidatePath);

        var documentsByModule = formal.ToDictionary(d => d.Module, StringComparer.Ordinal);
        var formalFingerprints = BuildFingerprints(formal);
        var addedCount = 0;
        var newClassCount = 0;

        foreach (var requirement in candidate.Requirements)
        {
            if (!documentsByModule.TryGetValue(requirement.Module, out var document))
            {
                document = new WhitelistDocument { Module = requirement.Module, Classes = [] };
                documentsByModule.Add(requirement.Module, document);
            }

            var whitelistClass = document.Classes.FirstOrDefault(c => c.Name == requirement.Class);
            if (whitelistClass is null)
            {
                whitelistClass = new WhitelistClass { Name = requirement.Class, Header = requirement.Header, Functions = [] };
                document.Classes.Add(whitelistClass);
                newClassCount++;
            }

            foreach (var function in requirement.Functions)
            {
                var fingerprint = MakeFingerprint(requirement.Module, requirement.Class, function);
                if (formalFingerprints.Contains(fingerprint))
                    continue;

                whitelistClass.Functions.Add(function);
                formalFingerprints.Add(fingerprint);
                addedCount++;
            }
        }

        if (addedCount == 0 && newClassCount == 0)
        {
            output.WriteLine("No new entries to merge.");
            return 0;
        }

        new WhitelistWriter().WriteDirectory(workspace.WhitelistDirectory, documentsByModule.Values.ToList());

        var hierarchyEntries = workspace.LoadHierarchyEntries();
        if (hierarchyEntries.Count > 0)
        {
            var normalized = new WhitelistNormalizer().Normalize(documentsByModule.Values.ToList(), hierarchyEntries, workspace.Config.Binding.ManualBindingClasses);
            new WhitelistWriter().WriteDirectory(workspace.WhitelistDirectory, normalized);
        }

        var parts = new List<string>();
        if (newClassCount > 0) parts.Add($"{newClassCount} class(es)");
        if (addedCount > 0) parts.Add($"{addedCount} function(s)");
        output.WriteLine($"Merged {string.Join(", ", parts)} from candidate into formal whitelist.");
        output.WriteLine("Review the changes with git diff before committing.");
        return 0;
    }

    private static CandidateDocument LoadCandidateFile(string path)
    {
        using var reader = File.OpenText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        return deserializer.Deserialize<CandidateDocument>(reader);
    }

    private static HashSet<string> BuildFingerprints(IReadOnlyList<WhitelistDocument> documents)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var document in documents)
        {
            foreach (var whitelistClass in document.Classes)
            {
                foreach (var function in whitelistClass.Functions)
                {
                    set.Add(MakeFingerprint(document.Module, whitelistClass.Name, function));
                }
            }
        }

        return set;
    }

    private static Dictionary<string, string> BuildCandidateFingerprints(CandidateDocument candidate)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var requirement in candidate.Requirements)
        foreach (var function in requirement.Functions)
        {
            var fingerprint = MakeFingerprint(requirement.Module, requirement.Class, function);
            dict[fingerprint] = requirement.Class;
        }

        return dict;
    }

    private static string MakeFingerprint(string module, string className, WhitelistFunction function)
    {
        var paramTypes = string.Join(",", function.Parameters.Select(p => p.Type));
        return $"{module}/{className}::{function.Name}({paramTypes})->{function.Return.Type}";
    }

    private static object FingerprintToJson(string fingerprint)
    {
        var parts = fingerprint.Split("::", 2);
        var path = parts[0];
        var signature = parts.Length > 1 ? parts[1] : "";
        return new { path, signature };
    }

    private static bool IsAllTypesSupported(InspectedFunction function)
    {
        var types = function.Parameters.Select(p => p.Type).Append(function.ReturnType);
        return types.All(WhitelistValidator.IsSupportedType);
    }

    private static string EscapeYaml(string text)
        => text.Contains('"', StringComparison.Ordinal)
            ? text.Replace("\"", "\\\"", StringComparison.Ordinal)
            : text;
}