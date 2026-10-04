using System.Security.Cryptography;
using System.Text;

namespace VtkSharp.Generator.Core.Configuration;

public sealed class NativeModuleLayout
{
    private NativeModuleLayout(
        string nativeLibraryPrefix,
        string strategyFingerprint,
        IReadOnlyList<NativeTargetLayout> targets,
        IReadOnlyDictionary<string, NativeTargetLayout> targetsByModule,
        IReadOnlyDictionary<string, IReadOnlyList<string>> manualSourcesByTarget)
    {
        this.NativeLibraryPrefix = nativeLibraryPrefix;
        this.StrategyFingerprint = strategyFingerprint;
        this.Targets = targets;
        this._targetsByModule = targetsByModule;
        this.ManualSourcesByTarget = manualSourcesByTarget;
    }

    private readonly IReadOnlyDictionary<string, NativeTargetLayout> _targetsByModule;

    public string NativeLibraryPrefix { get; }
    public string StrategyFingerprint { get; }
    public IReadOnlyList<NativeTargetLayout> Targets { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ManualSourcesByTarget { get; }

    public string GetNativeLibraryName(string module)
        => this.GetTarget(module).Name + ".dll";

    public NativeTargetLayout GetTarget(string module)
        => this._targetsByModule.TryGetValue(module, out var target)
            ? target
            : throw new InvalidDataException($"Wrapped VTK module '{module}' has no native target.");

    public static NativeModuleLayout Create(
        NativeModuleStrategyConfig strategy,
        IEnumerable<string> wrappedModules,
        string nativeLibraryPrefix,
        string configuredVtkVersion,
        string nativeSourceRoot,
        NativeModuleMetadataSnapshot? metadata = null)
    {
        if (strategy.SchemaVersion != 1)
            throw new InvalidDataException($"Unsupported native module strategy schema version: {strategy.SchemaVersion}.");
        if (strategy.StrategyVersion < 1 || string.IsNullOrWhiteSpace(strategy.VtkVersion) || string.IsNullOrWhiteSpace(strategy.Platform))
            throw new InvalidDataException("Native module strategy must specify a positive strategy version, VTK version, and platform.");
        if (strategy.VtkVersion != configuredVtkVersion && !strategy.VtkVersion.StartsWith(configuredVtkVersion + ".", StringComparison.Ordinal))
            throw new InvalidDataException($"Native module strategy targets VTK {strategy.VtkVersion}, but generator config targets VTK {configuredVtkVersion}.");
        if (string.IsNullOrWhiteSpace(nativeLibraryPrefix))
            throw new InvalidDataException("The native library prefix is required.");
        var metadataByModule = ValidateMetadata(metadata, strategy, configuredVtkVersion);

        var moduleList = wrappedModules.ToList();
        var moduleSet = new HashSet<string>(moduleList, StringComparer.Ordinal);
        if (moduleSet.Count != moduleList.Count)
            throw new InvalidDataException("The wrapper input contains a duplicate VTK module.");
        if (metadataByModule.Count > 0)
        {
            var missing = moduleSet.Except(metadataByModule.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            var unexpected = metadataByModule.Keys.Except(moduleSet, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
            if (missing.Length > 0 || unexpected.Length > 0)
                throw new InvalidDataException($"Native module metadata does not match the wrapper module set. Missing: [{string.Join(", ", missing)}]; unexpected: [{string.Join(", ", unexpected)}].");
        }

        var targets = new List<NativeTargetLayout>();
        var targetsByModule = new Dictionary<string, NativeTargetLayout>(StringComparer.Ordinal);
        var targetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var targetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var groupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var group in strategy.Groups)
        {
            if (string.IsNullOrWhiteSpace(group.Name) || !groupNames.Add(group.Name))
                throw new InvalidDataException($"Native module group name is empty or duplicated: '{group.Name}'.");
            if (!IsValidTargetName(group.Target) || !targetNames.Add(group.Target))
                throw new InvalidDataException($"Native module target name is empty or duplicated: '{group.Target}'.");
            if (!targetKeys.Add(ToTargetKey(group.Target)))
                throw new InvalidDataException($"Native module target key conflicts with another target: '{group.Target}'.");
            if (group.Modules.Count == 0)
                throw new InvalidDataException($"Native module group '{group.Name}' has no modules.");

            var groupModules = DistinctOrThrow(group.Modules, $"group '{group.Name}'", "module");
            foreach (var module in groupModules)
            {
                if (!moduleSet.Contains(module))
                    throw new InvalidDataException($"Native module group '{group.Name}' names unknown wrapped module '{module}'.");
            }

            ValidateInitializationProviders(group, groupModules, metadataByModule);

            var providers = DistinctOrThrow(group.InitializationProviders, $"group '{group.Name}'", "initialization provider");
            foreach (var provider in providers)
            {
                if (!groupModules.Contains(provider, StringComparer.Ordinal))
                    throw new InvalidDataException($"Initialization provider '{provider}' is not a member of native module group '{group.Name}'.");
            }

            var target = new NativeTargetLayout(group.Target, groupModules.Order(StringComparer.Ordinal).ToArray(), providers.Order(StringComparer.Ordinal).ToArray());
            targets.Add(target);
            foreach (var module in groupModules)
            {
                if (!targetsByModule.TryAdd(module, target))
                    throw new InvalidDataException($"VTK module '{module}' is assigned to more than one native target.");
            }
        }

        foreach (var module in moduleList.Order(StringComparer.Ordinal))
        {
            if (targetsByModule.ContainsKey(module))
                continue;

            var suffix = module.StartsWith("vtk", StringComparison.Ordinal) ? module[3..] : module;
            var targetName = $"{nativeLibraryPrefix}.{suffix}";
            if (!IsValidTargetName(targetName))
                throw new InvalidDataException($"Independent native target name is invalid: '{targetName}'.");
            if (!targetNames.Add(targetName))
                throw new InvalidDataException($"Independent native target name conflicts with another target: '{targetName}'.");
            if (!targetKeys.Add(ToTargetKey(targetName)))
                throw new InvalidDataException($"Independent native target key conflicts with another target: '{targetName}'.");
            var target = new NativeTargetLayout(targetName, [module], []);
            targets.Add(target);
            targetsByModule.Add(module, target);
        }

        targets.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
        var targetNameSet = targets.Select(target => target.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sourceOwnership = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sourceLists = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var ownership in strategy.ManualSourceOwnership)
        {
            var source = NormalizeRelativeSourcePath(ownership.Source);
            if (!sourceOwnership.TryAdd(source, ownership.Target))
                throw new InvalidDataException($"Manual native source '{source}' is assigned more than once.");
            if (!targetNameSet.Contains(ownership.Target))
                throw new InvalidDataException($"Manual native source '{source}' names unknown target '{ownership.Target}'.");

            var sourcePath = Path.GetFullPath(Path.Combine(nativeSourceRoot, source.Replace('/', Path.DirectorySeparatorChar)));
            var sourceRoot = Path.GetFullPath(nativeSourceRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!sourcePath.StartsWith(sourceRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Manual native source '{source}' escapes the native source directory.");
            if (!File.Exists(sourcePath))
                throw new InvalidDataException($"Manual native source does not exist: '{source}'.");

            if (!sourceLists.TryGetValue(ownership.Target, out var ownedSources))
                sourceLists.Add(ownership.Target, ownedSources = []);
            ownedSources.Add(source);
        }

        var manualSourcesByTarget = sourceLists.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<string>)pair.Value.Order(StringComparer.Ordinal).ToArray(),
            StringComparer.OrdinalIgnoreCase);
        var fingerprint = CreateFingerprint(strategy, targets, manualSourcesByTarget, metadata);
        return new NativeModuleLayout(nativeLibraryPrefix, fingerprint, targets, targetsByModule, manualSourcesByTarget);
    }

    private static IReadOnlyDictionary<string, NativeModuleMetadata> ValidateMetadata(
        NativeModuleMetadataSnapshot? metadata,
        NativeModuleStrategyConfig strategy,
        string configuredVtkVersion)
    {
        if (metadata is null)
            return new Dictionary<string, NativeModuleMetadata>(StringComparer.Ordinal);
        if (metadata.SchemaVersion != 1 || metadata.Configuration.Length == 0)
            throw new InvalidDataException("Native module metadata snapshot must use schema version 1 and specify a build configuration.");
        if (metadata.VtkVersion != strategy.VtkVersion ||
            (metadata.VtkVersion != configuredVtkVersion && !metadata.VtkVersion.StartsWith(configuredVtkVersion + ".", StringComparison.Ordinal)))
            throw new InvalidDataException($"Native module metadata snapshot VTK version '{metadata.VtkVersion}' does not match strategy '{strategy.VtkVersion}'.");
        if (!string.Equals(metadata.Platform, strategy.Platform, StringComparison.Ordinal))
            throw new InvalidDataException($"Native module metadata platform '{metadata.Platform}' does not match strategy '{strategy.Platform}'.");

        var result = new Dictionary<string, NativeModuleMetadata>(StringComparer.Ordinal);
        foreach (var module in metadata.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.Module) || !result.TryAdd(module.Module, module))
                throw new InvalidDataException($"Native module metadata contains an empty or duplicate module '{module.Module}'.");
        }
        return result;
    }

