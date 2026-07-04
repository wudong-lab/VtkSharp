using System.Windows;
using VtkSharp.Generator.Gui.ViewModels;

namespace VtkSharp.Generator.Gui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var viewModel = new MainWindowViewModel();
        this.DataContext = viewModel;
        this.Loaded += async (_, _) => await viewModel.LoadTypesCommand.ExecuteAsync(null);
    }
}
