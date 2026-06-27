# GCHandle 在 Interop 场景中的用法与应用场景

`GCHandle` 是 .NET 提供的底层运行时 API，主要用于在托管对象和非托管代码之间建立显式生命周期桥接。它在 C# 与 C/C++ 互操作、native 回调、CAD SDK 插件、图形和工程计算数据交换等场景中非常有用。

但它也容易误用。`GCHandle` 不是普通业务代码的首选工具，而是当托管对象需要跨越 GC 管理边界，被 native 代码间接保存、回调或访问时才应使用。

## 1. 核心概念

命名空间：

```csharp
using System.Runtime.InteropServices;
```

创建一个 `GCHandle`：

```csharp
GCHandle handle = GCHandle.Alloc(obj, GCHandleType.Normal);
```

释放：

```csharp
handle.Free();
```

访问目标对象：

```csharp
object? target = handle.Target;
```

将 `GCHandle` 转成 `IntPtr`，传给 native：

```csharp
IntPtr ptr = GCHandle.ToIntPtr(handle);
```

从 `IntPtr` 恢复：

```csharp
GCHandle handle = GCHandle.FromIntPtr(ptr);
object? target = handle.Target;
```

需要特别注意：`GCHandle.ToIntPtr(handle)` 得到的不是托管对象地址，而是一个可传回 .NET 的 opaque token。native 代码不能把它当作对象指针解引用。

## 2. GCHandleType

`GCHandleType` 主要有四种：

```csharp
GCHandleType.Weak
GCHandleType.WeakTrackResurrection
GCHandleType.Normal
GCHandleType.Pinned
```

工程中最常用的是 `Normal` 和 `Pinned`。

## 3. Normal：保活托管对象

```csharp
var handle = GCHandle.Alloc(obj, GCHandleType.Normal);
```

`Normal` 的作用是让 GC 认为该对象仍然可达，因此不会被回收。但对象仍然可以被 GC 移动。

适用场景：

- native 回调 C# 时携带托管上下文。
- 将托管对象作为 opaque handle 暴露给 native。
- native 只保存 token，并在之后原样传回 C#。
- 不需要 native 直接访问对象内存地址。

典型用法：

```csharp
public sealed class CallbackState
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}

var state = new CallbackState
{
    Id = 1,
    Name = "CAD Session"
};

GCHandle handle = GCHandle.Alloc(state, GCHandleType.Normal);
IntPtr userData = GCHandle.ToIntPtr(handle);

Native.RegisterCallback(callback, userData);
```

回调时恢复对象：

```csharp
private static void OnNativeCallback(IntPtr userData)
{
    GCHandle handle = GCHandle.FromIntPtr(userData);

    if (handle.Target is not CallbackState state)
        return;

    // 使用 state
}
```

释放：

```csharp
handle.Free();
```

## 4. Pinned：固定对象并获取稳定地址

```csharp
var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
IntPtr address = handle.AddrOfPinnedObject();
```

`Pinned` 的作用是固定对象，使 GC 不能移动它，从而允许获取一个稳定地址传给 native。

适用场景：

- native API 需要直接读写托管数组内存。
- C/C++ 函数参数需要 `void*`、`double*`、`byte*` 等指针。
- 图像、点云、网格、坐标数组、工程计算结果等大块数据交换。
- 希望避免额外内存拷贝。

示例：

```csharp
byte[] buffer = new byte[1024];

GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
try
{
    IntPtr ptr = handle.AddrOfPinnedObject();
    Native.FillBuffer(ptr, buffer.Length);
}
finally
{
    handle.Free();
}
```

`Pinned` 要谨慎使用。长时间 pin 托管对象会阻止 GC 移动对象，可能造成托管堆碎片化并影响 GC 性能。

工程建议：

- 同步调用、短时间固定：可以使用 `fixed` 或 `GCHandleType.Pinned`。
- 跨方法或跨对象生命周期固定：可以使用 `GCHandleType.Pinned`，但要封装释放。
- native 长期持有内存：优先使用 unmanaged memory，而不是长期 pin 托管数组。

## 5. Weak 与 WeakTrackResurrection

`Weak` 不会阻止对象被 GC 回收：

```csharp
var handle = GCHandle.Alloc(obj, GCHandleType.Weak);
```

对象被回收后：

```csharp
handle.Target // 可能为 null
```

适用场景：

