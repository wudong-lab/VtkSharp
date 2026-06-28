# VtkSharp Agent Notes

请默认用中文与用户交流，除非用户明确要求英文。

## 生成器实现注意事项

- `generator` 项目使用 YamlDotNet 反序列化 YAML 配置和白名单模型。
- 供 YamlDotNet 直接反序列化的 DTO/record 属性，集合类型优先使用具体可变类型，例如 `List<T>`、`Dictionary<TKey, TValue>`。
- 不要在 YAML DTO 上使用 `IReadOnlyList<T>`、`IReadOnlyDictionary<TKey, TValue>` 作为 `init` 属性类型，否则 YamlDotNet 可能无法构造并填充集合。
- 对外只读需求可以在业务层转换，或额外暴露只读视图，不要牺牲反序列化稳定性。
- generator 只为当前 VTK 类直接声明的 public 实例成员函数生成导出；继承自基类但当前类未重新声明的函数，不在当前类重复导出，C# 端通过继承调用基类 wrapper。
- 如果当前类声明了同名函数，应按 C++ 名称隐藏规则理解候选函数，不应把被隐藏的基类 overload 当作当前类可直接调用的函数。
- C# wrapper 的调用语义应尽量等价于 C++ 通过对应静态类型指针/引用调用；是否分派到子类实现由 C++ virtual dispatch 决定。
- VTK 静态函数中只特殊导出 `static New()`；其他 static 函数默认忽略。C++ 构造函数、析构函数不导出。
- 候选函数列表、白名单校验和最终生成必须使用一致的函数可导出规则，避免候选/校验通过但 native 编译失败。
