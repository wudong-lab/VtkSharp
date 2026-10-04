using System.Diagnostics;
using System.Runtime.InteropServices;

var runtime = Path.GetFullPath(args[0]);
var scenario = args.Length > 1 ? args[1] : "core";
var started = Stopwatch.StartNew();
Native.Configure(runtime);

switch (scenario)
{
    case "core":
        UseCore();
        break;
    case "filters":
        UseFilters();
        break;
    case "render":
        UseRendering();
        break;
    case "private-first":
        UsePrivateThenPublic();
        break;
    case "public-first":
        UsePublicThenPrivate();
        break;
    case "concurrent":
        await Task.WhenAll(Task.Run(UseCore), Task.Run(UsePrivateThenPublic));
        break;
    default:
        throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown scenario");
}

started.Stop();
using var process = Process.GetCurrentProcess();
Console.WriteLine($"scenario={scenario}; elapsedMs={started.Elapsed.TotalMilliseconds:F2}; workingSetBytes={process.WorkingSet64}");
foreach (ProcessModule module in process.Modules)
{
    if (module.ModuleName.StartsWith("vtk", StringComparison.OrdinalIgnoreCase)
        || module.ModuleName.StartsWith("VtkSharp.Native", StringComparison.OrdinalIgnoreCase)
        || module.ModuleName.Equals("BRDI.VtkSharp.Native.dll", StringComparison.OrdinalIgnoreCase))
        Console.WriteLine($"loaded={module.FileName}");
}

void UseCore()
{
    var points = Native.NewPoints(runtime);
    try { Console.WriteLine($"core={Native.GetClassName(runtime, points)}"); }
    finally { Native.Delete(runtime, points); }
}

void UseFilters()
{
    var source = Native.NewSphereSource(runtime);
    try
    {
        Native.SetRadius(runtime, source, 2.5);
        var radius = Native.GetRadius(runtime, source);
        if (radius != 2.5) throw new InvalidOperationException($"Unexpected sphere radius: {radius}");
        Console.WriteLine($"filters={radius:F1}");
    }
    finally { Native.Delete(runtime, source); }
}

void UseRendering()
{
    var window = Native.NewRenderWindow(runtime);
    try
    {
        var className = Native.GetClassName(runtime, window);
        if (className.IndexOf("OpenGL", StringComparison.Ordinal) < 0)
            throw new InvalidOperationException($"Rendering factory selected an unexpected backend: {className}");
        Console.WriteLine($"render={className}");
    }
    finally { Native.Delete(runtime, window); }
}

void UsePrivateThenPublic()
{
    var actor = Native.NewPrivateActor(runtime);
    try
    {
        var className = Native.GetClassName(runtime, actor);
        if (className != "vtkBrdiContourLegendActor")
            throw new InvalidOperationException($"Unexpected private actor type: {className}");
        Console.WriteLine($"private-first={className}");
    }
    finally { Native.Delete(runtime, actor); }
}

void UsePublicThenPrivate()
{
    UseCore();
    UsePrivateThenPublic();
}

static class Native
{
    private const uint LoadLibrarySearchDllLoadDir = 0x00000100;
    private const uint LoadLibrarySearchSystem32 = 0x00000800;
    private static readonly object Gate = new();
    private static readonly Dictionary<string, IntPtr> Handles = new(StringComparer.OrdinalIgnoreCase);
    private static string RuntimeDirectory = "";

    public static void Configure(string runtime)
    {
        RuntimeDirectory = runtime;
#if NET8_0_OR_GREATER
        NativeLibrary.SetDllImportResolver(typeof(Native).Assembly, ResolveLibrary);
#endif
    }

#if NET8_0_OR_GREATER
    private static IntPtr ResolveLibrary(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName.Equals("VtkSharp.Native.CommonCore.dll", StringComparison.OrdinalIgnoreCase)
            || libraryName.Equals("VtkSharp.Native.FiltersSources.dll", StringComparison.OrdinalIgnoreCase)
            || libraryName.Equals("VtkSharp.Native.Rendering.dll", StringComparison.OrdinalIgnoreCase)
            || libraryName.Equals("BRDI.VtkSharp.Native.dll", StringComparison.OrdinalIgnoreCase))
            return Load(RuntimeDirectory, libraryName);
        return IntPtr.Zero;
    }