- native 侧只是弱观察托管对象。
- 缓存或注册表不希望阻止对象释放。

`WeakTrackResurrection` 会在对象 finalizer 执行后仍继续跟踪对象，直到内存真正回收。这个类型非常底层，普通 Interop 场景通常不建议使用。

## 6. 典型场景：native 回调携带 C# 上下文

C++ API 形态：

```cpp
typedef void(__stdcall* Callback)(int value, void* userData);

void RegisterCallback(Callback cb, void* userData);
void UnregisterCallback();
```

C# 侧：

```csharp
[UnmanagedFunctionPointer(CallingConvention.StdCall)]
private delegate void NativeCallback(int value, IntPtr userData);

private static readonly NativeCallback Callback = OnCallback;

[DllImport("NativeLib")]
private static extern void RegisterCallback(NativeCallback callback, IntPtr userData);

[DllImport("NativeLib")]
private static extern void UnregisterCallback();

private sealed class CallbackState
{
    public string Name { get; init; } = "";
}

private GCHandle _stateHandle;

public void Start()
{
    var state = new CallbackState
    {
        Name = "CAD Session"
    };

    _stateHandle = GCHandle.Alloc(state, GCHandleType.Normal);
    RegisterCallback(Callback, GCHandle.ToIntPtr(_stateHandle));
}

public void Stop()
{
    UnregisterCallback();

    if (_stateHandle.IsAllocated)
        _stateHandle.Free();
}

private static void OnCallback(int value, IntPtr userData)
{
    var handle = GCHandle.FromIntPtr(userData);

    if (handle.Target is not CallbackState state)
        return;

    // 使用 state
}
```

这里有两个关键点：

- `CallbackState` 通过 `GCHandleType.Normal` 保活。
- 回调 delegate 本身也必须保活。

很多 Interop 崩溃并不是因为 `GCHandle`，而是因为 delegate 被 GC 回收后，native 仍然调用旧函数指针。

错误示例：

```csharp
RegisterCallback(OnCallback, userData);
```

如果没有字段或静态变量保存 delegate，native 后续回调可能崩溃。

## 7. 典型场景：把 C# 对象作为 opaque handle 暴露给 C++

有时希望 native 持有一个句柄，但不关心其内部结构。

```csharp
public static IntPtr CreateManagedObject()
{
    var obj = new ManagedSession();
    var handle = GCHandle.Alloc(obj, GCHandleType.Normal);

    return GCHandle.ToIntPtr(handle);
}

public static void DestroyManagedObject(IntPtr handlePtr)
{
    if (handlePtr == IntPtr.Zero)
        return;

    var handle = GCHandle.FromIntPtr(handlePtr);

    if (handle.IsAllocated)
        handle.Free();
}

public static void UseManagedObject(IntPtr handlePtr)
{
    var handle = GCHandle.FromIntPtr(handlePtr);

    if (handle.Target is not ManagedSession session)
        throw new InvalidOperationException("Invalid managed handle.");

    session.DoWork();
}
```

native 侧只能把这个值当作 opaque token 保存，不能解引用，不能进行指针运算，也不能假设其内部布局。

## 8. 典型场景：托管数组传给 native 读写

C++ API：

```cpp
void ProcessPoints(double* points, int count);
```

C#：

```csharp
double[] points = new double[count * 3];

GCHandle handle = GCHandle.Alloc(points, GCHandleType.Pinned);
try
{
    IntPtr ptr = handle.AddrOfPinnedObject();
    Native.ProcessPoints(ptr, count);
}
finally
{
    handle.Free();
}
```

P/Invoke：

```csharp
[DllImport("NativeLib")]
private static extern void ProcessPoints(IntPtr points, int count);
```

如果只是同步调用，也可以使用 `fixed`：

```csharp
unsafe
{
    fixed (double* p = points)
    {
        Native.ProcessPoints((IntPtr)p, count);
    }
}
```

推荐原则：

- 同步短生命周期调用：优先 `fixed`。
- pin 生命周期需要跨方法或对象：使用 `GCHandleType.Pinned`。
- native 异步或长期持有内存：使用 unmanaged memory。

## 9. 工程计算、绘图、CAD 场景中的数组交换

在工程软件中，常见数据包括：

- 点数组。
- 顶点坐标。
- 索引数组。
- 多段线采样坐标。
- 曲线、曲面离散结果。
- 网格数据。
- 图像像素。
- FEM/CAE 数值矩阵片段。

