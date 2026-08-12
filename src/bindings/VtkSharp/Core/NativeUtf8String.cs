using System.Runtime.InteropServices;

namespace VtkSharp;

[StructLayout(LayoutKind.Sequential)]
internal struct NativeUtf8String
{
    public nint Data;
    public nuint Length;
}
