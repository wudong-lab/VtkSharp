using System.Text;

namespace VtkSharp.Generator.Core.Generation;

public static class GeneratedFileWriter
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static bool WriteIfChanged(string path, string content)
    {
        var directory = Path.GetDirectoryName(path)
                        ?? throw new InvalidOperationException($"Output path '{path}' does not have a directory.");

        Directory.CreateDirectory(directory);

        if (File.Exists(path) && TextEquals(File.ReadAllText(path), content))
            return false;

        var bytes = Utf8NoBom.GetBytes(content);
        File.WriteAllBytes(path, bytes);
        return true;
    }

    private static bool TextEquals(string existing, string generated)
        => string.Equals(NormalizeLineEndings(existing), NormalizeLineEndings(generated), StringComparison.Ordinal);

    private static string NormalizeLineEndings(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\r", "\n", StringComparison.Ordinal);
}
