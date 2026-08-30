using System.Text.Json;
using YamlDotNet.Serialization;

namespace VtkSharp.Generator.Core.Whitelist;

/// <summary>已确认的托管枚举契约；NativeType 始终保留原生签名类型。</summary>
public sealed record EnumProperty
{
    public string Name { get; init; } = "";
    public string NativeType { get; init; } = "int";
    public string Getter { get; init; } = "";
    public string Setter { get; init; } = "";
    public List<EnumValue> Values { get; init; } = [];
    public List<string> ConvenienceMethods { get; init; } = [];

    [YamlIgnore, System.Text.Json.Serialization.JsonIgnore]
    public IEnumerable<string> Methods => new[] { Getter, Setter }.Concat(ConvenienceMethods);

    public bool SameContract(EnumProperty other) => JsonSerializer.Serialize(this) == JsonSerializer.Serialize(other);

    public WhitelistFunction ToAbiFunction(WhitelistFunction function)
        => function.Name == Getter ? function with { Return = function.Return with { Type = "int" } }
         : function.Name == Setter ? function with
         {
             Parameters = function.Parameters.Select(p => p with { Type = "int" }).ToList(),
         } : function;
}

public sealed record EnumValue
{
    public string Name { get; init; } = "";
    public string NativeExpression { get; init; } = "";
    public int Value { get; init; }
}
