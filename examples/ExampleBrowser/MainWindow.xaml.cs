using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;

namespace VtkSharp.ExampleBrowser;

public partial class MainWindow : Window
{
    private static readonly string ExamplesRoot = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples", "ExampleBrowser");

    private readonly Dictionary<string, List<ExampleInfo>> _examples;
    private ExampleInfo? _selectedExample;

    public MainWindow()
    {
        this.InitializeComponent();

        this.CodeEditor.Options.EnableHyperlinks = false;
        this.CodeEditor.Options.EnableEmailHyperlinks = false;
        this.CodeEditor.Options.ConvertTabsToSpaces = true;
        this.CodeEditor.Options.IndentationSize = 4;
        this.CodeEditor.Text = "// Select an example from the tree to view its source code.";
        this.CodeEditor.SyntaxHighlighting = CreateDarkCSharpHighlighting();

        this._examples = ExampleDiscovery.DiscoverAll();
        this.PopulateTreeView();

        this.ExampleTreeView.SelectedItemChanged += this.OnTreeSelectionChanged;
        this.FileListBox.SelectionChanged += this.OnFileListSelectionChanged;
        this.RunButton.Click += this.OnRunClick;
    }

    private void PopulateTreeView()
    {
        foreach (var (category, examples) in this._examples.OrderBy(kv => kv.Key))
        {
            var categoryNode = new TreeViewItem { Header = category };

            foreach (var example in examples.OrderBy(e => e.Name))
            {
                categoryNode.Items.Add(new TreeViewItem
                {
                    Header = example.Name,
                    Tag = example
                });
            }

            this.ExampleTreeView.Items.Add(categoryNode);
        }
    }

    private void OnTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
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

    private void OnFileListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (this.FileListBox.SelectedItem is string fileName)
        {
            this.LoadSourceFile(fileName);
        }
    }

    private void LoadSourceFile(string fileName)
    {
        if (this._selectedExample is null) return;

        var sourceFileNames = this._selectedExample.SourceFiles.Select(Path.GetFileName).ToArray();
        var index = Array.IndexOf(sourceFileNames, fileName);
        if (index < 0)
            return;

        var relativePath = this._selectedExample.SourceFiles[index];
        var filePath = Path.GetFullPath(Path.Combine(ExamplesRoot, relativePath));

        if (!File.Exists(filePath))
        {
            this.CodeEditor.Text = $"// File not found: {filePath}";
            this.StatusText.Text = $"Failed to load: {fileName}";
            return;
        }

        this.CodeEditor.Text = File.ReadAllText(filePath);
        this.StatusText.Text = $"{this._selectedExample.Name} - {fileName}";
    }

    private static IHighlightingDefinition CreateDarkCSharpHighlighting()
    {
        using var stream = typeof(HighlightingManager).Assembly
            .GetManifestResourceStream("ICSharpCode.AvalonEdit.Highlighting.Resources.CSharp-Mode.xshd")
            ?? throw new InvalidOperationException("AvalonEdit C# highlighting definition was not found.");
        using var reader = new System.Xml.XmlTextReader(stream);
        var highlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);

        foreach (var color in highlighting.NamedHighlightingColors)
        {
            color.Foreground = color.Name switch
            {
                "Comment" => Brush("#8B949E"),
                "String" or "Char" => Brush("#A5D6FF"),
                "Preprocessor" => Brush("#FFA657"),
                "NumberLiteral" or "Number" => Brush("#79C0FF"),
                "Keywords" or "ValueTypeKeywords" or "ReferenceTypeKeywords" or "NullOrValueKeywords" => Brush("#FF7B72"),
                "ThisOrBaseReference" => Brush("#D2A8FF"),
                "MethodCall" => Brush("#D2A8FF"),
                "TypeKeywords" => Brush("#FFA657"),
                _ => color.Foreground
            };
        }

        return highlighting;
    }

    private static SimpleHighlightingBrush Brush(string hex)
        => new((Color)ColorConverter.ConvertFromString(hex));

    private async void OnRunClick(object sender, RoutedEventArgs e)
    {
        if (this._selectedExample is null) return;

        this.RunButton.IsEnabled = false;
        this.StatusText.Text = $"Running {this._selectedExample.Name}...";

        var selectedExample = this._selectedExample;
        var example = (IExample)Activator.CreateInstance(selectedExample.ExampleType)!;

        await Task.Run(() =>
        {
            try
            {
                example.Run();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Example '{selectedExample.Name}' failed: {ex}");
            }
        });

        this.RunButton.IsEnabled = true;
        this.StatusText.Text = $"{selectedExample.Name} - Finished";
    }
}
