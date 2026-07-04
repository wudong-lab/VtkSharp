namespace VtkSharp.Generator.Gui.ViewModels;

public sealed class FunctionGroupViewModel
{
    public FunctionGroupViewModel(string declaringTypeName, IReadOnlyList<FunctionItemViewModel> functions)
    {
        this.DeclaringTypeName = declaringTypeName;
        this.Functions = functions;
    }

    public string DeclaringTypeName { get; }
    public IReadOnlyList<FunctionItemViewModel> Functions { get; }
}