    private static void ValidateInitializationProviders(
        NativeModuleGroupConfig group,
        IReadOnlyList<string> groupModules,
        IReadOnlyDictionary<string, NativeModuleMetadata> metadataByModule)
    {
        if (metadataByModule.Count == 0)
            return;

        var groupedTargets = groupModules.Select(ToVtkTarget).ToHashSet(StringComparer.Ordinal);
        foreach (var provider in group.InitializationProviders)
        {
            if (!metadataByModule.TryGetValue(provider, out var providerMetadata))
                throw new InvalidDataException($"Native module metadata snapshot is missing initialization provider '{provider}'.");
            if (!providerMetadata.NeedsAutoinit || !providerMetadata.Implements.Any(groupedTargets.Contains))
                throw new InvalidDataException($"Initialization provider '{provider}' does not provide an autoinit implementation for group '{group.Name}'.");
        }

        foreach (var module in groupModules)
        {
            if (!metadataByModule.TryGetValue(module, out var metadata))
                throw new InvalidDataException($"Native module metadata snapshot is missing grouped module '{module}'.");
            if (!metadata.Implementable || group.InitializationProviders.Contains(module, StringComparer.Ordinal))
                continue;

            var vtkTarget = ToVtkTarget(module);
            var covered = group.InitializationProviders.Any(provider =>
                metadataByModule.TryGetValue(provider, out var providerMetadata) &&
                providerMetadata.Implements.Contains(vtkTarget, StringComparer.Ordinal));
            if (!covered)
                throw new InvalidDataException($"Initialization providers in group '{group.Name}' do not implement required target '{module}'.");
        }
    }

