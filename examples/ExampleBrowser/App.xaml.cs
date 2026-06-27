using System.Windows;

namespace VtkSharp.ExampleBrowser;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        global::Wpf.Ui.Appearance.ApplicationThemeManager.Apply(
            global::Wpf.Ui.Appearance.ApplicationTheme.Dark);
    }
}