#endif

    public static IntPtr Load(string runtime, string name)
    {
        var path = Path.Combine(runtime, name);
        lock (Gate)
        {
            if (!Handles.TryGetValue(path, out var handle))
            {
                handle = LoadLibraryEx(path, IntPtr.Zero, LoadLibrarySearchDllLoadDir | LoadLibrarySearchSystem32);
                if (handle == IntPtr.Zero)
                    throw new InvalidOperationException($"LoadLibraryExW failed for {path}: {Marshal.GetLastWin32Error()}");
                Handles.Add(path, handle);
                Console.WriteLine($"load-request={name}; path={path}");
            }
            return handle;
        }
    }

    public static IntPtr NewPoints(string runtime) { EnsureLoaded(runtime, "VtkSharp.Native.CommonCore.dll"); return NewPointsNative(); }
    public static IntPtr NewSphereSource(string runtime) { EnsureLoaded(runtime, "VtkSharp.Native.FiltersSources.dll"); return NewSphereSourceNative(); }
    public static IntPtr NewRenderWindow(string runtime) { EnsureLoaded(runtime, "VtkSharp.Native.Rendering.dll"); return NewRenderWindowNative(); }
    public static IntPtr NewPrivateActor(string runtime) { EnsureLoaded(runtime, "BRDI.VtkSharp.Native.dll"); return NewPrivateActorNative(); }

    public static string GetClassName(string runtime, IntPtr value)
    {
        EnsureLoaded(runtime, "VtkSharp.Native.CommonCore.dll");
        return Marshal.PtrToStringAnsi(GetClassNameNative(value))!;
    }

    public static void Delete(string runtime, IntPtr value)
    {
        EnsureLoaded(runtime, "VtkSharp.Native.CommonCore.dll");
        DeleteNative(value);
    }

    public static double GetRadius(string runtime, IntPtr value) { EnsureLoaded(runtime, "VtkSharp.Native.FiltersSources.dll"); return GetRadiusNative(value); }
    public static void SetRadius(string runtime, IntPtr value, double radius) { EnsureLoaded(runtime, "VtkSharp.Native.FiltersSources.dll"); SetRadiusNative(value, radius); }

    private static void EnsureLoaded(string runtime, string name)
    {
#if !NET8_0_OR_GREATER
        Load(runtime, name);
#endif
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "LoadLibraryExW")]
    private static extern IntPtr LoadLibraryEx(string fileName, IntPtr file, uint flags);

    [DllImport("VtkSharp.Native.CommonCore.dll", EntryPoint = "VtkSharpCommonCore_NewPoints")]
    private static extern IntPtr NewPointsNative();
    [DllImport("VtkSharp.Native.CommonCore.dll", EntryPoint = "VtkSharpCommonCore_GetClassName")]
    private static extern IntPtr GetClassNameNative(IntPtr value);
    [DllImport("VtkSharp.Native.CommonCore.dll", EntryPoint = "VtkSharpCommonCore_Delete")]
    private static extern void DeleteNative(IntPtr value);
    [DllImport("VtkSharp.Native.FiltersSources.dll", EntryPoint = "VtkSharpFiltersSources_NewSphereSource")]
    private static extern IntPtr NewSphereSourceNative();
    [DllImport("VtkSharp.Native.FiltersSources.dll", EntryPoint = "VtkSharpFiltersSources_GetRadius")]
    private static extern double GetRadiusNative(IntPtr value);
    [DllImport("VtkSharp.Native.FiltersSources.dll", EntryPoint = "VtkSharpFiltersSources_SetRadius")]
    private static extern void SetRadiusNative(IntPtr value, double radius);
    [DllImport("VtkSharp.Native.Rendering.dll", EntryPoint = "VtkSharpRendering_NewRenderWindow")]
    private static extern IntPtr NewRenderWindowNative();
    [DllImport("BRDI.VtkSharp.Native.dll", EntryPoint = "BRDI_vtkBrdiContourLegendActor_New")]
    private static extern IntPtr NewPrivateActorNative();
}
