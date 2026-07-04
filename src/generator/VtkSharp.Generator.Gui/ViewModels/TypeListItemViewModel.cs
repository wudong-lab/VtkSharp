using VtkSharp.Generator.Core.Exporting;

namespace VtkSharp.Generator.Gui.ViewModels;

public sealed class TypeListItemViewModel
{
    public TypeListItemViewModel(ExportableTypeInfo typeInfo)
    {
        this.TypeName = typeInfo.TypeName;
        this.Module = typeInfo.Module;
        this.Header = typeInfo.Header;
    }

    public string TypeName { get; }
    public string Module { get; }
    public string Header { get; }
}
