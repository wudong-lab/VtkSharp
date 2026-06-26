using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaEdit.Highlighting;

namespace VtkSharp.ExampleBrowser;

public partial class MainWindow : Window
{
    private static readonly string ExamplesRoot = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples", "ExampleBrowser");

    private readonly Dictionary<string, List<ExampleInfo>> _examples = null!;
    private ExampleInfo? _selectedExample;

    public MainWindow()
    {
        this.InitializeComponent();

        this.CodeEditor.Text = "// Select an example from the tree to view its source code.";
        this.CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
        this.CodeEditor.IsReadOnly = true;
        this.CodeEditor.ShowLineNumbers = true;

        this._examples = ExampleDiscovery.DiscoverAll();
        this.PopulateTreeView();
    }

    private void PopulateTreeView()
    {
        foreach (var (category, examples) in this._examples.OrderBy(kv => kv.Key))
        {
            var categoryNode = new TreeViewItem { Header = category };

            foreach (var example in examples.OrderBy(e => e.Name))
            {
                var exampleNode = new TreeViewItem
                {
                    Header = example.Name,
                    Tag = example
                };
                categoryNode.Items.Add(exampleNode);
            }

            this.ExampleTreeView.Items.Add(categoryNode);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        this.ExampleTreeView.SelectionChanged += this.OnTreeSelectionChanged;
        this.FileListBox.SelectionChanged += this.OnFileListSelectionChanged;
        this.RunButton.Click += this.OnRunClick;
    }

    private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (this.ExampleTreeView.SelectedItem is not TreeViewItem { Tag: ExampleInfo info })
        {
            this._selectedExample = null;
            this.FileListBox.ItemsSource = null;
            this.RunButton.IsEnabled = false;
            return;
        }

        this._selectedExample = info;
        this.RunButton.IsEnabled = true;

        var fileNames = info.SourceFiles.Select(Path.GetFileName).ToList();
        this.FileListBox.ItemsSource = fileNames;

        if (fileNames.Count > 0 && fileNames[0] is { } firstFile)
        {
            this.FileListBox.SelectedIndex = 0;
            this.LoadSourceFile(firstFile);
        }
    }

    private void OnFileListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (this.FileListBox.SelectedItem is string fileName)
        {
            this.LoadSourceFile(fileName);
        }
    }

    private void LoadSourceFile(string fileName)
    {
        if (this._selectedExample is null)
            return;

        var index = Array.IndexOf(this._selectedExample.SourceFiles.Select(Path.GetFileName).ToArray(), fileName);
        if (index < 0)
            return;

        // Read from disk (development mode — source files are on disk)
        var relativePath = this._selectedExample.SourceFiles[index];
        var filePath = Path.GetFullPath(Path.Combine(ExamplesRoot, relativePath));

        if (!File.Exists(filePath))
        {
            this.CodeEditor.Text = $"// File not found: {filePath}";
            this.StatusText.Text = $"Failed to load: {fileName}";
            return;
        }

        var code = File.ReadAllText(filePath);
        this.CodeEditor.Text = code;
        this.StatusText.Text = $"{this._selectedExample.Name} — {fileName}";
    }

    private void OnRunClick(object? sender, RoutedEventArgs e)
    {
        if (this._selectedExample is null)
            return;

        this.RunButton.IsEnabled = false;
        this.StatusText.Text = $"Running {this._selectedExample.Name}...";

        var example = (IExample)Activator.CreateInstance(this._selectedExample.ExampleType)!;

        Task.Run(() =>
        {
            try
            {
                example.Run();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Example '{this._selectedExample.Name}' failed: {ex}");
            }
            finally
            {
                Dispatcher.UIThread.Post(() =>
                {
                    this.RunButton.IsEnabled = true;
                    this.StatusText.Text = $"{this._selectedExample.Name} — Finished";
                });
            }
        });
    }
}