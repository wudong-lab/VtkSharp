using VtkSharp.Generator.Core.Configuration;
using VtkSharp.Generator.Core.Generation;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;
using VtkSharp.Generator.Core.Vtk;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Core.Exporting;

public sealed class ExportInventoryService
{
    private static readonly HashSet<string> DefaultHiddenTypeNames = new(StringComparer.Ordinal)
    {
        "vtkObjectBase",
        "vtkObject",
    };

    private readonly VtkClassInspector _inspector;

    public ExportInventoryService()
        : this(new VtkClassInspector())
    {
    }

    internal ExportInventoryService(VtkClassInspector inspector)
    {
        this._inspector = inspector;
    }

    public IReadOnlyList<ExportableTypeInfo> ListTypes(string configPath)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        var entries = workspace.LoadHierarchyEntries();
        var hiddenTypeNames = GetHiddenTypeNames(workspace);

        return entries.Values
            .Where(entry => !hiddenTypeNames.Contains(entry.ClassName))
            .OrderBy(entry => entry.ClassName, StringComparer.Ordinal)
            .Select(entry => new ExportableTypeInfo(entry.ClassName, entry.Module, entry.Header))
            .ToList();
    }

    public TypeFunctionInventory GetTypeInventory(string configPath, string typeName)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        if (workspace.IncludeDirectory is null)
            throw new InvalidOperationException("VTK include directory was not found. Set VTK_ROOT or vtk.includeDirectory in local config.");

        var hierarchyEntries = workspace.LoadHierarchyEntries();
        var hierarchyResolver = new VtkHierarchyResolver(hierarchyEntries);
        var whitelist = workspace.LoadWhitelist();
        var exportedIds = BuildExportedIds(whitelist);
        var hiddenTypeNames = GetHiddenTypeNames(workspace);
        var chain = GetInheritanceChain(typeName, hierarchyEntries);
        var candidates = new List<ExportFunctionCandidate>();

        foreach (var declaringTypeName in chain)
        {
            if (!hierarchyEntries.TryGetValue(declaringTypeName, out var entry))
                continue;

            InspectedClass inspected;
            try
            {
                inspected = this._inspector.InspectHeader(workspace.IncludeDirectory, entry.Header, declaringTypeName);
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
            {
                candidates.Add(new ExportFunctionCandidate(
                    Id: $"{declaringTypeName}::<inspection-failed>",
                    SelectedTypeName: typeName,
                    DeclaringTypeName: declaringTypeName,
                    Module: entry.Module,
                    Header: entry.Header,
                    Signature: $"{declaringTypeName}: inspection failed",
                    FunctionName: "",
                    ReturnType: "",
                    Parameters: [],
                    Status: ExportStatus.Unsupported,
                    CanSelectForExport: false,
                    Reason: ex.Message));
                continue;
            }

            foreach (var function in inspected.Functions)
            {
                var id = CreateFunctionId(declaringTypeName, function.ReturnType, function.Name, function.Parameters.Select(parameter => parameter.Type));
                var signature = $"{declaringTypeName}::{function.CppSignature}";
                var isExported = exportedIds.Contains(id);
                var reason = GetUnsupportedReason(declaringTypeName, function, hiddenTypeNames);
                var status = isExported
                    ? ExportStatus.AlreadyExported
                    : reason is null
                        ? ExportStatus.AvailableToAdd
                        : ExportStatus.Unsupported;

                candidates.Add(new ExportFunctionCandidate(
                    Id: id,
                    SelectedTypeName: typeName,
                    DeclaringTypeName: declaringTypeName,
                    Module: hierarchyResolver.GetModule(declaringTypeName),
                    Header: hierarchyResolver.GetHeader(declaringTypeName),
                    Signature: signature,
                    FunctionName: function.Name,
                    ReturnType: function.ReturnType,
                    Parameters: function.Parameters.Select(parameter => new ExportParameterCandidate(parameter.Type, parameter.Name)).ToList(),
                    Status: status,
                    CanSelectForExport: status == ExportStatus.AvailableToAdd,
                    Reason: status == ExportStatus.AlreadyExported ? null : reason));
            }
        }

        return new TypeFunctionInventory(
            typeName,
            GroupByDeclaringType(chain, candidates, ExportStatus.AlreadyExported),
            GroupByDeclaringType(chain, candidates, ExportStatus.AvailableToAdd),
            GroupByDeclaringType(chain, candidates, ExportStatus.Unsupported));
    }

    public ExportPlan CreatePlan(IEnumerable<ExportFunctionCandidate> selectedFunctions)
    {
        var functions = selectedFunctions
            .Where(function => function.CanSelectForExport)
            .GroupBy(function => function.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(function => function.Module, StringComparer.Ordinal)
            .ThenBy(function => function.DeclaringTypeName, StringComparer.Ordinal)
            .ThenBy(function => function.FunctionName, StringComparer.Ordinal)
            .ThenBy(function => function.Id, StringComparer.Ordinal)
            .ToList();

        var diagnostics = functions
            .GroupBy(function => (function.Module, function.DeclaringTypeName))
            .Select(group => $"{group.Key.Module}/{group.Key.DeclaringTypeName}: +{group.Count()} function(s)")
            .ToList();

        return new ExportPlan(functions, diagnostics);
    }

    public void ApplyPlanToWhitelist(string configPath, ExportPlan plan)
    {
        if (plan.Functions.Count == 0)
            return;

        var workspace = GeneratorWorkspace.Load(configPath);
        var documents = workspace.LoadWhitelist().ToDictionary(document => document.Module, StringComparer.Ordinal);
        var exportedIds = BuildExportedIds(documents.Values.ToList());

        foreach (var candidate in plan.Functions)
        {
            if (exportedIds.Contains(candidate.Id))
                continue;

            if (!documents.TryGetValue(candidate.Module, out var document))
            {
                document = new WhitelistDocument { Module = candidate.Module, Classes = [] };
                documents.Add(candidate.Module, document);
            }

            var whitelistClass = document.Classes.FirstOrDefault(item => item.Name == candidate.DeclaringTypeName);
            if (whitelistClass is null)
            {
                whitelistClass = new WhitelistClass
                {
                    Name = candidate.DeclaringTypeName,
                    Header = candidate.Header,
                    Functions = [],
                };
                document.Classes.Add(whitelistClass);
            }

            whitelistClass.Functions.Add(new WhitelistFunction
            {
                Name = candidate.FunctionName,
                CppSignature = candidate.Signature[(candidate.DeclaringTypeName.Length + 2)..],
                Return = new WhitelistReturn { Type = candidate.ReturnType },
                Parameters = candidate.Parameters
                    .Select(parameter => new WhitelistParameter { Type = parameter.Type, Name = parameter.Name })
                    .ToList(),
            });
            exportedIds.Add(candidate.Id);
        }

        var hierarchyEntries = workspace.LoadHierarchyEntries();
        var normalized = hierarchyEntries.Count > 0
            ? new WhitelistNormalizer().Normalize(documents.Values.ToList(), hierarchyEntries, workspace.Config.Binding.ManualBindingClasses)
            : documents.Values.OrderBy(document => document.Module, StringComparer.Ordinal).ToList();
        new WhitelistWriter().WriteDirectory(workspace.WhitelistDirectory, normalized);
    }

    internal static TypeFunctionInventory BuildTypeInventoryForTests(
        string selectedTypeName,
        IReadOnlyList<string> inheritanceChain,
        IReadOnlyDictionary<string, InspectedClass> inspectedClasses,
        IReadOnlyDictionary<string, VtkHierarchyEntry> hierarchyEntries,
        IReadOnlyCollection<string> exportedIds,
        IReadOnlyCollection<string>? hiddenTypeNames = null)
    {
        var hidden = hiddenTypeNames is null
            ? DefaultHiddenTypeNames
            : hiddenTypeNames.ToHashSet(StringComparer.Ordinal);
        var candidates = new List<ExportFunctionCandidate>();

        foreach (var declaringTypeName in inheritanceChain)
        {
            if (!inspectedClasses.TryGetValue(declaringTypeName, out var inspected))
                continue;

            var entry = hierarchyEntries.TryGetValue(declaringTypeName, out var value)
                ? value
                : new VtkHierarchyEntry(declaringTypeName, "", $"{declaringTypeName}.h", "");

            foreach (var function in inspected.Functions)
            {
                var id = CreateFunctionId(declaringTypeName, function.ReturnType, function.Name, function.Parameters.Select(parameter => parameter.Type));
                var reason = GetUnsupportedReason(declaringTypeName, function, hidden);
                var status = exportedIds.Contains(id)
                    ? ExportStatus.AlreadyExported
                    : reason is null
                        ? ExportStatus.AvailableToAdd
                        : ExportStatus.Unsupported;
                candidates.Add(new ExportFunctionCandidate(
                    id,
                    selectedTypeName,
                    declaringTypeName,
                    entry.Module,
                    entry.Header,
                    $"{declaringTypeName}::{function.CppSignature}",
                    function.Name,
                    function.ReturnType,
                    function.Parameters.Select(parameter => new ExportParameterCandidate(parameter.Type, parameter.Name)).ToList(),
                    status,
                    status == ExportStatus.AvailableToAdd,
                    status == ExportStatus.AlreadyExported ? null : reason));
            }
        }

        return new TypeFunctionInventory(
            selectedTypeName,
            GroupByDeclaringType(inheritanceChain, candidates, ExportStatus.AlreadyExported),
            GroupByDeclaringType(inheritanceChain, candidates, ExportStatus.AvailableToAdd),
            GroupByDeclaringType(inheritanceChain, candidates, ExportStatus.Unsupported));
    }

    internal static string CreateFunctionId(string declaringTypeName, string returnType, string functionName, IEnumerable<string> parameterTypes)
        => $"{declaringTypeName}::{returnType} {functionName}({string.Join(",", parameterTypes)})";

    private static HashSet<string> GetHiddenTypeNames(GeneratorWorkspace workspace)
    {
        var hidden = new HashSet<string>(DefaultHiddenTypeNames, StringComparer.Ordinal);
        foreach (var className in workspace.Config.Binding.ManualBindingClasses)
            hidden.Add(className);
        return hidden;
    }

    private static HashSet<string> BuildExportedIds(IReadOnlyList<WhitelistDocument> documents)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var document in documents)
        {
            foreach (var whitelistClass in document.Classes)
            {
                foreach (var function in whitelistClass.Functions)
                {
                    ids.Add(CreateFunctionId(
                        whitelistClass.Name,
                        function.Return.Type,
                        function.Name,
                        function.Parameters.Select(parameter => parameter.Type)));
                }
            }
        }

        return ids;
    }

    private static IReadOnlyList<string> GetInheritanceChain(string typeName, IReadOnlyDictionary<string, VtkHierarchyEntry> entries)
    {
        var result = new List<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = typeName;

        while (!string.IsNullOrWhiteSpace(current) && visited.Add(current))
        {
            result.Add(current);
            if (!entries.TryGetValue(current, out var entry) || string.IsNullOrWhiteSpace(entry.BaseClassName))
                break;
            current = entry.BaseClassName;
        }

        return result;
    }

    private static IReadOnlyList<FunctionExportGroup> GroupByDeclaringType(
        IReadOnlyList<string> declaringTypeOrder,
        IEnumerable<ExportFunctionCandidate> candidates,
        ExportStatus status)
    {
        var order = declaringTypeOrder
            .Select((typeName, index) => (typeName, index))
            .ToDictionary(item => item.typeName, item => item.index, StringComparer.Ordinal);

        return candidates
            .Where(candidate => candidate.Status == status)
            .GroupBy(candidate => candidate.DeclaringTypeName, StringComparer.Ordinal)
            .OrderBy(group => order.TryGetValue(group.Key, out var index) ? index : int.MaxValue)
            .Select(group => new FunctionExportGroup(
                group.Key,
                group.OrderBy(candidate => candidate.Signature, StringComparer.Ordinal).ToList()))
            .Where(group => group.Functions.Count > 0)
            .ToList();
    }

    private static string? GetUnsupportedReason(
        string declaringTypeName,
        InspectedFunction function,
        IReadOnlySet<string> hiddenTypeNames)
    {
        if (hiddenTypeNames.Contains(declaringTypeName))
            return $"'{declaringTypeName}' is a manual binding class.";

        foreach (var type in function.Parameters.Select(parameter => parameter.Type).Append(function.ReturnType))
        {
            if (!WhitelistValidator.IsSupportedType(type))
                return $"Unsupported type '{type}'.";
        }

        foreach (var parameter in function.Parameters)
        {
            if (TypeClassifier.IsSupportedPrimitivePointerType(parameter.Type))
                return $"Parameter '{parameter.Name}' ({parameter.Type}) requires direction and length metadata.";
        }

        return null;
    }
}
