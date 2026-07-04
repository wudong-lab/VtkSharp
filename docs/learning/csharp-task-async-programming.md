# C# 异步编程学习文档：基于 Task 的异步模型

本文面向工程计算、WPF 桌面软件、CAD 二次开发等场景，系统整理 C# 基于 `Task` 的异步编程知识。

## 1. 异步编程解决什么问题

C# 异步编程主要用于：

- 避免阻塞线程，例如 UI 线程、服务线程、调度线程。
- 提高 I/O 密集型任务的吞吐，例如文件读写、网络请求、数据库访问。
- 用接近同步代码的形式表达异步控制流。

需要明确一点：

异步不等于多线程。`async` / `await` 本身不一定创建新线程。它主要表达的是：当前操作需要等待时，先把线程释放出去；等异步操作完成后，再继续执行后续代码。

## 2. 核心类型

### 2.1 Task

`Task` 表示一个没有返回值的异步操作。

```csharp
public async Task SaveAsync()
{
    await File.WriteAllTextAsync("data.txt", "hello");
}
```

### 2.2 Task<T>

`Task<T>` 表示一个有返回值的异步操作。

```csharp
public async Task<string> LoadTextAsync(string path)
{
    return await File.ReadAllTextAsync(path);
}
```

### 2.3 ValueTask<T>

`ValueTask<T>` 用于性能敏感场景，尤其是结果可能同步完成且调用频繁的 API。

普通业务代码优先使用 `Task<T>`。只有在明确有性能压力，并理解 `ValueTask` 使用限制时，再考虑 `ValueTask<T>`。

```csharp
public ValueTask<int> GetCachedCountAsync()
{
    return ValueTask.FromResult(42);
}
```

## 3. async 和 await 的基本规则

`async` 方法通常返回：

```csharp
Task
Task<T>
ValueTask
ValueTask<T>
```

不推荐返回 `void`，除非是事件处理器。

```csharp
private async void Button_Click(object sender, RoutedEventArgs e)
{
    await LoadDataAsync();
}
```

业务方法不要写成：

```csharp
public async void LoadData()
{
    await Task.Delay(1000);
}
```

应该写成：

```csharp
public async Task LoadDataAsync()
{
    await Task.Delay(1000);
}
```

原因是 `async void` 无法被调用方 `await`，异常也更难管理。

## 4. await 的执行模型

当执行到：

```csharp
await SomeAsyncOperation();
```

大致过程是：

1. 如果任务已经完成，继续向下执行。
2. 如果任务未完成，当前方法挂起。
3. 当前线程返回给调用方或线程池。
4. 异步操作完成后，恢复执行 `await` 后面的代码。
5. 如果任务失败，`await` 会重新抛出异常。

示例：

```csharp
public async Task DemoAsync()
{
    Console.WriteLine("Before");
    await Task.Delay(1000);
    Console.WriteLine("After");
}
```

`Task.Delay` 不会占用一个线程睡眠 1 秒，而是注册计时器，时间到了再继续执行。

## 5. 异步方法命名

异步方法一般以 `Async` 结尾：

```csharp
LoadAsync()
SaveAsync()
CalculateAsync()
ExportDrawingAsync()
```

这是 .NET 生态中的强约定，建议坚持。

## 6. I/O 密集型与 CPU 密集型

### 6.1 I/O 密集型：天然适合 async

网络请求：

```csharp
public async Task<string> DownloadAsync(HttpClient httpClient, string url)
{
    return await httpClient.GetStringAsync(url);
}
```

文件读写：

```csharp
public async Task<string> ReadConfigAsync(string path)
{
    return await File.ReadAllTextAsync(path);
}
```

数据库访问：

```csharp
public async Task<List<ProjectInfo>> LoadProjectsAsync()
{
    return await dbContext.Projects.ToListAsync();
}
```

### 6.2 CPU 密集型：async 本身不能加速计算

几何计算、有限元前处理、点云处理、CAD 图元批量计算，通常属于 CPU 密集型任务。

下面这种写法不会自动切到后台线程：

```csharp
public async Task<double> CalculateAsync()
{
    return HeavyCalculation();
}
```

如果目标是避免阻塞 UI，可以用 `Task.Run`：

