using VtkSharp.Generator.Core.Vtk;
using VtkSharp.Generator.Core.Whitelist;

namespace VtkSharp.Generator.Tests;

public sealed class WhitelistNormalizerTests
{
    [Fact]
    public void Normalize_AddsDependencyClassesAndBaseClassChain()
    {
        var document = new WhitelistDocument
        {
            Module = "vtkRenderingCore",
            Classes =
            [
                new WhitelistClass
                {
                    Name = "vtkActor",
                    Header = "vtkActor.h",
                    Functions =
                    [
                        new WhitelistFunction
                        {
                            Name = "SetMapper",
                            CppSignature = "void SetMapper(vtkMapper * mapper)",
                            Return = new WhitelistReturn { Type = "void" },
                            Parameters = [new WhitelistParameter { Type = "vtkMapper *", Name = "mapper" }],
                        },
                    ],
                },
            ],
        };
        var hierarchy = new Dictionary<string, VtkHierarchyEntry>(StringComparer.Ordinal)
        {
            ["vtkActor"] = new("vtkActor", "vtkProp3D", "vtkActor.h", "vtkRenderingCore"),
            ["vtkProp3D"] = new("vtkProp3D", "vtkProp", "vtkProp3D.h", "vtkRenderingCore"),
            ["vtkProp"] = new("vtkProp", "vtkObject", "vtkProp.h", "vtkRenderingCore"),
            ["vtkMapper"] = new("vtkMapper", "vtkObject", "vtkMapper.h", "vtkRenderingCore"),
        };

        var normalized = new WhitelistNormalizer().Normalize([document], hierarchy, manualBindingClasses: ["vtkObject"]);

        var renderingCore = Assert.Single(normalized);
        Assert.Equal(["vtkActor", "vtkMapper", "vtkProp", "vtkProp3D"], renderingCore.Classes.Select(item => item.Name));
        var actor = renderingCore.Classes.Single(item => item.Name == "vtkActor");
        Assert.Equal("vtkMapper*", actor.Functions[0].Parameters[0].Type);
        Assert.Equal([], renderingCore.Classes.Single(item => item.Name == "vtkMapper").Functions);
    }

    [Theory]
    [InlineData("vtkTypeBool")]
    [InlineData("vtkTypeUInt32")]
    [InlineData("vtkIdType")]
    [InlineData("vtkMTimeType")]
    public void Normalize_ExcludesVtkScalarTypesFromDependencyClasses(string scalarType)
    {
        var document = new WhitelistDocument
        {
            Module = "vtkCommonCore",
            Classes =
            [
                new WhitelistClass
                {
                    Name = "vtkMinimalStandardRandomSequence",
                    Header = "vtkMinimalStandardRandomSequence.h",
                    Functions =
                    [
                        new WhitelistFunction
                        {
                            Name = "Initialize",
                            CppSignature = $"void Initialize({scalarType} seed)",
                            Return = new WhitelistReturn { Type = "void" },
                            Parameters = [new WhitelistParameter { Type = scalarType, Name = "seed" }],
                        },
                    ],
                },
            ],
        };
        var hierarchy = new Dictionary<string, VtkHierarchyEntry>(StringComparer.Ordinal)
        {
            ["vtkMinimalStandardRandomSequence"] = new("vtkMinimalStandardRandomSequence", "vtkRandomSequence", "vtkMinimalStandardRandomSequence.h", "vtkCommonCore"),
        };

        var normalized = new WhitelistNormalizer().Normalize([document], hierarchy, manualBindingClasses: ["vtkObject"]);

        var commonCore = Assert.Single(normalized);
        Assert.DoesNotContain(commonCore.Classes, c => c.Name == scalarType);
    }
}
