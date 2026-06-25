using System;
using System.Text;

namespace VtkSharp;

internal static class VtkString
{
    public static unsafe string FromUtf8Pointer(nint ptr)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;
        var p = (byte*)ptr;
        var length = 0;
        while (p[length] != 0)
            length++;
        return Encoding.UTF8.GetString(p, length);
    }

    public static byte[] ToNullTerminatedUtf8(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return new byte[1];
        var s = value!;
        var byteCount = Encoding.UTF8.GetByteCount(s);
        var bytes = new byte[byteCount + 1];
        Encoding.UTF8.GetBytes(s, 0, s.Length, bytes, 0);
        return bytes;
    }
}