```csharp
public Task<double> CalculateAsync(CancellationToken cancellationToken)
{
    return Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        return HeavyCalculation();
    }, cancellationToken);
}
```

`Task.Run` 是把 CPU 工作放到线程池。它适合桌面软件避免 UI 卡死，但不应滥用于本来就是异步 I/O 的操作。

## 7. WPF 中的异步编程

WPF 的关键约束是：UI 只能在 UI 线程访问。

```csharp
private async void LoadButton_Click(object sender, RoutedEventArgs e)
{
    IsBusy = true;

    try
    {
        var data = await LoadModelAsync();
        ViewModel.Model = data;
    }
    finally
    {
        IsBusy = false;
    }
}
```

`await` 默认会捕获当前同步上下文。在 WPF 中，`await` 后面的代码通常会回到 UI 线程，所以可以安全更新绑定属性。

## 8. ConfigureAwait(false)

默认情况下：

```csharp
await SomeAsync();
```

会尝试回到原来的上下文。在 WPF 中通常就是 UI 线程。

库代码中常见写法：

```csharp
await SomeAsync().ConfigureAwait(false);
```

含义是：异步操作完成后，不强制回到原来的上下文。

推荐规则：

- WPF ViewModel / UI 层通常不要加 `ConfigureAwait(false)`，因为后续可能要更新 UI 绑定状态。
- 底层库、计算库、I/O 工具类可以考虑使用 `ConfigureAwait(false)`，避免不必要的上下文切换。

```csharp
public async Task<string> LoadRawTextAsync(string path)
{
    return await File.ReadAllTextAsync(path).ConfigureAwait(false);
}
```

## 9. 异常处理

异步方法中的异常会存储在 `Task` 中，在 `await` 时重新抛出。

```csharp
public async Task LoadAsync()
{
    try
    {
        var text = await File.ReadAllTextAsync("missing.txt");
    }
    catch (FileNotFoundException ex)
    {
        Console.WriteLine(ex.Message);
    }
}
```

不要用 `.Wait()` 或 `.Result` 获取结果：

```csharp
var text = LoadTextAsync().Result;
```

这可能导致：

- UI 死锁。
- 线程阻塞。
- 异常被包进 `AggregateException`。

推荐：

```csharp
var text = await LoadTextAsync();
```

## 10. 取消：CancellationToken

长时间计算、批量导入、CAD 图元遍历、文件导出，都应该考虑取消。

```csharp
public async Task ExportAsync(
    string path,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();

    var data = await LoadExportDataAsync(cancellationToken);

    cancellationToken.ThrowIfCancellationRequested();

    await File.WriteAllTextAsync(path, data, cancellationToken);
}
```

CPU 密集型任务中需要主动检查：

```csharp
public Task RunGeometryCalculationAsync(CancellationToken cancellationToken)
{
    return Task.Run(() =>
    {
        for (int i = 0; i < 1_000_000; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Geometry calculation.
        }
    }, cancellationToken);
}
```

取消不是强制杀线程，而是协作式退出。

## 11. 进度报告：IProgress<T>

`IProgress<T>` 适合 WPF 中后台任务向 UI 报告进度。

```csharp
public async Task ImportAsync(
    string filePath,
    IProgress<int> progress,
    CancellationToken cancellationToken)
{
    for (int i = 0; i <= 100; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Task.Delay(50, cancellationToken);
        progress.Report(i);
    }
}
```

WPF 调用：

```csharp
var progress = new Progress<int>(value =>
{
    ProgressValue = value;
});

await ImportAsync(path, progress, cancellationToken);
```

`Progress<T>` 会把回调切回创建它的同步上下文。如果在 UI 线程创建，它会在 UI 线程更新进度。

## 12. 并发执行多个任务

顺序执行：

```csharp
var a = await LoadAAsync();
var b = await LoadBAsync();
```

总耗时约等于 A + B。

并发执行：

```csharp
Task<ModelA> taskA = LoadAAsync();
Task<ModelB> taskB = LoadBAsync();

await Task.WhenAll(taskA, taskB);

ModelA a = await taskA;
ModelB b = await taskB;
```

总耗时约等于 max(A, B)。适合多个互不依赖的 I/O 操作。

