namespace VtkSharp.ExampleBrowser;

// 可选验收能力：返回前保存首帧并释放本次创建的资源，不进入交互事件循环。
public interface ISmokeExample : IExample
{
    void RenderScreenshot(string screenshotPath);
}
