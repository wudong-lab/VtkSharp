# Unsafe 类接口与主要应用场景

`Unsafe` 通常指：

```csharp
System.Runtime.CompilerServices.Unsafe
```

它是 .NET 中非常底层的工具类，主要用于绕过部分托管类型系统和内存安全检查，直接做引用重解释、地址偏移、非托管指针读写、内存块复制等操作。

如果说 `Marshal` 是托管代码和非托管代码之间的边界工具，那么 `Unsafe` 更像是托管代码内部的底层内存工具。它可以写出非常高性能的代码，也可以写出非常难排查的崩溃。

简单区分：

```text
Marshal       ：托管代码 <-> 非托管代码边界
GCHandle      ：托管对象生命周期和 GC 边界控制
MemoryMarshal ：Span/Memory 体系内相对受控的内存视图转换
Unsafe        ：托管代码内部绕过安全护栏的底层操作
```

## 1. `Unsafe` 的定位

`Unsafe` 主要用于：

- 高性能库内部实现。
- `Span<T>` / `Memory<T>` 相关底层操作。
- 自定义容器、内存池、buffer adapter。
- 二进制协议解析。
- 序列化和反序列化。
- 图形、几何、VTK 数据数组适配。
- 避免装箱、减少拷贝。
- 泛型代码中做底层类型大小、引用偏移和内存复制。

它不适合普通业务代码，也不应该散落在 WPF ViewModel、CAD 命令逻辑或 UI 交互层中。比较好的做法是把它限制在非常小的底层模块中，例如：

- `NativeInterop`
- `BufferAdapter`
- `GeometryMemory`
- `VtkArrayBridge`
- `BinaryReaderCore`

## 2. .NET Framework 4.8 中能否使用

`Unsafe` 可以在 .NET Framework 4.8 中使用，但通常需要安装 NuGet 包：

```powershell
Install-Package System.Runtime.CompilerServices.Unsafe
```

然后引用命名空间：

```csharp
using System.Runtime.CompilerServices;
```

需要注意：

- 它不是 .NET Framework 4.8 原生内置 API。
- 可能会和 `System.Memory`、`System.Buffers` 等包一起出现版本依赖。
- 老 WPF / .NET Framework 项目可能需要 binding redirect。
- 生产项目中应尽量把 `Unsafe` 封装在少量底层类型里，避免版本和运行时问题扩散。

如果只是普通 P/Invoke、COM、非托管内存分配、结构体转换，应优先使用 `Marshal`、`GCHandle`、`SafeHandle`。

## 3. 常用接口

常见接口包括：

```csharp
Unsafe.As<TFrom, TTo>(ref TFrom source)
Unsafe.AsRef<T>(void* source)
Unsafe.Add<T>(ref T source, int elementOffset)
Unsafe.Subtract<T>(ref T source, int elementOffset)
Unsafe.ByteOffset<T>(ref T origin, ref T target)
Unsafe.SizeOf<T>()
Unsafe.Read<T>(void* source)
Unsafe.ReadUnaligned<T>(void* source)
Unsafe.Write<T>(void* destination, T value)
Unsafe.WriteUnaligned<T>(void* destination, T value)
Unsafe.Copy<T>(void* destination, ref T source)
Unsafe.CopyBlock(void* destination, void* source, uint byteCount)
Unsafe.InitBlock(void* startAddress, byte value, uint byteCount)
```

这些接口多数绕过了普通 C# 的安全检查，使用时需要自己保证类型、地址、对齐、生命周期和边界都是正确的。

## 4. 类型重解释：`Unsafe.As`

`Unsafe.As<TFrom, TTo>` 可以把一个类型的引用重解释为另一个类型的引用：

```csharp
int value = 0x12345678;
ref byte firstByte = ref Unsafe.As<int, byte>(ref value);
```

这表示把 `int` 的同一段内存当作 `byte` 来访问。

适用场景：

- 高性能二进制解析。
- 避免拷贝地查看数据底层字节。
- 泛型底层代码中做类型转换。
- 特定结构体布局转换。

风险：

- 类型布局不兼容会读到错误数据。
- 引用类型重解释可能破坏 GC 对对象引用的理解。
- 字节序、对齐和平台差异需要自己处理。

一般建议：如果目标是把 `Span<T>` 转成 `Span<byte>`，优先使用 `MemoryMarshal.AsBytes`，不要直接手写 `Unsafe.As`。

```csharp
Span<double> values = stackalloc double[3];
Span<byte> bytes = MemoryMarshal.AsBytes(values);
```

## 5. 引用偏移：`Unsafe.Add`

`Unsafe.Add<T>` 可以在 `ref T` 基础上按元素大小移动：

```csharp
ref double first = ref values[0];
ref double second = ref Unsafe.Add(ref first, 1);
```

它移动的是一个 `T` 元素，而不是一个字节。对于 `double`，`Unsafe.Add(ref first, 1)` 等价于地址前进 `sizeof(double)` 个字节。

适用场景：