## 13. 限制并发数量

批量处理文件、网络请求、图纸导入时，不应一次性启动大量任务。

可以用 `SemaphoreSlim`：

```csharp
public async Task ProcessFilesAsync(
    IReadOnlyList<string> files,
    CancellationToken cancellationToken)
{
    using var semaphore = new SemaphoreSlim(4);

    var tasks = files.Select(async file =>
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            await ProcessFileAsync(file, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    });

    await Task.WhenAll(tasks);
}
```

这里最大并发数是 4。

## 14. TaskCompletionSource

`TaskCompletionSource` 用于把回调式 API 包装成 `Task`。

```csharp
public Task<string> WaitForMessageAsync()
{
    var tcs = new TaskCompletionSource<string>();

    void OnMessageReceived(object? sender, string message)
    {
        MessageReceived -= OnMessageReceived;
        tcs.SetResult(message);
    }

    MessageReceived += OnMessageReceived;

    return tcs.Task;
}
```

更严谨一点可以处理取消：

```csharp
public Task<string> WaitForMessageAsync(CancellationToken cancellationToken)
{
    var tcs = new TaskCompletionSource<string>(
        TaskCreationOptions.RunContinuationsAsynchronously);

    void OnMessageReceived(object? sender, string message)
    {
        MessageReceived -= OnMessageReceived;
        tcs.TrySetResult(message);
    }

    MessageReceived += OnMessageReceived;

    cancellationToken.Register(() =>
    {
        MessageReceived -= OnMessageReceived;
        tcs.TrySetCanceled(cancellationToken);
    });

    return tcs.Task;
}
```

`RunContinuationsAsynchronously` 可以避免 continuation 在事件触发线程上同步执行，降低死锁和重入风险。

## 15. 常见错误

### 15.1 忘记 await

```csharp
SaveAsync();
```

这样会启动异步操作但不等待，异常也可能丢失。

应该：

```csharp
await SaveAsync();
```

如果确实要 fire-and-forget，需要明确处理异常：

```csharp
_ = Task.Run(async () =>
{
    try
    {
        await SaveLogAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Save log failed.");
    }
});
```

### 15.2 在 UI 线程使用 .Result / .Wait()

```csharp
var result = LoadAsync().Result;
```

WPF 中容易死锁。

应该：

```csharp
var result = await LoadAsync();
```

### 15.3 把 CPU 计算误认为 async 会自动变快

```csharp
public async Task CalculateAsync()
{
    HeavyCalculation();
}
```

这没有意义。应该根据目标选择：

```csharp
await Task.Run(() => HeavyCalculation());
```

或者如果调用方已经在后台线程，就保持同步方法：

```csharp
public CalculationResult Calculate()
{
    return HeavyCalculation();
}
```

### 15.4 过度使用 Task.Run

下面这种 I/O 异步不需要 `Task.Run`：

```csharp
await Task.Run(() => File.ReadAllTextAsync(path));
```

应该直接：

```csharp
await File.ReadAllTextAsync(path);
```

## 16. 工程软件中的实践建议

### 16.1 UI 命令异步化

WPF MVVM 中推荐使用异步命令：

```csharp
public async Task LoadCommandAsync()
{
    IsBusy = true;

    try
    {
        Model = await modelService.LoadAsync();
    }
    finally
    {
        IsBusy = false;
    }
}
```

如果使用 CommunityToolkit.Mvvm：

```csharp
[RelayCommand]
private async Task LoadAsync()
{
    IsBusy = true;

    try
    {
        Model = await modelService.LoadAsync();
    }
    finally
    {
        IsBusy = false;
    }
}
```

### 16.2 CAD API 访问要谨慎

很多 CAD 宿主 API 要求：

- 必须在主线程调用。
- 必须持有文档锁。
- 必须在事务中操作数据库。
- 不能随意跨线程访问实体对象。

因此不要简单这样写：

```csharp
await Task.Run(() =>
{
    // Do not directly access CAD database entities here.
});
```

更稳妥的模式是：

1. 在 CAD 主线程读取必要数据，转换为自己的纯数据结构。
2. 在后台线程做纯计算。
3. 回到 CAD 主线程写回实体。

示意：

