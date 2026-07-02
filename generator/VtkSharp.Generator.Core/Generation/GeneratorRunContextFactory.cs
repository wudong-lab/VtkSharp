using VtkSharp.Generator.Core.Configuration;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;

namespace VtkSharp.Generator.Core.Generation;

public sealed class GeneratorRunContextFactory
{
    public GeneratorRunContext? Create(string configPath, TextWriter error)
    {
        var workspace = GeneratorWorkspace.Load(configPath);
        if (workspace.IncludeDirectory is null)
        {
            error.WriteLine("VTK include directory was not found. Set VTK_ROOT or vtk.includeDirectory in local config.");
            return null;
        }

        var documents = workspace.LoadWhitelist();
        var hierarchyResolver = workspace.LoadHierarchyResolver();
        var inspector = new VtkClassInspector();
        var inspectedClasses = new Dictionary<string, InspectedClass>(StringComparer.Ordinal);
        var diagnostics = new List<ValidationDiagnostic>();

        foreach (var whitelistClass in documents.SelectMany(document => document.Classes))
        {
            try
            {
                inspectedClasses[whitelistClass.Name] = inspector.InspectHeader(workspace.IncludeDirectory, whitelistClass.Header, whitelistClass.Name);
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or ArgumentException)
            {
                diagnostics.Add(new ValidationDiagnostic($"Class '{whitelistClass.Name}' could not be inspected from '{whitelistClass.Header}': {ex.Message}"));
            }
        }

        return new GeneratorRunContext(
            workspace,
            documents,
            hierarchyResolver,
            inspectedClasses,
            diagnostics);
    }
}
