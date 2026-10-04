using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace VtkSharp;

/// <summary>Configures the directory that contains the VtkSharp native modules.</summary>
public static class VtkSharpRuntime
{
    /// <summary>Sets the native module directory before the first VtkSharp native call.</summary>
    public static void ConfigureNativeRuntimeDirectory(string runtimeDirectory)
        => NativeModuleLoader.Configure(runtimeDirectory);
}

internal static class NativeModuleLoader
{
    private const uint LoadLibrarySearchDllLoadDir = 0x00000100;
    private const uint LoadLibrarySearchSystem32 = 0x00000800;
    private static readonly object Gate = new();
    private static readonly Dictionary<string, IntPtr> Handles = new(StringComparer.OrdinalIgnoreCase);
#if NET8_0_OR_GREATER
    private static bool ResolverRegistered;
#endif
    private static string _runtimeDirectory = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "native", "VtkSharp", "win-x64"));

    internal static void Configure(string runtimeDirectory)
    {
        if (string.IsNullOrWhiteSpace(runtimeDirectory))
            throw new ArgumentException("A native runtime directory is required.", nameof(runtimeDirectory));
        lock (Gate)
        {
            if (Handles.Count > 0)
                throw new InvalidOperationException("The VtkSharp native runtime directory cannot change after the first module has loaded.");
            _runtimeDirectory = Path.GetFullPath(runtimeDirectory);
        }
    }

    internal static void EnsureLoaded(string libraryName)
    {
#if NET8_0_OR_GREATER
        lock (Gate)
        {
            if (!ResolverRegistered)
            {
                NativeLibrary.SetDllImportResolver(typeof(NativeModuleLoader).Assembly, ResolveLibrary);
                ResolverRegistered = true;
            }
        }
#else
        Load(libraryName);
#endif
    }

#if NET8_0_OR_GREATER
    private static IntPtr ResolveLibrary(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
        => IsVtkSharpModule(libraryName) ? Load(libraryName) : IntPtr.Zero;
#endif

    private static IntPtr Load(string libraryName)
    {
        if (!IsVtkSharpModule(libraryName))
            throw new ArgumentException($"'{libraryName}' is not a VtkSharp native module name.", nameof(libraryName));

        lock (Gate)
        {
            if (Handles.TryGetValue(libraryName, out var cachedHandle))
                return cachedHandle;

            var modulePath = Path.Combine(_runtimeDirectory, libraryName);
            var handle = LoadLibraryExW(modulePath, IntPtr.Zero, LoadLibrarySearchDllLoadDir | LoadLibrarySearchSystem32);
            if (handle == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                throw new DllNotFoundException($"Failed to load VtkSharp native module '{libraryName}' from '{modulePath}' (Win32 error {error}).");
            }

            Handles.Add(libraryName, handle);
            return handle;
        }
    }

    private static bool IsVtkSharpModule(string libraryName)
        => libraryName.StartsWith("VtkSharp.Native.", StringComparison.OrdinalIgnoreCase)
           && libraryName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "LoadLibraryExW")]
    private static extern IntPtr LoadLibraryExW(string fileName, IntPtr file, uint flags);
}