例如 native 点结构：

```csharp
[StructLayout(LayoutKind.Sequential)]
public readonly struct Point3dNative
{
    public readonly double X;
    public readonly double Y;
    public readonly double Z;

    public Point3dNative(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
```

传递点数组：

```csharp
Point3dNative[] points = BuildPoints();

GCHandle handle = GCHandle.Alloc(points, GCHandleType.Pinned);
try
{
    IntPtr ptr = handle.AddrOfPinnedObject();
    Native.BuildMesh(ptr, points.Length);
}
finally
{
    handle.Free();
}
```

要求：

- 结构体必须具有确定布局，例如 `[StructLayout(LayoutKind.Sequential)]`。
- 字段应是 blittable 类型，例如 `double`、`float`、`int`、`long`。
- 不要直接 pin 包含 `string`、引用类型、托管数组字段的结构体。
- C# 结构体布局必须和 C/C++ 结构体布局一致，包括字段顺序、对齐、打包和平台位数。

## 10. GCHandle 与 fixed 的区别

`fixed` 示例：

```csharp
unsafe
{
    fixed (byte* p = buffer)
    {
        Native.UseBuffer((IntPtr)p, buffer.Length);
    }
}
```

`GCHandleType.Pinned` 示例：

```csharp
var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
try
{
    IntPtr p = handle.AddrOfPinnedObject();
    Native.UseBuffer(p, buffer.Length);
}
finally
{
    handle.Free();
}
```

选择建议：

| 场景 | 推荐方式 |
| --- | --- |
| 同步 native 调用 | `fixed` |
| native 回调携带托管上下文 | `GCHandleType.Normal` |
| 需要稳定地址且生命周期跨越代码块 | `GCHandleType.Pinned` |
| native 长期持有数据内存 | unmanaged memory |
| 缓存或弱观察 | `Weak` 或 `WeakReference<T>` |

## 11. GCHandle 与 SafeHandle 的区别

`GCHandle` 管的是托管对象在 GC 中的可达性和固定状态。

`SafeHandle` 管的是 native 资源生命周期，例如：

- Windows `HANDLE`。
- 文件句柄。
- native C++ 对象指针。
- CAD SDK 返回的 native object handle。

示例：

```csharp
public sealed class NativeObjectHandle : SafeHandle
{
    public NativeObjectHandle()
        : base(IntPtr.Zero, ownsHandle: true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        Native.DestroyObject(handle);
        return true;
    }
}
```

一句话区分：

- native 资源生命周期：用 `SafeHandle`。
- managed 对象交给 native 回调：用 `GCHandle`。
- native 长期访问内存：优先 unmanaged memory，并用 `SafeHandle` 或 `IDisposable` 管理。

## 12. 推荐封装

裸用 `GCHandle` 容易忘记释放，可以对常见场景做轻量封装。

```csharp
public sealed class ManagedObjectHandle<T> : IDisposable
    where T : class
{
    private GCHandle _handle;

    public ManagedObjectHandle(T target)
    {
        _handle = GCHandle.Alloc(target, GCHandleType.Normal);
    }

    public IntPtr IntPtr
    {
        get
        {
            Debug.Assert(_handle.IsAllocated);
            return GCHandle.ToIntPtr(_handle);
        }
    }

    public T Target
    {
        get
        {
            Debug.Assert(_handle.IsAllocated);
            return (T)_handle.Target!;
        }
    }

    public void Dispose()
    {
        if (_handle.IsAllocated)
            _handle.Free();
    }

    public static T FromIntPtr(IntPtr ptr)
    {
        var handle = GCHandle.FromIntPtr(ptr);
        return (T)handle.Target!;
    }
}
```

使用：

```csharp
using var stateHandle = new ManagedObjectHandle<CallbackState>(state);

Native.RegisterCallback(Callback, stateHandle.IntPtr);
```

注意：如果 native 会在 `using` 结束后继续回调，这种使用方式就是错误的。handle 的生命周期必须覆盖 native 可能访问它的完整时间段。

## 13. 一个完整的 Interop 生命周期模板

