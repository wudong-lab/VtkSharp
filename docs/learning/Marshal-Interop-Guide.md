# Marshal 类接口与主要应用场景

`Marshal` 是 .NET 中用于托管代码与非托管代码互操作的核心类型，位于：

```csharp
using System.Runtime.InteropServices;
```

它主要解决的问题是：托管对象由 CLR 和 GC 管理，而 Win32 API、C/C++ DLL、COM 组件、CAD 宿主 API 等通常使用非托管指针、结构体、字符串和手动生命周期。`Marshal` 负责在这两个世界之间搬运数据、转换表示、分配和释放非托管内存。

在工程软件、CAD 二次开发、几何计算和 C# / C++ 混合开发中，`Marshal` 很常见，但也很容易误用。使用它时最重要的不是记住某个 API，而是先明确这些约束：

- 内存由谁分配、谁释放。
- 结构体布局是否与 native 端一致。
- 字符串编码是 UTF-16、UTF-8、ANSI 还是 BSTR。
- 调用约定是 `cdecl`、`stdcall` 还是其他形式。
- native 是否会保存指针、回调或托管对象引用。
- 数据量是否足够大，是否需要避免逐项 marshal。

## 1. `Marshal` 的定位

`Marshal` 主要面向这些场景：

- P/Invoke 调用 Win32 API 或 C/C++ DLL。
- 托管结构体与 native 结构体互转。
- 托管数组与非托管内存互拷贝。
- 分配和释放非托管内存。
- 字符串与 native 字符串指针互转。
- 委托与函数指针互转。
- COM 对象访问与释放。
- 读取、写入非托管内存中的基础类型。

它不适合普通业务代码，也不应该散落在 WPF ViewModel、业务服务或几何算法主流程中。比较好的做法是把它集中封装在 `NativeInterop`、`NativeMethods`、`GeometryNativeClient` 这类边界层中。

## 2. 非托管内存分配与释放

常用接口：

```csharp
IntPtr ptr = Marshal.AllocHGlobal(byteCount);
Marshal.FreeHGlobal(ptr);
```

以及：

```csharp
IntPtr ptr = Marshal.AllocCoTaskMem(byteCount);
Marshal.FreeCoTaskMem(ptr);
```

二者的典型区别：

- `AllocHGlobal` / `FreeHGlobal`：常用于普通 native 内存。
- `AllocCoTaskMem` / `FreeCoTaskMem`：常用于 COM 或 API 明确要求 CoTaskMem 分配器的场景。

示例：

```csharp
int byteCount = sizeof(double) * 3;
IntPtr buffer = Marshal.AllocHGlobal(byteCount);

try
{
    double[] values = [1.0, 2.0, 3.0];
    Marshal.Copy(values, 0, buffer, values.Length);

    // NativeApi.ProcessValues(buffer, values.Length);
}
finally
{
    Marshal.FreeHGlobal(buffer);
}
```

工程建议：

- 谁分配，谁释放，必须在接口设计中写清楚。
- 短生命周期可以用 `try/finally`。
- 长生命周期 native 资源建议封装为 `SafeHandle`。
- 不要把 `IntPtr.Zero` 当作有效地址传给 native。

## 3. 托管数组与非托管内存互拷贝

常用接口：

```csharp
Marshal.Copy(sourceArray, startIndex, destinationPtr, length);
Marshal.Copy(sourcePtr, destinationArray, startIndex, length);
```

常用数组类型包括：

- `byte[]`
- `short[]`
- `int[]`
- `long[]`
- `float[]`
- `double[]`
- `char[]`
- `IntPtr[]`

示例：

```csharp
double[] points =
[
    0.0, 0.0,
    1.0, 0.0,
    1.0, 1.0
];

IntPtr buffer = Marshal.AllocHGlobal(sizeof(double) * points.Length);

try
{
    Marshal.Copy(points, 0, buffer, points.Length);

    // native side sees double* xy
}
finally
{
    Marshal.FreeHGlobal(buffer);
}
```

在几何计算、CAD 坐标、VTK 顶点、网格索引、矩阵数据传输中，连续基础数组通常比结构体数组更简单、更高效。

如果数据量很大，应尽量避免逐个元素 `PtrToStructure` / `StructureToPtr`。优先考虑：

- native 接口使用 `double*`、`float*`、`int*` 等连续数组。
- C# 端使用 `Marshal.Copy`。
- 在允许 `unsafe` 的边界层中使用 `fixed` 临时固定托管数组。
- 使用 `Span<T>` / `MemoryMarshal` 在托管内存内做零拷贝视图转换。

## 4. 结构体与指针互转

常用接口：

