using VtkSharp.Generator.Core.Configuration;
using VtkSharp.Generator.Core.Inspection;
using VtkSharp.Generator.Core.Validation;
using VtkSharp.Generator.Core.Vtk;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Core.Generation;

public sealed record GeneratorRunContext(
    GeneratorWorkspace Workspace,
    IReadOnlyList<WhitelistDocument> Documents,
    VtkHierarchyResolver HierarchyResolver,
    IReadOnlyDictionary<string, InspectedClass> InspectedClasses,
    IReadOnlyList<ValidationDiagnostic> InspectionDiagnostics);
