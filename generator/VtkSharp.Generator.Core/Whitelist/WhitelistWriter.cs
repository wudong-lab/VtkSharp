using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VtkSharp.Generator.Core.Whitelist;

public sealed class WhitelistWriter
{
    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .WithIndentedSequences()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .DisableAliases()
        .Build();

    public void WriteDirectory(string directory, IReadOnlyList<WhitelistDocument> documents)
    {
        Directory.CreateDirectory(directory);
        foreach (var document in documents)
        {
            var path = Path.Combine(directory, $"{document.Module}.yml");
            WriteFile(path, document);
        }
    }

    public void WriteFile(string path, WhitelistDocument document)
    {
        var text = "# yaml-language-server: $schema=../schemas/vtksharp.whitelist.schema.json"
                   + Environment.NewLine
                   + Environment.NewLine
                   + _serializer.Serialize(document);
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
