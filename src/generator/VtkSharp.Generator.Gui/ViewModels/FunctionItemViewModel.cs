using CommunityToolkit.Mvvm.ComponentModel;
using VtkSharp.Generator.Core.Exporting;

namespace VtkSharp.Generator.Gui.ViewModels;

public sealed partial class FunctionItemViewModel : ObservableObject
{
    public FunctionItemViewModel(ExportFunctionCandidate candidate)
    {
        this.Candidate = candidate;
    }

    public ExportFunctionCandidate Candidate { get; }
    public string Signature => this.Candidate.Signature;
    public string? Reason => this.Candidate.Reason;
    public bool CanSelectForExport => this.Candidate.CanSelectForExport;

    [ObservableProperty]
    private bool _isSelected;
}