- 高性能循环。
- 自定义数组访问器。
- 跳过边界检查的内部实现。
- 对连续 buffer 做底层遍历。

风险：

- 不做边界检查。
- 越界后不会得到普通 C# 异常，而可能读写错误内存。
- 如果底层对象被 GC 移动，错误引用可能变得无效。

除非有明确性能瓶颈，否则普通数组访问和 `Span<T>` 索引更容易维护。

## 6. 指针读写：`Read` / `Write`

`Unsafe` 可以从非托管指针读取或写入值：

```csharp
unsafe
{
    int value = Unsafe.Read<int>(ptr);
    Unsafe.Write(ptr, value);
}
```

对于可能未对齐的内存，应使用：

```csharp
unsafe
{
    int value = Unsafe.ReadUnaligned<int>(ptr);
    Unsafe.WriteUnaligned(ptr, value);
}
```

适用场景：

- 二进制协议解析。
- native buffer 读取。
- 图形 buffer 打包。
- 与固定布局结构体交互。

注意：这类代码必须自己保证 `ptr` 有效、长度足够、类型大小正确。否则可能直接导致进程崩溃。

## 7. 内存块操作：`CopyBlock` / `InitBlock`

常用接口：

```csharp
unsafe
{
    Unsafe.CopyBlock(destination, source, byteCount);
    Unsafe.InitBlock(destination, 0, byteCount);
}
```

它们类似 C/C++ 中的 `memcpy` / `memset`。

适用场景：

- 大块二进制 buffer 拷贝。
- 初始化非托管内存。
- 图形顶点 buffer 打包。
- 自定义内存池内部实现。

风险：

- byteCount 错误会覆盖错误内存。
- source 和 destination 生命周期必须有效。
- 内存重叠时要确认语义，避免得到不可预期结果。

在普通托管数组之间拷贝时，优先考虑：

```csharp
Array.Copy(...)
Buffer.BlockCopy(...)
Span<T>.CopyTo(...)
```

只有在底层性能路径中才考虑 `Unsafe.CopyBlock`。

## 8. `SizeOf<T>` 与泛型底层代码

`Unsafe.SizeOf<T>()` 可以得到类型 `T` 的大小：

```csharp
int size = Unsafe.SizeOf<double>();
```

它常用于泛型 buffer 代码：

```csharp
int byteCount = Unsafe.SizeOf<T>() * length;
```

注意它和 `Marshal.SizeOf<T>()` 的关注点不同：

- `Unsafe.SizeOf<T>()` 更偏运行时内存大小。
- `Marshal.SizeOf<T>()` 更偏互操作 marshal 后的非托管表示大小。

对于 blittable struct，二者通常一致；但对于含有 `bool`、`char`、引用类型、字符串、数组等字段的结构体，不能简单假设一致。

## 9. 与 `Marshal` 的区别

`Marshal` 更偏互操作边界：

```csharp
IntPtr ptr = Marshal.AllocHGlobal(1024);
Marshal.Copy(array, 0, ptr, array.Length);
MyStruct value = Marshal.PtrToStructure<MyStruct>(ptr);
```

它关注：

- native 指针。
- 非托管内存分配和释放。
- P/Invoke。
- COM。
- 字符串编码。
- 结构体布局。
- native 生命周期。

`Unsafe` 更偏托管内部底层操作：

```csharp
ref T item = ref Unsafe.Add(ref first, index);
```

它关注：

- 引用重解释。
- 手动地址偏移。
- 跳过边界检查。
- 指针读写。
- 内存块复制。
- 高性能泛型代码。

一句话：

```text
Marshal 是跨托管/非托管边界的工具。
Unsafe 是在托管世界里打开安全护栏的工具。
```

## 10. 与 `MemoryMarshal` 的区别

`MemoryMarshal` 位于：

```csharp
System.Runtime.InteropServices.MemoryMarshal
```

它通常服务于 `Span<T>` / `Memory<T>` 体系：

```csharp
Span<double> values = stackalloc double[3];
Span<byte> bytes = MemoryMarshal.AsBytes(values);
```

相对于 `Unsafe`，`MemoryMarshal` 的意图更明确，约束也更清晰。很多场景下应优先使用 `MemoryMarshal`：

- `Span<T>` 转 `Span<byte>`。
- 从 `ReadOnlySpan<byte>` 读取结构体。
- 托管连续内存的视图转换。
- 避免不必要的拷贝。

只有当 `MemoryMarshal` 表达不了需求，或者确实需要更底层的泛型引用操作时，再考虑 `Unsafe`。

推荐优先级：

```text
普通托管代码：不用底层 API
托管连续内存视图转换：MemoryMarshal
native 互操作边界：Marshal / GCHandle / SafeHandle
极限性能或库级基础设施：Unsafe
```

## 11. 与 `GCHandle` 的区别

`GCHandle` 用于控制托管对象和 GC 的关系：

```csharp
GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Normal);
IntPtr token = GCHandle.ToIntPtr(handle);
```

常见用途：