```csharp
int size = Marshal.SizeOf<T>();
T value = Marshal.PtrToStructure<T>(ptr);
Marshal.StructureToPtr(value, ptr, fDeleteOld: false);
```

示例：

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct Point2D
{
    public double X;
    public double Y;
}
```

写入非托管内存：

```csharp
var point = new Point2D { X = 10.0, Y = 20.0 };
IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<Point2D>());

try
{
    Marshal.StructureToPtr(point, ptr, fDeleteOld: false);

    // ptr can be passed as Point2D*
}
finally
{
    Marshal.FreeHGlobal(ptr);
}
```

从非托管内存读取：

```csharp
Point2D point = Marshal.PtrToStructure<Point2D>(ptr);
```

结构体互操作的关键是布局。C# 端通常必须显式声明：

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct Segment2D
{
    public Point2D Start;
    public Point2D End;
}
```

需要特别关注：

- 字段顺序。
- 字段类型大小。
- 对齐和 padding。
- `bool` 在 C# 与 C/C++ 中的大小差异。
- `char` 和字符串字段的编码。
- x86 / x64 下指针大小差异。

对于 C# / C++ ABI 边界，建议写测试保护结构体大小和字段偏移：

```csharp
int size = Marshal.SizeOf<Segment2D>();
IntPtr endOffset = Marshal.OffsetOf<Segment2D>(nameof(Segment2D.End));
```

## 5. 读取和写入非托管内存

常用读取接口：

```csharp
byte b = Marshal.ReadByte(ptr);
short s = Marshal.ReadInt16(ptr);
int i = Marshal.ReadInt32(ptr);
long l = Marshal.ReadInt64(ptr);
IntPtr p = Marshal.ReadIntPtr(ptr);
```

常用写入接口：

```csharp
Marshal.WriteByte(ptr, value);
Marshal.WriteInt16(ptr, value);
Marshal.WriteInt32(ptr, value);
Marshal.WriteInt64(ptr, value);
Marshal.WriteIntPtr(ptr, value);
```

支持偏移：

```csharp
int value = Marshal.ReadInt32(ptr, offset);
Marshal.WriteInt32(ptr, offset, value);
```

适用场景：

- 解析 native 返回的简单二进制 buffer。
- 操作非托管结构体中的少量字段。
- 调试 P/Invoke 返回数据。
- 与只暴露 `void*` / `byte*` 的老式 C API 交互。

如果是高性能二进制解析，优先考虑 `Span<T>`、`BinaryPrimitives`、`MemoryMarshal` 或专门的序列化格式，不要在大循环中大量调用 `Marshal.ReadXxx`。

## 6. 字符串与指针转换

常用接口：

```csharp
IntPtr pAnsi = Marshal.StringToHGlobalAnsi(text);
IntPtr pUni = Marshal.StringToHGlobalUni(text);
IntPtr pUtf8 = Marshal.StringToCoTaskMemUTF8(text);

string? s1 = Marshal.PtrToStringAnsi(pAnsi);
string? s2 = Marshal.PtrToStringUni(pUni);
string? s3 = Marshal.PtrToStringUTF8(pUtf8);
```

示例：

```csharp
IntPtr ptr = Marshal.StringToHGlobalUni("图层名称");

try
{
    // native side sees wchar_t* / LPWSTR
}
finally
{
    Marshal.FreeHGlobal(ptr);
}
```

常见编码含义：

- `StringToHGlobalAnsi`：当前系统 ANSI 代码页，不建议用于中文路径和跨机器数据。
- `StringToHGlobalUni`：UTF-16，适合 Windows `wchar_t*`、`LPWSTR`。
- `StringToCoTaskMemUTF8`：UTF-8，适合现代 C API 或明确使用 UTF-8 的 C++ 接口。
- COM 中常见字符串是 `BSTR`，通常由 COM interop 自动处理。

工程软件中中文文件路径、图层名、块名、族名、材料名很常见，字符串编码不能靠猜。接口文档必须明确 native 端接收的是：

- `char*`
- `wchar_t*`
- UTF-8
- ANSI
- BSTR

## 7. 委托与函数指针

常用接口：

```csharp
IntPtr functionPointer = Marshal.GetFunctionPointerForDelegate(callback);
TDelegate callback = Marshal.GetDelegateForFunctionPointer<TDelegate>(functionPointer);
```

示例：

```csharp
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void ProgressCallback(int percent);
```

```csharp
private static ProgressCallback? s_progressCallback;

public static IntPtr CreateCallback()
{
    s_progressCallback = percent =>
    {
        Console.WriteLine(percent);
    };

    return Marshal.GetFunctionPointerForDelegate(s_progressCallback);
}
```

