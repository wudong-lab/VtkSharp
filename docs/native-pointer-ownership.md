# Native 指针封装与所有权

VtkSharp wrapper 提供两个 public 静态工厂方法，用于把外部获得的 VTK 对象指针封装为对应的 C# 类型：

```csharp
vtkPolyData borrowed = vtkPolyData.FromBorrowedPointer(nativePointer);
vtkPolyData owned = vtkPolyData.TakeReference(nativePointer);
```

两者都不会调用 `Register()`。区别在于 wrapper 是否接管传入指针当前已有的一份 VTK 引用。

## `FromBorrowedPointer`

`FromBorrowedPointer` 创建不拥有引用的 wrapper，适用于 native 调用方仍保有对象所有权、只把指针临时借给 C# 使用的场景。

```csharp
vtkRenderer renderer = vtkRenderer.FromBorrowedPointer(nativeRenderer);
renderer.SetBackground(0.1, 0.2, 0.3);

// Dispose 不会对借用的 native 引用调用 Delete/UnRegister。
renderer.Dispose();
```

使用要求：

- native 所有者必须在 wrapper 的整个使用期内保持对象存活。
- wrapper 的 `Dispose()` 不延长或结束 native 对象生命周期，也不会把 `NativePointer` 清零。
- native 对象释放后不得再访问 wrapper；VtkSharp 无法检测悬空指针。
- 如果需要让 wrapper 独立于原所有者存活，应先为该指针取得一份引用，再按所有权契约封装；不要把借用指针直接传给 `TakeReference`。

## `TakeReference`

`TakeReference` 接管传入指针当前已有的一份引用，适用于 native API 明确把一份 VTK 引用转移给调用方的场景。它不会增加引用计数；wrapper 在 `Dispose()` 或 `Delete()` 时释放所接管的引用。

```csharp
nint nativePolyData = CreatePolyData(); // 契约：返回一份由调用方负责释放的引用

using vtkPolyData polyData = vtkPolyData.TakeReference(nativePolyData);
// 使用 polyData；离开作用域时释放 CreatePolyData 转移的那份引用。
```

使用要求：

- 只有在调用方确实拥有一份可转移引用时才能使用。
- 调用后，该引用的释放责任转移给 wrapper；原调用方不得再释放同一份引用。
- 同一份引用不能交给多个 owning wrapper，否则会重复释放。
- 如果 native API 返回借用指针，应使用 `FromBorrowedPointer`；只有先按 VTK 规则增加引用计数后，才能把新增的那份引用交给 `TakeReference`。

## 通用注意事项

- `nativePointer` 必须非零，并指向仍然存活、且与所选 wrapper 类型兼容的 VTK 对象。
- 应使用指针实际类型对应的最具体 wrapper 工厂。例如 `vtkPolyData*` 使用 `vtkPolyData.TakeReference(...)`，不要仅因其继承自 `vtkObject` 就使用 `vtkObject.TakeReference(...)`。
- 工厂方法不验证 native 对象的动态类型。类型不匹配属于 ABI 误用，可能导致 native 崩溃或内存破坏。
- `OwnsReference` 可用于确认 wrapper 是否会释放引用，但不能证明外部所有权契约正确。
- 不确定 native API 的返回值是 borrowed reference 还是 transferred reference 时，应先查明其 VTK 引用计数契约，不要根据方法名猜测。

简化的选择规则如下：

```text
native API 是否把一份现有引用的释放责任交给当前调用方？
├─ 否：FromBorrowedPointer；native 所有者必须比 wrapper 活得更久
└─ 是：TakeReference；wrapper 接管并最终释放这一份引用
```