- 保活托管对象。
- 将托管对象作为 opaque token 传给 native。
- pin 托管数组，让 native 临时访问其固定地址。

`Unsafe` 不负责对象生命周期。它可以拿到引用、偏移引用、重解释引用，但不能替代 `GCHandle` 的保活和 pinning 语义。

特别注意：不要因为用了 `Unsafe` 就假设对象不会被 GC 移动。native 如果要保存托管内存地址，必须重新设计生命周期，通常需要 pinning、复制到非托管内存，或者使用明确的 native-owned buffer。

## 12. 工程软件中的典型应用

### 12.1 几何数据 buffer

例如大量二维/三维点：

```text
x0, y0, z0, x1, y1, z1, ...
```

底层 adapter 可能需要把 `Span<Point3D>` 转成字节视图，或者高效写入 native / VTK buffer。优先考虑 `MemoryMarshal.AsBytes`，只有在必要时用 `Unsafe.Add` 做手动遍历。

### 12.2 VTK 数据数组适配

VTK 常见的是连续数值数组。使用 `Unsafe` 时要确认：

- 元素类型是否匹配 VTK array 类型。
- VTK 是复制数据，还是引用外部内存。
- 外部 buffer 生命周期是否覆盖 VTK 使用周期。
- 坐标维度和 stride 是否正确。
- 是否存在 double / float 精度转换。

这里最容易出的问题不是编译错误，而是渲染异常、数据错位或偶发崩溃。

### 12.3 CAD 二次开发

CAD 插件中一般不建议在命令逻辑层直接使用 `Unsafe`。更适合的边界是：

- 与 native 几何内核的数据适配层。
- 大量实体坐标的批量转换层。
- 二进制文件格式解析层。

同时仍要遵守 CAD 宿主约束：

- 文档锁。
- 事务。
- 对象生命周期。
- 撤销栈。
- UI 线程和宿主 API 线程模型。

`Unsafe` 不能绕过这些宿主规则。

### 12.4 WPF / DevExpress 桌面程序

UI 层不应直接看到 `Unsafe`。推荐形式：

```csharp
public interface IGeometryBufferWriter
{
    void Write(ReadOnlySpan<double> coordinates);
}
```

具体实现内部可以使用 `MemoryMarshal` 或 `Unsafe`。ViewModel 只依赖普通托管接口。

## 13. 常见风险

### 13.1 越界访问

`Unsafe.Add` 不做边界检查：

```csharp
ref double item = ref Unsafe.Add(ref first, index);
```

如果 `index` 错了，可能读写到错误内存。

### 13.2 类型重解释错误

```csharp
ref SomeStruct s = ref Unsafe.As<byte, SomeStruct>(ref firstByte);
```

如果字节长度、对齐、字段布局、字节序不正确，读取结果就是错误的。

### 13.3 破坏 GC 假设

错误地重解释包含对象引用的结构，可能让 GC 无法正确追踪引用，造成非常隐蔽的问题。

### 13.4 悬垂引用

如果引用指向的对象生命周期已经结束，或者指向 stackalloc 内存但逃逸出了作用域，后续访问就是未定义风险。

### 13.5 对齐问题

某些平台或指令对未对齐访问敏感。处理二进制协议或网络数据时，应考虑 `ReadUnaligned` / `WriteUnaligned`。

### 13.6 平台差异

需要关注：

- x86 / x64 指针大小。
- 字节序。
- struct packing。
- CLR 版本差异。
- .NET Framework 和现代 .NET API 差异。

## 14. 使用建议

在 .NET Framework 4.8 或现代 .NET 项目中，可以按这个顺序选择工具：

```text
普通逻辑：
  普通 C#、数组、List<T>、Span<T>

托管 buffer 视图转换：
  Span<T>、MemoryMarshal

native / COM / P/Invoke：
  Marshal、GCHandle、SafeHandle

明确性能瓶颈的底层代码：
  Unsafe
```

使用 `Unsafe` 时建议遵守：

- 把代码限制在少量底层类型中。
- 对外暴露普通托管 API。
- 明确写出长度、stride、元素大小、所有权。
- 对边界条件、空数组、退化几何、大数组做测试。
- 不要在 UI 层、命令层、业务层直接使用。
- 不要为了“看起来高级”引入它。

## 15. 简短结论

`Unsafe` 确实是 .NET 中执行底层操作的重要类，但它不是 `Marshal` 的替代品。

- `Marshal` 解决托管 / 非托管边界。
- `GCHandle` 解决托管对象生命周期和 pinning。
- `MemoryMarshal` 解决 `Span<T>` / `Memory<T>` 内的视图转换。
- `Unsafe` 解决更底层、更自由、也更危险的引用和内存操作。

在工程软件开发中，`Unsafe` 的合理位置通常是几何 buffer、VTK 数据桥接、二进制解析、native adapter 这类底层模块。只要能用 `Span<T>`、`MemoryMarshal`、`Marshal` 清楚表达，就不要优先使用 `Unsafe`。当确实需要它时，范围要小、边界要清楚、测试要覆盖异常尺寸和退化数据。