```csharp
public async Task AnalyzeDrawingAsync(CancellationToken cancellationToken)
{
    DrawingSnapshot snapshot = ReadDrawingSnapshotOnCadThread();

    AnalysisResult result = await Task.Run(() =>
    {
        return Analyze(snapshot, cancellationToken);
    }, cancellationToken);

    WriteResultBackOnCadThread(result);
}
```

重点是不要把 CAD 实体对象直接带到后台线程长期使用。

### 16.3 几何计算建议保持同步核心

几何算法核心通常建议写成同步、纯函数：

```csharp
public static IntersectionResult Intersect(Line2d a, Line2d b, double tolerance)
{
    // Pure calculation.
}
```

异步只放在外层调度：

```csharp
public Task<IntersectionResult> IntersectAsync(
    Line2d a,
    Line2d b,
    double tolerance,
    CancellationToken cancellationToken)
{
    return Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Intersect(a, b, tolerance);
    }, cancellationToken);
}
```

这样有几个好处：

- 算法更容易测试。
- 不污染核心模型。
- 调用方可以自由选择同步或异步执行。
- 更适合高性能场景做批处理优化。

## 17. 推荐学习顺序

1. 理解 `Task` / `Task<T>`。
2. 掌握 `async` / `await`。
3. 区分 I/O 密集型和 CPU 密集型。
4. 避免 `.Result` / `.Wait()`。
5. 学会异常处理和取消。
6. 学会 `Task.WhenAll`。
7. 学会限制并发。
8. 理解 WPF 同步上下文。
9. 理解 `ConfigureAwait(false)`。
10. 学会用 `TaskCompletionSource` 包装旧式回调 API。
11. 在 CAD / WPF / 工程计算场景中建立正确的线程边界。

## 18. 综合示例：WPF 后台导入文件

ViewModel：

```csharp
public sealed class ImportViewModel : ObservableObject
{
    private readonly ImportService _importService;
    private CancellationTokenSource? _cancellationTokenSource;

    private bool _isBusy;
    private int _progress;

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    public int Progress
    {
        get => _progress;
        set => SetProperty(ref _progress, value);
    }

    public ImportViewModel(ImportService importService)
    {
        _importService = importService;
    }

    public async Task ImportAsync(string filePath)
    {
        if (IsBusy)
            return;

        _cancellationTokenSource = new CancellationTokenSource();

        var progress = new Progress<int>(value =>
        {
            Progress = value;
        });

        IsBusy = true;

        try
        {
            await _importService.ImportAsync(
                filePath,
                progress,
                _cancellationTokenSource.Token);
        }
        finally
        {
            IsBusy = false;
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
    }
}
```

服务层：

```csharp
public sealed class ImportService
{
    public async Task ImportAsync(
        string filePath,
        IProgress<int> progress,
        CancellationToken cancellationToken)
    {
        string text = await File.ReadAllTextAsync(filePath, cancellationToken);

        var lines = text.Split(Environment.NewLine);

        await Task.Run(() =>
        {
            for (int i = 0; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Parse line and build domain model.

                int value = (i + 1) * 100 / lines.Length;
                progress.Report(value);
            }
        }, cancellationToken);
    }
}
```

这个例子体现了：

- 文件读取用真正的异步 I/O。
- CPU 解析放到 `Task.Run`。
- 通过 `CancellationToken` 支持取消。
- 通过 `IProgress<int>` 安全更新 UI 进度。
- UI 状态用 `IsBusy` 控制。

## 19. 核心判断原则

遇到一个方法要不要做成异步，可以按这个顺序判断：

1. 它是否等待 I/O？  
   是，优先做成 `async Task` / `async Task<T>`。

2. 它是否是长时间 CPU 计算，并且调用方是 UI 线程？  
   可以外层用 `Task.Run`，但核心算法保持同步。

3. 它是否只是普通快速计算？  
   不要异步化。

4. 它是否访问 WPF 控件、DevExpress 控件或 CAD 宿主对象？  
   注意线程约束，不要随意放到后台线程。

一句话总结：

`async` 主要解决“等待时不要阻塞线程”；`Task.Run` 主要解决“CPU 工作不要卡住 UI”；CAD / UI 对象通常不能随便跨线程访问。