```csharp
public sealed class NativeSubscription : IDisposable
{
    private readonly NativeCallback _callback;
    private readonly GCHandle _stateHandle;
    private bool _disposed;

    public NativeSubscription(CallbackState state)
    {
        _callback = OnCallback;
        _stateHandle = GCHandle.Alloc(state, GCHandleType.Normal);

        Native.RegisterCallback(_callback, GCHandle.ToIntPtr(_stateHandle));
    }

    private static void OnCallback(int code, IntPtr userData)
    {
        var handle = GCHandle.FromIntPtr(userData);

        if (handle.Target is not CallbackState state)
            return;

        state.Handle(code);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Native.UnregisterCallback();

        if (_stateHandle.IsAllocated)
            _stateHandle.Free();

        _disposed = true;
    }
}
```

这个模板中：

- `_callback` 保活 delegate。
- `_stateHandle` 保活托管上下文。
- native 只保存 opaque `IntPtr`。
- `Dispose` 中先取消注册，再释放 handle。

## 14. 常见坑

### 14.1 忘记 Free

```csharp
GCHandle.Alloc(obj);
```

如果没有 `Free()`，对象会一直被认为可达，形成托管内存泄漏。

### 14.2 把 GCHandle.ToIntPtr 当对象地址

```csharp
IntPtr ptr = GCHandle.ToIntPtr(handle);
```

这个值不是对象地址，native 不能解引用。它只能原样传回 .NET，再通过 `GCHandle.FromIntPtr(ptr)` 恢复。

### 14.3 长时间 Pinned

长时间 pin 托管对象会增加堆碎片风险。对于 native 长期持有的数据，建议使用：

```csharp
IntPtr ptr = Marshal.AllocHGlobal(byteCount);
```

或：

```csharp
unsafe
{
    void* ptr = NativeMemory.Alloc((nuint)byteCount);
}
```

并用 `SafeHandle` 或 `IDisposable` 管理释放。

### 14.4 回调 delegate 没有保活

即使上下文对象通过 `GCHandle` 保活，delegate 本身也要保活：

```csharp
private readonly NativeCallback _callback;
private GCHandle _contextHandle;
```

### 14.5 释放顺序错误

危险顺序：

```csharp
_contextHandle.Free();
Native.UnregisterCallback();
```

更合理：

```csharp
Native.UnregisterCallback();

if (_contextHandle.IsAllocated)
    _contextHandle.Free();
```

先让 native 不再访问，再释放托管 handle。

### 14.6 native 回调线程直接访问 UI

WPF/DevExpress 中，native 回调可能发生在非 UI 线程，不应直接操作 UI。

```csharp
Application.Current.Dispatcher.Invoke(() =>
{
    // 更新 ViewModel 或 UI
});
```

更稳妥的做法是：native 回调只负责采集事件和入队，UI 线程或业务线程再消费。

### 14.7 GCHandle 解决不了 ABI 问题

即使用了 `GCHandle`，仍然需要正确处理：

- `CallingConvention`。
- `StructLayout`。
- `Pack`。
- `bool` 大小。
- 字符串编码。
- `char*`、`wchar_t*`、UTF-8、UTF-16。
- x86/x64 指针大小。
- C++ 对象 ABI 边界。

## 15. 工程推荐策略

推荐按下面顺序判断：

1. native 只是同步读写托管数组：优先 `fixed` 或普通 P/Invoke 数组封送。
2. native 需要回调 C# 并携带上下文：使用 `GCHandleType.Normal`。
3. native 需要短期直接访问托管数组地址：使用 `fixed` 或 `GCHandleType.Pinned`。
4. native 需要长期持有数据内存：使用 unmanaged memory。
5. native 资源句柄生命周期：使用 `SafeHandle`。
6. CAD/WPF 宿主插件中涉及回调：特别注意取消注册、文档关闭、插件卸载、UI 线程切换和宿主 API 约束。

## 16. 总结

`GCHandle` 最重要的价值不是“拿到对象地址”，而是“显式控制托管对象在 native 互操作边界上的生命周期语义”。

在 Interop 中：

- 用 `Normal` 保活托管上下文。
- 用 `Pinned` 短期固定托管内存并获取稳定地址。
- 不要把 `GCHandle.ToIntPtr` 当对象指针。
- 不要长期 pin 普通托管对象。
- native 资源使用 `SafeHandle`，native 长期内存使用 unmanaged memory。
- native 回调场景中，托管上下文和 delegate 都必须保活。

对于工程计算、CAD 二次开发和图形数据交换，`GCHandle` 是一个很有价值的工具，但它应该被放在清晰的生命周期边界内使用，并尽量通过小型封装减少泄漏和释放顺序错误。
