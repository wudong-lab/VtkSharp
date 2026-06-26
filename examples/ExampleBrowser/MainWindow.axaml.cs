using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaEdit.Highlighting;

namespace VtkSharp.ExampleBrowser;

public partial class MainWindow : Window
{
    private static readonly string ExamplesRoot = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples", "ExampleBrowser");

    private Dictionary<string, List<ExampleInfo>> _examples = null!;
    private ExampleInfo? _selectedExample;

    public MainWindow()
    {
        InitializeComponent();

        CodeEditor.Text = "// Select an example from the tree to view its source code.";
        CodeEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");
        CodeEditor.IsReadOnly = true;
        CodeEditor.ShowLineNumbers = true;

        _examples = ExampleDiscovery.DiscoverAll();
        PopulateTreeView();
    }

    private void PopulateTreeView()
    {
        foreach (var (category, examples) in _examples.OrderBy(kv => kv.Key))
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

            ExampleTreeView.Items.Add(categoryNode);
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        ExampleTreeView.SelectionChanged += OnTreeSelectionChanged;
        FileListBox.SelectionChanged += OnFileListSelectionChanged;
        RunButton.Click += OnRunClick;
    }

    private void OnTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ExampleTreeView.SelectedItem is not TreeViewItem { Tag: ExampleInfo info })
        {
            _selectedExample = null;
            FileListBox.ItemsSource = null;
            RunButton.IsEnabled = false;
            return;
        }

        _selectedExample = info;
        RunButton.IsEnabled = true;

        var fileNames = info.SourceFiles.Select(Path.GetFileName).ToList();
        FileListBox.ItemsSource = fileNames;

        if (fileNames.Count > 0 && fileNames[0] is { } firstFile)
        {
            FileListBox.SelectedIndex = 0;
            LoadSourceFile(firstFile);
        }
    }

    private void OnFileListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FileListBox.SelectedItem is string fileName)
        {
            LoadSourceFile(fileName);
        }
    }

    private void LoadSourceFile(string fileName)
    {
        if (_selectedExample is null)
            return;

        var index = Array.IndexOf(_selectedExample.SourceFiles.Select(Path.GetFileName).ToArray(), fileName);
        if (index < 0)
            return;

        // Read from disk (development mode — source files are on disk)
        var relativePath = _selectedExample.SourceFiles[index];
        var filePath = Path.GetFullPath(Path.Combine(ExamplesRoot, relativePath));

        if (!File.Exists(filePath))
        {
            CodeEditor.Text = $"// File not found: {filePath}";
            StatusText.Text = $"Failed to load: {fileName}";
            return;
        }

        var code = File.ReadAllText(filePath);
        CodeEditor.Text = code;
        StatusText.Text = $"{_selectedExample.Name} — {fileName}";
    }

    private void OnRunClick(object? sender, RoutedEventArgs e)
    {
        if (_selectedExample is null)
            return;

        RunButton.IsEnabled = false;
        StatusText.Text = $"Running {_selectedExample.Name}...";

        var example = (IExample)Activator.CreateInstance(_selectedExample.ExampleType)!;

        Task.Run(() =>
        {
            try
            {
                example.Run();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Example '{_selectedExample.Name}' failed: {ex}");
            }
            finally
            {
                Dispatcher.UIThread.Post(() =>
                {
                    RunButton.IsEnabled = true;
                    StatusText.Text = $"{_selectedExample.Name} — Finished";
                });
            }
        });
    }
}
