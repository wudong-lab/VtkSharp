using System;
using System.Text;

namespace VtkSharp;

internal static class VtkString
{
    public static unsafe string FromUtf8Pointer(nint ptr)
    {
        if (ptr == nint.Zero)
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
            return [0];
        var byteCount = Encoding.UTF8.GetByteCount(value);
        var bytes = new byte[byteCount + 1];
        Encoding.UTF8.GetBytes(value, 0, value.Length, bytes, 0);
        return bytes;
    }
}
