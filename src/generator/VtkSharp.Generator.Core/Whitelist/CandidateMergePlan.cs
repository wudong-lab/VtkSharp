using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Types;
using VtkSharp.Generator.Core.Vtk;

namespace VtkSharp.Generator.Core.Whitelist;

public sealed record AddedCandidateClass(string Module, string Class, IReadOnlyList<string> Reasons);

public sealed record CandidateMergePlan(
    IReadOnlyList<WhitelistDocument> Documents,
    IReadOnlyList<AddedCandidateClass> AddedClasses,
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Unchanged,
    IReadOnlyList<string> Conflicts)
{
    public static CandidateMergePlan Build(
        IReadOnlyList<WhitelistDocument> formal,
        CandidateDocument candidate,
        IReadOnlyDictionary<string, VtkHierarchyEntry> hierarchy,
        IReadOnlyCollection<string> manualClasses)
    {
        var normalizer = new WhitelistNormalizer();
        // 先复制和规范化类型，但不提前补依赖，否则 diff 会漏掉自动新增的类型。
        var documents = normalizer.Normalize(formal, new Dictionary<string, VtkHierarchyEntry>(), manualClasses)
            .ToDictionary(document => document.Module, StringComparer.Ordinal);
        var originalClasses = documents.Values.SelectMany(document => document.Classes).Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        var classes = documents.Values.SelectMany(document => document.Classes.Select(item => (document.Module, Class: item)))
            .ToDictionary(item => item.Class.Name, StringComparer.Ordinal);
        var added = new SortedSet<string>(StringComparer.Ordinal);
        var unchanged = new SortedSet<string>(StringComparer.Ordinal);
        var conflicts = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var requirement in candidate.Requirements)
        {
            if (manualClasses.Contains(requirement.Class))
            {
                conflicts.Add($"{requirement.Class}: manual binding class cannot be added through a candidate.");
                continue;
            }
            if (!hierarchy.TryGetValue(requirement.Class, out var entry))
            {
                conflicts.Add($"{requirement.Class}: class not found in VTK hierarchy.");
                continue;
            }
            if (entry.Module != requirement.Module || entry.Header != requirement.Header)
            {
                conflicts.Add($"{requirement.Class}: expected {entry.Module}/{entry.Header}, got {requirement.Module}/{requirement.Header}.");
                continue;
            }
            if (classes.TryGetValue(requirement.Class, out var existing) &&
                (existing.Module != requirement.Module || existing.Class.Header != requirement.Header))
            {
                conflicts.Add($"{requirement.Class}: module/header conflicts with the formal whitelist.");
                continue;
            }
            if (!documents.TryGetValue(requirement.Module, out var document))
                documents.Add(requirement.Module, document = new WhitelistDocument { Module = requirement.Module });
            var whitelistClass = existing.Class;
            if (whitelistClass is null)
            {
                whitelistClass = new WhitelistClass { Name = requirement.Class, Header = requirement.Header };
                document.Classes.Add(whitelistClass);
                classes.Add(requirement.Class, (requirement.Module, whitelistClass));
            }
            foreach (var function in requirement.Functions)
            {
                var id = FunctionId(requirement.Class, function);
                var match = whitelistClass.Functions.FirstOrDefault(item => FunctionId(requirement.Class, item) == id);
                if (match is null)
                {
                    whitelistClass.Functions.Add(function);
                    added.Add(id);
                }
                else if (HasMetadataConflict(match, function))
                    conflicts.Add($"{id}: ownership/direction/length metadata conflicts; existing metadata will not be overwritten.");
                else if (!added.Contains(id))
                    unchanged.Add(id);
            }
        }

        var normalized = normalizer.Normalize(documents.Values.ToList(), hierarchy, manualClasses);
        var allClasses = normalized.SelectMany(document => document.Classes).ToList();
        var known = allClasses.Select(item => item.Name).Concat(manualClasses).ToHashSet(StringComparer.Ordinal);
        foreach (var item in allClasses)
        {
            if (hierarchy.TryGetValue(item.Name, out var entry) && !string.IsNullOrEmpty(entry.BaseClassName) && !known.Contains(entry.BaseClassName))
                conflicts.Add($"{item.Name}: missing base wrapper '{entry.BaseClassName}'.");
            foreach (var dependency in Dependencies(item).Where(name => !known.Contains(name)))
                conflicts.Add($"{item.Name}: missing signature dependency '{dependency}'.");
        }
        var requested = candidate.Requirements.Select(item => item.Class).ToHashSet(StringComparer.Ordinal);
        var addedClasses = normalized.SelectMany(document => document.Classes
            .Where(item => !originalClasses.Contains(item.Name))
            .Select(item => new AddedCandidateClass(document.Module, item.Name, Reasons(item.Name)))).ToList();
        return new(normalized, addedClasses, added.ToList(), unchanged.ToList(), conflicts.ToList());

        IReadOnlyList<string> Reasons(string className)
        {
            var reasons = new List<string>();
            if (requested.Contains(className)) reasons.Add("explicit-request");
            foreach (var item in allClasses)
            {
                if (hierarchy.TryGetValue(item.Name, out var entry) && entry.BaseClassName == className)
                    reasons.Add($"base-of:{item.Name}");
                if (Dependencies(item).Contains(className)) reasons.Add($"signature-of:{item.Name}");
            }
            return reasons;
        }
    }

    public static string FunctionId(string className, WhitelistFunction function)
        => FunctionId(className, function.Return.Type, function.Name, function.Parameters.Select(parameter => parameter.Type));

    public static string FunctionId(string className, string returnType, string name, IEnumerable<string> parameterTypes)
    {
        var canonicalizer = new TypeCanonicalizer();
        return $"{className}::{canonicalizer.Canonicalize(returnType).Text} {name}({string.Join(",", parameterTypes.Select(type => canonicalizer.Canonicalize(type).Text))})";
    }

    private static IEnumerable<string> Dependencies(WhitelistClass item)
        => item.Functions.SelectMany(function => function.Parameters.Select(parameter => parameter.Type).Append(function.Return.Type))
            .Select(type => TypeClassifier.TryGetVtkClassPointerName(new TypeCanonicalizer().Canonicalize(type).Text, out var name) ? name : null)
            .OfType<string>().Distinct(StringComparer.Ordinal);

    private static bool HasMetadataConflict(WhitelistFunction existing, WhitelistFunction requested)
    {
        // 省略的元数据表示沿用已有契约；显式不同值不能当作“已存在”而静默丢弃。
        if (requested.Return.Ownership is not null && requested.Return.Ownership != existing.Return.Ownership) return true;
        return requested.Parameters.Zip(existing.Parameters).Any(pair =>
            pair.First.Direction is not null && pair.First.Direction != pair.Second.Direction ||
            pair.First.Length is not null && !SameLength(existing, requested, pair.Second.Length, pair.First.Length));
    }

    private static bool SameLength(WhitelistFunction existing, WhitelistFunction requested, WhitelistLength? left, WhitelistLength right)
        => left is not null && left.Kind == right.Kind && left.Value == right.Value &&
           (right.Kind == "parameter"
               ? existing.Parameters.FindIndex(parameter => parameter.Name == left.Name) == requested.Parameters.FindIndex(parameter => parameter.Name == right.Name)
               : left.Name == right.Name);
}
