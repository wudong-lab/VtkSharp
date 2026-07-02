using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VtkSharp.Generator.Core.Whitelist;

public sealed class WhitelistLoader
{
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public IReadOnlyList<WhitelistDocument> LoadDirectory(string directory)
    {
        var files = Directory.GetFiles(directory, "vtk*.yml", SearchOption.TopDirectoryOnly)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return files.Select(LoadFile).ToList();
    }

    public WhitelistDocument LoadFile(string path)
    {
        using var reader = File.OpenText(path);
        var document = _deserializer.Deserialize<WhitelistDocument>(reader);
        return document with
        {
            Classes = document.Classes.Select(whitelistClass => whitelistClass with
            {
                Functions = whitelistClass.Functions ?? [],
            }).ToList(),
        };
    }
}
