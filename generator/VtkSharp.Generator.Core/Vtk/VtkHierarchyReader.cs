using System.Text.RegularExpressions;

namespace VtkSharp.Generator.Core.Vtk;

public sealed partial class VtkHierarchyReader
{
    public IReadOnlyDictionary<string, VtkHierarchyEntry> ReadDirectory(string directory)
    {
        var result = new Dictionary<string, VtkHierarchyEntry>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(directory, "vtk*-hierarchy.txt", SearchOption.TopDirectoryOnly))
        {
            foreach (var entry in ReadFile(file))
            {
                result.TryAdd(entry.ClassName, entry);
            }
        }

        return result;
    }

    public IReadOnlyList<VtkHierarchyEntry> ReadFile(string path)
    {
        return File.ReadLines(path)
            .Select(ParseLine)
            .Where(static entry => entry is not null)
            .Select(static entry => entry!)
            .ToList();
    }

    private static VtkHierarchyEntry? ParseLine(string line)
    {
        var match = ClassLineRegex().Match(line.Trim());
        if (!match.Success)
            return null;

        return new VtkHierarchyEntry(
            match.Groups["class"].Value,
            match.Groups["base"].Value,
            match.Groups["header"].Value,
            match.Groups["module"].Value);
    }

    [GeneratedRegex(@"^\s*(?<class>vtk[\w-]+)\s*:\s*(?<base>vtk[\w-]+)\s*;\s*(?<header>vtk[\w-]+\.h)\s*;\s*(?<module>vtk[\w-]+)\s*$")]
    private static partial Regex ClassLineRegex();
}
