using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using VtkSharp.Generator.Core.Exporting;
using VtkSharp.Generator.Core.Generation;

namespace VtkSharp.Generator.Gui.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly ExportInventoryService _inventoryService = new();
    private readonly ObservableCollection<TypeListItemViewModel> _allTypes = [];
    private readonly List<FunctionGroupViewModel> _allExportedGroups = [];
    private readonly List<FunctionGroupViewModel> _allAvailableGroups = [];
    private readonly List<FunctionGroupViewModel> _allUnsupportedGroups = [];

    public MainWindowViewModel()
    {
        this.ConfigPath = FindDefaultConfigPath();
    }

    public ObservableCollection<TypeListItemViewModel> Types { get; } = [];
    public ObservableCollection<FunctionGroupViewModel> ExportedGroups { get; } = [];
    public ObservableCollection<FunctionGroupViewModel> AvailableGroups { get; } = [];
    public ObservableCollection<FunctionGroupViewModel> UnsupportedGroups { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadTypesCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplyYamlCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunGeneratorCommand))]
    private string configPath = "";

    [ObservableProperty]
    private string typeSearchText = "";

    [ObservableProperty]
    private string exportedSearchText = "";

    [ObservableProperty]
    private string availableSearchText = "";

    [ObservableProperty]
    private string unsupportedSearchText = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadTypeInventoryCommand))]
    private TypeListItemViewModel? selectedType;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isUnsupportedExpanded;

    [ObservableProperty]
    private string logText = "";

    partial void OnTypeSearchTextChanged(string value) => this.RefreshTypes();
    partial void OnExportedSearchTextChanged(string value) => this.RefreshExportedGroups();
    partial void OnAvailableSearchTextChanged(string value) => this.RefreshAvailableGroups();
    partial void OnUnsupportedSearchTextChanged(string value) => this.RefreshUnsupportedGroups();

    partial void OnSelectedTypeChanged(TypeListItemViewModel? value)
    {
        if (value is not null)
            _ = this.LoadTypeInventoryCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private void BrowseConfig()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select generator config",
            Filter = "YAML files (*.yml;*.yaml)|*.yml;*.yaml|All files (*.*)|*.*",
            FileName = string.IsNullOrWhiteSpace(this.ConfigPath) ? "vtksharp.generator.yml" : this.ConfigPath,
        };

        if (dialog.ShowDialog() == true)
            this.ConfigPath = dialog.FileName;
    }

    [RelayCommand(CanExecute = nameof(CanLoadTypes))]
    private async Task LoadTypesAsync()
    {
        await this.RunBusyAsync(async () =>
        {
            this.AppendLog($"Loading types from {this.ConfigPath}");
            var configPath = this.ConfigPath;
            var types = await Task.Run(() => this._inventoryService.ListTypes(configPath));
            this._allTypes.Clear();
            foreach (var type in types)
                this._allTypes.Add(new TypeListItemViewModel(type));

            this.RefreshTypes();
            this.AppendLog($"Loaded {types.Count} type(s).");
        });
    }

    [RelayCommand(CanExecute = nameof(CanLoadTypeInventory))]
    private async Task LoadTypeInventoryAsync()
    {
        if (this.SelectedType is null)
            return;

        await this.RunBusyAsync(async () =>
        {
            await this.LoadTypeInventoryForSelectedTypeAsync();
        });
    }

    [RelayCommand(CanExecute = nameof(CanApplyOrRun))]
    private async Task ApplyYamlAsync()
    {
        await this.RunBusyAsync(async () =>
        {
            var plan = this.CreateCurrentPlan();
            if (plan.Functions.Count == 0)
            {
                this.AppendLog("No selected functions to apply.");
                return;
            }

            foreach (var diagnostic in plan.Diagnostics)
                this.AppendLog(diagnostic);

            var configPath = this.ConfigPath;
            await Task.Run(() => this._inventoryService.ApplyPlanToWhitelist(configPath, plan));
            this.AppendLog("Whitelist YAML updated. Review the changes with git diff.");
        });
    }

    [RelayCommand(CanExecute = nameof(CanApplyOrRun))]
    private async Task RunGeneratorAsync()
    {
        await this.RunBusyAsync(async () =>
        {
            var plan = this.CreateCurrentPlan();
            if (plan.Functions.Count == 0)
            {
                this.AppendLog("No selected functions to apply.");
                return;
            }

            foreach (var diagnostic in plan.Diagnostics)
                this.AppendLog(diagnostic);

            var configPath = this.ConfigPath;
            await Task.Run(() => this._inventoryService.ApplyPlanToWhitelist(configPath, plan));
            this.AppendLog("Whitelist YAML updated.");

            var outputRoot = GetDefaultOutputRoot(configPath);
            var result = await Task.Run(() =>
            {
                using var output = new StringWriter();
                using var error = new StringWriter();
                var exitCode = new BindingGenerationService().Generate(
                    configPath,
                    outputRoot,
                    continueOnError: false,
                    incremental: true,
                    force: false,
                    output,
                    error);
                return (ExitCode: exitCode, Output: output.ToString(), Error: error.ToString());
            });

            AppendMultiline(result.Output);
            AppendMultiline(result.Error);

            if (result.ExitCode == 0)
            {
                this.AppendLog("Generation succeeded. Rescanning current type.");
                if (this.SelectedType is not null)
                    await this.LoadTypeInventoryForSelectedTypeAsync();
            }
            else
            {
                this.AppendLog($"Generation failed with exit code {result.ExitCode}. YAML changes were not rolled back.");
            }
        });
    }

    private bool CanLoadTypes() => !this.IsBusy && File.Exists(this.ConfigPath);
    private bool CanLoadTypeInventory() => !this.IsBusy && this.SelectedType is not null && File.Exists(this.ConfigPath);
    private bool CanApplyOrRun() => !this.IsBusy && File.Exists(this.ConfigPath);

    private ExportPlan CreateCurrentPlan()
        => this._inventoryService.CreatePlan(this.GetSelectedFunctions().Select(function => function.Candidate));

    private async Task LoadTypeInventoryForSelectedTypeAsync()
    {
        if (this.SelectedType is null)
            return;

        var typeName = this.SelectedType.TypeName;
        this.AppendLog($"Loading inventory for {typeName}");
        var configPath = this.ConfigPath;
        var inventory = await Task.Run(() => this._inventoryService.GetTypeInventory(configPath, typeName));
        this._allExportedGroups.ReplaceWith(ToGroupViewModels(inventory.AlreadyExported));
        this._allAvailableGroups.ReplaceWith(ToGroupViewModels(inventory.AvailableToAdd));
        this._allUnsupportedGroups.ReplaceWith(ToGroupViewModels(inventory.Unsupported));
        this.RefreshExportedGroups();
        this.RefreshAvailableGroups();
        this.RefreshUnsupportedGroups();
        this.AppendLog(
            $"Loaded {typeName}: {CountFunctions(this._allExportedGroups)} exported, " +
            $"{CountFunctions(this._allAvailableGroups)} available, {CountFunctions(this._allUnsupportedGroups)} unsupported.");
    }

    private IEnumerable<FunctionItemViewModel> GetSelectedFunctions()
        => this._allAvailableGroups
            .SelectMany(group => group.Functions)
            .Where(function => function.IsSelected && function.CanSelectForExport);

    private async Task RunBusyAsync(Func<Task> action)
    {
        if (this.IsBusy)
            return;

        try
        {
            this.IsBusy = true;
            this.LoadTypesCommand.NotifyCanExecuteChanged();
            this.LoadTypeInventoryCommand.NotifyCanExecuteChanged();
            this.ApplyYamlCommand.NotifyCanExecuteChanged();
            this.RunGeneratorCommand.NotifyCanExecuteChanged();
            await action();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
        {
            this.AppendLog($"Error: {ex.Message}");
        }
        finally
        {
            this.IsBusy = false;
            this.LoadTypesCommand.NotifyCanExecuteChanged();
            this.LoadTypeInventoryCommand.NotifyCanExecuteChanged();
            this.ApplyYamlCommand.NotifyCanExecuteChanged();
            this.RunGeneratorCommand.NotifyCanExecuteChanged();
        }
    }

    private void RefreshTypes()
    {
        var selectedTypeName = this.SelectedType?.TypeName;
        var items = this._allTypes
            .Where(type => IsMatch(type.TypeName, this.TypeSearchText))
            .ToList();
        ReplaceCollection(this.Types, items);
        this.SelectedType = this.Types.FirstOrDefault(type => type.TypeName == selectedTypeName);
    }

    private void RefreshExportedGroups()
        => ReplaceCollection(this.ExportedGroups, FilterGroups(this._allExportedGroups, this.ExportedSearchText));

    private void RefreshAvailableGroups()
        => ReplaceCollection(this.AvailableGroups, FilterGroups(this._allAvailableGroups, this.AvailableSearchText));

    private void RefreshUnsupportedGroups()
        => ReplaceCollection(this.UnsupportedGroups, FilterGroups(this._allUnsupportedGroups, this.UnsupportedSearchText));

    private static IReadOnlyList<FunctionGroupViewModel> ToGroupViewModels(IReadOnlyList<FunctionExportGroup> groups)
        => groups
            .Select(group => new FunctionGroupViewModel(
                group.DeclaringTypeName,
                group.Functions.Select(function => new FunctionItemViewModel(function)).ToList()))
            .ToList();

    private static IReadOnlyList<FunctionGroupViewModel> FilterGroups(
        IReadOnlyList<FunctionGroupViewModel> groups,
        string searchText)
        => groups
            .Select(group => new FunctionGroupViewModel(
                group.DeclaringTypeName,
                group.Functions.Where(function => IsMatch(function.Signature, searchText)).ToList()))
            .Where(group => group.Functions.Count > 0)
            .ToList();

    private static bool IsMatch(string text, string pattern)
        => string.IsNullOrWhiteSpace(pattern) ||
           text.Contains(pattern, StringComparison.OrdinalIgnoreCase);

    private void AppendLog(string message)
    {
        this.LogText += $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
    }

    private void AppendMultiline(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        foreach (var line in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            if (!string.IsNullOrWhiteSpace(line))
                this.AppendLog(line);
        }
    }

    private static int CountFunctions(IEnumerable<FunctionGroupViewModel> groups)
        => groups.Sum(group => group.Functions.Count);

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
            collection.Add(item);
    }

    private static string FindDefaultConfigPath()
    {
        var current = new DirectoryInfo(Environment.CurrentDirectory);
        while (current is not null)
        {
            var fromRepoRoot = Path.Combine(current.FullName, "src", "generator", "config", "vtksharp.generator.yml");
            if (File.Exists(fromRepoRoot))
                return fromRepoRoot;

            var fromGeneratorRoot = Path.Combine(current.FullName, "config", "vtksharp.generator.yml");
            if (File.Exists(fromGeneratorRoot))
                return fromGeneratorRoot;

            current = current.Parent;
        }

        return Path.GetFullPath(Path.Combine("src", "generator", "config", "vtksharp.generator.yml"));
    }

    private static string GetDefaultOutputRoot(string configPath)
    {
        var configDirectory = Path.GetDirectoryName(Path.GetFullPath(configPath))
                              ?? throw new InvalidOperationException($"Config path '{configPath}' does not have a directory.");
        return Path.GetFullPath(Path.Combine(configDirectory, "..", ".."));
    }
}

file static class ListExtensions
{
    public static void ReplaceWith<T>(this List<T> list, IEnumerable<T> items)
    {
        list.Clear();
        list.AddRange(items);
    }
}
