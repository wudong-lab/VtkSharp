using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace VtkSharp.ExampleBrowser;

internal static class ExampleSmokeRunner
{
    public static int Run(string[] args)
    {
        try
        {
            if (args is not ["--smoke", var name, "--output", var outputDirectory])
                throw new ArgumentException("Usage: ExampleBrowser --smoke <Category/Name> --output <new-directory>");

            var info = ExampleDiscovery.DiscoverAll().SelectMany(group => group.Value)
                .SingleOrDefault(item => $"{item.Category}/{item.Name}" == name)
                ?? throw new ArgumentException($"Example not found: {name}");
            if (!typeof(ISmokeExample).IsAssignableFrom(info.ExampleType))
                throw new NotSupportedException($"Example does not support automatic smoke verification: {name}");

            var outputPath = Path.GetFullPath(outputDirectory);
            // 每次使用新目录，避免旧截图被误认为本次运行结果。
            if (Directory.Exists(outputPath))
                throw new IOException($"Output directory already exists: {outputPath}");
            Directory.CreateDirectory(outputPath);
            var screenshotPath = Path.Combine(outputPath, "screenshot.png");
            var example = (ISmokeExample)Activator.CreateInstance(info.ExampleType)!;
            example.RenderScreenshot(screenshotPath);

            using var stream = File.OpenRead(screenshotPath);
            var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            File.WriteAllText(Path.Combine(outputPath, "result.json"), JsonSerializer.Serialize(new
            {
                status = "passed", example = name, screenshot = screenshotPath,
                width = frame.PixelWidth, height = frame.PixelHeight,
                manualChecks = new[] { "visual-content", "interaction", "repeated-create-dispose" },
            }, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Smoke passed: {name}; screenshot: {screenshotPath}");
            return 0;
        }
        catch (Exception exception)
        {
            // 进程入口统一转成非零退出码；不让自动验收停在未处理异常对话框。
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
