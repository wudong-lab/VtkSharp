namespace VtkSharp.Generator.Core.Generation;

public static class GeneratedManifestCache
{
    public static bool TryGetReusableEntry(
        GeneratedManifest manifest,
        string className,
        string inputHash,
        string managedPath,
        string nativePath,
        out GeneratedManifestEntry? entry)
    {
        entry = manifest.Classes.FirstOrDefault(item => item.ClassName.Equals(className, StringComparison.Ordinal));
        if (entry is null ||
            !entry.InputHash.Equals(inputHash, StringComparison.Ordinal) ||
            !File.Exists(managedPath) ||
            !File.Exists(nativePath))
        {
            return false;
        }

        var managedHash = GenerationInputFingerprint.HashFileText(managedPath);
        var nativeHash = GenerationInputFingerprint.HashFileText(nativePath);
        return entry.ManagedContentHash.Equals(managedHash, StringComparison.Ordinal) &&
               entry.NativeContentHash.Equals(nativeHash, StringComparison.Ordinal);
    }
}