    private static string ToVtkTarget(string module)
        => $"VTK::{(module.StartsWith("vtk", StringComparison.Ordinal) ? module[3..] : module)}";

    private static List<string> DistinctOrThrow(List<string> values, string owner, string kind)
    {
        var result = values.ToList();
        if (result.Count != result.Distinct(StringComparer.Ordinal).Count())
            throw new InvalidDataException($"Native module {owner} contains a duplicate {kind}.");
        return result;
    }

    private static string NormalizeRelativeSourcePath(string source)
    {
        if (string.IsNullOrWhiteSpace(source) || Path.IsPathRooted(source))
            throw new InvalidDataException($"Manual native source path must be relative: '{source}'.");
        return source.Replace('\\', '/');
    }

    private static string CreateFingerprint(
        NativeModuleStrategyConfig strategy,
        IReadOnlyList<NativeTargetLayout> targets,
        IReadOnlyDictionary<string, IReadOnlyList<string>> manualSources,
        NativeModuleMetadataSnapshot? metadata)
    {
        var text = new StringBuilder()
            .Append(strategy.SchemaVersion).Append('\n')
            .Append(strategy.StrategyVersion).Append('\n')
            .Append(strategy.VtkVersion).Append('\n')
            .Append(strategy.Platform).Append('\n');
        foreach (var target in targets)
        {
            text.Append(target.Name).Append('\n');
            foreach (var module in target.Modules) text.Append("module:").Append(module).Append('\n');
            foreach (var provider in target.InitializationProviders) text.Append("provider:").Append(provider).Append('\n');
            if (manualSources.TryGetValue(target.Name, out var sources))
                foreach (var source in sources) text.Append("source:").Append(source).Append('\n');
        }
        foreach (var group in strategy.Groups.OrderBy(group => group.Name, StringComparer.Ordinal))
        {
            text.Append("group:").Append(group.Name).Append('\n')
                .Append("target:").Append(group.Target).Append('\n')
                .Append("rationale:").Append(group.Rationale.Trim()).Append('\n');
        }
        foreach (var ownership in strategy.ManualSourceOwnership
                     .OrderBy(item => item.Source, StringComparer.Ordinal)
                     .ThenBy(item => item.Target, StringComparer.Ordinal))
        {
            text.Append("manual-source-owner:").Append(ownership.Source.Replace('\\', '/')).Append('\n')
                .Append("manual-source-target:").Append(ownership.Target).Append('\n')
                .Append("manual-source-rationale:").Append(ownership.Rationale.Trim()).Append('\n');
        }
        if (metadata is not null)
        {
            text.Append("metadata:").Append(metadata.VtkVersion).Append('|')
                .Append(metadata.Platform).Append('|').Append(metadata.Configuration).Append('\n');
            foreach (var module in metadata.Modules.OrderBy(item => item.Module, StringComparer.Ordinal))
            {
                text.Append("metadata-module:").Append(module.Module).Append('|')
                    .Append(module.Implementable).Append('|').Append(module.LibraryName).Append('|')
                    .Append(module.NeedsAutoinit).Append('\n');
                AppendMetadataList(text, "depends:", module.Depends);
                AppendMetadataList(text, "private-depends:", module.PrivateDepends);
                AppendMetadataList(text, "optional-depends:", module.OptionalDepends);
                AppendMetadataList(text, "implements:", module.Implements);
            }
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()))).ToLowerInvariant();
    }

    private static void AppendMetadataList(StringBuilder text, string label, IEnumerable<string> values)
    {
        foreach (var value in values.Order(StringComparer.Ordinal))
            text.Append(label).Append(value).Append('\n');
    }

    private static bool IsValidTargetName(string target)
        => !string.IsNullOrWhiteSpace(target)
           && target.All(static character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-');

    private static string ToTargetKey(string target)
        => target.Replace('.', '_').Replace('-', '_');
}

public sealed record NativeTargetLayout(
    string Name,
    IReadOnlyList<string> Modules,
    IReadOnlyList<string> InitializationProviders)
{
    public string Key => this.Name.Replace('.', '_').Replace('-', '_');
}
