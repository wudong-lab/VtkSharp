namespace VtkSharp.Generator.Core.Generation;

public sealed record GeneratedOutputDifference(string RelativePath, string Message);

public sealed class GeneratedOutputComparer
{
    public IReadOnlyList<GeneratedOutputDifference> CompareDirectories(string expectedDirectory, string actualDirectory, string searchPattern)
    {
        var differences = new List<GeneratedOutputDifference>();
        var expectedFiles = GetRelativeFiles(expectedDirectory, searchPattern);
        var actualFiles = GetRelativeFiles(actualDirectory, searchPattern);

        foreach (var relativePath in expectedFiles.Keys.Except(actualFiles.Keys, StringComparer.Ordinal))
            differences.Add(new GeneratedOutputDifference(relativePath, "Missing from generated output."));

        foreach (var relativePath in actualFiles.Keys.Except(expectedFiles.Keys, StringComparer.Ordinal))
            differences.Add(new GeneratedOutputDifference(relativePath, "Only exists in generated output."));

        foreach (var relativePath in expectedFiles.Keys.Intersect(actualFiles.Keys, StringComparer.Ordinal))
        {
            var expected = File.ReadAllText(expectedFiles[relativePath]);
            var actual = File.ReadAllText(actualFiles[relativePath]);
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                differences.Add(new GeneratedOutputDifference(relativePath, "Content differs."));
        }

        return differences
            .OrderBy(difference => difference.RelativePath, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<GeneratedOutputDifference> CompareFiles(string expectedPath, string actualPath, string relativePath)
    {
        if (!File.Exists(expectedPath) && !File.Exists(actualPath))
            return [];

        if (!File.Exists(expectedPath))
            return [new GeneratedOutputDifference(relativePath, "Missing from current output.")];

        if (!File.Exists(actualPath))
            return [new GeneratedOutputDifference(relativePath, "Missing from generated output.")];

        var expected = File.ReadAllText(expectedPath);
        var actual = File.ReadAllText(actualPath);
        return string.Equals(expected, actual, StringComparison.Ordinal)
            ? []
            : [new GeneratedOutputDifference(relativePath, "Content differs.")];
    }

    private static Dictionary<string, string> GetRelativeFiles(string directory, string searchPattern)
    {
        if (!Directory.Exists(directory))
            return new Dictionary<string, string>(StringComparer.Ordinal);

        return Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetRelativePath(directory, path).Replace('\\', '/'),
                path => path,
                StringComparer.Ordinal);
    }
}
