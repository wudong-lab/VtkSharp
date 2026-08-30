using System.Windows;

namespace VtkSharp.ExampleBrowser;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 0)
        {
            this.MainWindow = new MainWindow();
            this.MainWindow.Show();
            return;
        }

        // 命令行验收在 WPF 启动的 STA 线程中完成，不创建浏览器窗口。
        this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        this.Shutdown(ExampleSmokeRunner.Run(e.Args));
    }
}