关键风险：传给 native 的 delegate 必须在 native 可能回调期间保持存活。否则 GC 可能回收 delegate，native 后续调用函数指针时会导致崩溃。

同时必须明确调用约定：

```csharp
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
```

它要和 C/C++ 导出或回调签名一致。调用约定不一致可能导致参数错乱、栈损坏或宿主进程崩溃。

## 8. COM 相关接口

`Marshal` 也常用于 COM 互操作：

```csharp
object obj = Marshal.GetActiveObject("AutoCAD.Application");
bool isComObject = Marshal.IsComObject(obj);
int count = Marshal.ReleaseComObject(obj);
```

常见接口：

```csharp
Marshal.GetActiveObject(progId);
Marshal.ReleaseComObject(comObject);
Marshal.FinalReleaseComObject(comObject);
Marshal.IsComObject(obj);
Marshal.GetIUnknownForObject(obj);
Marshal.GetObjectForIUnknown(ptr);
```

典型场景：

- AutoCAD COM 自动化。
- Office 自动化。
- 老式 CAD / CAE 软件 COM API。
- ActiveX 控件。
- COM 插件桥接。

COM 生命周期需要谨慎处理。`ReleaseComObject` 用不好会提前释放 RCW 背后的 COM 对象，导致其他代码继续访问时失败。一般建议：

- 明确 COM 对象的拥有边界。
- 不要把 COM 对象到处缓存。
- 避免在不确定所有权时调用 `FinalReleaseComObject`。
- 注意 STA 线程模型。
- CAD 宿主里还要注意文档锁、事务、撤销栈和宿主 API 的线程限制。

## 9. P/Invoke 中的典型组合

C++ 端：

```cpp
struct Point2D
{
    double x;
    double y;
};

extern "C" __declspec(dllexport)
void TransformPoints(Point2D* points, int count);
```

C# 端：

```csharp
[StructLayout(LayoutKind.Sequential)]
public struct Point2D
{
    public double X;
    public double Y;
}

[DllImport("GeometryNative.dll", CallingConvention = CallingConvention.Cdecl)]
private static extern void TransformPoints(IntPtr points, int count);
```

调用：

```csharp
Point2D[] points =
[
    new() { X = 0.0, Y = 0.0 },
    new() { X = 1.0, Y = 1.0 }
];

int size = Marshal.SizeOf<Point2D>();
IntPtr buffer = Marshal.AllocHGlobal(size * points.Length);

try
{
    for (int i = 0; i < points.Length; i++)
    {
        IntPtr itemPtr = IntPtr.Add(buffer, i * size);
        Marshal.StructureToPtr(points[i], itemPtr, fDeleteOld: false);
    }

    TransformPoints(buffer, points.Length);

    for (int i = 0; i < points.Length; i++)
    {
        IntPtr itemPtr = IntPtr.Add(buffer, i * size);
        points[i] = Marshal.PtrToStructure<Point2D>(itemPtr);
    }
}
finally
{
    Marshal.FreeHGlobal(buffer);
}
```

对于大量点，更推荐设计 native 接口为连续基础数组：

```cpp
extern "C" __declspec(dllexport)
void TransformXY(double* xy, int pointCount);
```

C# 端：

```csharp
double[] xy =
[
    0.0, 0.0,
    1.0, 1.0
];

IntPtr buffer = Marshal.AllocHGlobal(sizeof(double) * xy.Length);

try
{
    Marshal.Copy(xy, 0, buffer, xy.Length);
    TransformXY(buffer, xy.Length / 2);
    Marshal.Copy(buffer, xy, 0, xy.Length);
}
finally
{
    Marshal.FreeHGlobal(buffer);
}
```

这个形式更适合几何点、曲线采样点、网格顶点、VTK 数据数组等大批量数值数据。

## 10. `Marshal`、`GCHandle`、`MemoryMarshal` 的区别

三者容易混淆：

### `Marshal`

用于托管 / 非托管边界：

```csharp
Marshal.AllocHGlobal(...);
Marshal.Copy(...);
Marshal.PtrToStructure<T>(...);
```

关注 native 指针、非托管内存、COM、P/Invoke。

### `GCHandle`

用于显式控制托管对象在 GC 视角下的可达性或固定状态：

```csharp
GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Normal);
IntPtr token = GCHandle.ToIntPtr(handle);
```

关注托管对象生命周期桥接、native 回调上下文、托管对象 pinning。

### `MemoryMarshal`

用于托管内存内部的高性能类型转换：

```csharp
Span<byte> bytes = MemoryMarshal.AsBytes(values.AsSpan());
```

关注 `Span<T>`、零拷贝、二进制视图和高性能解析。它通常不负责 native 内存生命周期。

