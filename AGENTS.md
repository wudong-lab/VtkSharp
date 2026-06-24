# VtkSharp Agent Notes

请默认用中文与用户交流，除非用户明确要求英文。

## 生成器实现注意事项

- `generator` 项目使用 YamlDotNet 反序列化 YAML 配置和白名单模型。
- 供 YamlDotNet 直接反序列化的 DTO/record 属性，集合类型优先使用具体可变类型，例如 `List<T>`、`Dictionary<TKey, TValue>`。
- 不要在 YAML DTO 上使用 `IReadOnlyList<T>`、`IReadOnlyDictionary<TKey, TValue>` 作为 `init` 属性类型，否则 YamlDotNet 可能无法构造并填充集合。
- 对外只读需求可以在业务层转换，或额外暴露只读视图，不要牺牲反序列化稳定性。