简单判断：

- 要处理 native 指针和非托管内存：优先看 `Marshal`。
- 要把托管对象作为 opaque token 传给 native：优先看 `GCHandle`。
- 要在托管内存中 reinterpret span：优先看 `MemoryMarshal`。
- 要让 native 长期持有资源句柄：优先封装 `SafeHandle`。

## 11. 常见风险

### 11.1 内存泄漏

```csharp
IntPtr ptr = Marshal.AllocHGlobal(1024);
// missing Marshal.FreeHGlobal(ptr)
```

只要手动分配了非托管内存，就必须确保释放。异常路径也要覆盖。

### 11.2 结构体布局不一致

C++：

```cpp
struct Data
{
    int id;
    double value;
};
```

C#：

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 8)]
public struct Data
{
    public int Id;
    public double Value;
}
```

`int` 后面可能存在 padding。不要只凭字段个数判断结构体大小，应该用 `sizeof`、`Marshal.SizeOf` 和字段偏移测试验证。

### 11.3 字符串编码错误

中文路径、图层名、块名、对象名称非常容易出乱码。不要把 `StringToHGlobalAnsi` 当作默认选择。Windows native API 多数宽字符版本应使用 UTF-16。

### 11.4 调用约定不匹配

C++ 的 `__cdecl`、`__stdcall` 和 C# 的 `CallingConvention` 必须一致。否则问题可能不是返回错误码，而是直接栈损坏。

### 11.5 Delegate 被 GC 回收

通过 `Marshal.GetFunctionPointerForDelegate` 传给 native 的 delegate 必须被 C# 持有引用，直到 native 不再可能调用。

### 11.6 指针大小错误

不要把指针转成 `int` 保存。应使用：

```csharp
IntPtr ptr;
nint value;
```

CAD、CAE、图形软件通常是 x64 进程，指针大小问题尤其重要。

### 11.7 native 保存临时指针

如果 native 只在函数调用期间使用指针，可以用临时 buffer 或 `fixed`。如果 native 会保存指针，必须重新设计生命周期，不能把临时托管数组地址交给 native 长期保存。

## 12. 在工程软件中的推荐实践

### C# 调用 C++ 几何内核

优先传递连续数值数组：

- `double*` 表示二维/三维点坐标。
- `int*` 表示索引。
- `float*` 表示渲染顶点。
- `double[16]` 表示矩阵。

少量配置参数可以用结构体，大批量几何数据尽量避免逐项结构体 marshal。

### CAD 二次开发

重点关注：

- 坐标系和单位。
- 图层、块、实体生命周期。
- 事务和文档锁。
- COM 对象生命周期。
- 宿主程序线程模型。
- 撤销栈和选择集约束。

`Marshal` 只能解决互操作数据边界问题，不能绕过 CAD 宿主 API 的线程和事务规则。

### WPF / DevExpress 桌面程序

建议让 UI 层只依赖托管服务接口：

```csharp
public interface IGeometryKernel
{
    IReadOnlyList<Point2D> Transform(IReadOnlyList<Point2D> points);
}
```

`Marshal` 相关实现放在 native adapter 层。这样可以避免 ViewModel 和 UI 线程中混入非托管资源管理细节。

### VTK / 图形数据

对于点、法线、颜色、索引等数组，优先使用连续内存模型。需要注意：

- 元素类型是否匹配 VTK 数据数组类型。
- 坐标维度是 2D、3D 还是齐次坐标。
- native 是否复制数据，还是只引用外部 buffer。
- 如果只引用外部 buffer，外部 buffer 生命周期必须覆盖 VTK 使用周期。

## 13. 简短结论

`Marshal` 的核心价值是把托管世界和非托管世界连接起来。它最常用于 P/Invoke、COM、C# / C++ 混合开发、CAD 插件、几何计算和图形数据传输。

使用时优先记住这些规则：

- 小量结构体可以用 `PtrToStructure<T>` / `StructureToPtr`。
- 大量数值数据优先用连续数组、`Marshal.Copy` 或 `fixed`。
- 非托管内存必须明确分配和释放方。
- 结构体必须显式声明布局，并验证大小和偏移。
- 字符串编码必须和 native 端约定一致。
- 回调 delegate 必须保活。
- COM 对象释放要谨慎，不能随意 `FinalReleaseComObject`。
- 高性能托管内存转换优先考虑 `MemoryMarshal`，不要把 `Marshal` 当成万能二进制工具。

在工程软件开发中，`Marshal` 相关问题通常不会表现为普通异常，而是数据错位、中文乱码、随机崩溃、宿主进程退出或难以复现的内存错误。因此互操作边界应尽量薄、清晰、可测试。
