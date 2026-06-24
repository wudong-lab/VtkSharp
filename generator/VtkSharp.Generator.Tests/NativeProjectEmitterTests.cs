using VtkSharp.Generator.Core.Generation;

namespace VtkSharp.Generator.Tests;

public sealed class NativeProjectEmitterTests
{
    [Fact]
    public void EmitCMakeLists_UsesGeneratedModuleVariables()
    {
        var emitter = new NativeProjectEmitter();

        var text = emitter.EmitCMakeLists("vtksharp_native");

        Assert.Contains("include(${CMAKE_CURRENT_SOURCE_DIR}/vtksharp.modules.generated.cmake)", text);
        Assert.Contains("find_package(VTK CONFIG REQUIRED COMPONENTS ${VTKSHARP_VTK_COMPONENTS})", text);
        Assert.Contains("target_link_libraries(vtksharp_native", text);
        Assert.Contains("${VTKSHARP_VTK_TARGETS}", text);
        Assert.Contains("vtk_module_autoinit(", text);
        Assert.EndsWith("\n", text);
    }

    [Fact]
    public void CMakeModulesEmitter_EmitsSortedDistinctComponentsAndTargets()
    {
        var emitter = new CMakeModulesEmitter();

        var text = emitter.Emit(
        [
            "vtkRenderingCore",
            "vtkCommonCore",
            "vtkRenderingOpenGL2",
            "vtkRenderingCore",
        ]);

        Assert.Contains("  CommonCore", text);
        Assert.Contains("  RenderingCore", text);
        Assert.Contains("  RenderingOpenGL2", text);
        Assert.Contains("  VTK::RenderingOpenGL2", text);
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.None);
        Assert.Equal(1, lines.Count(line => line == "  RenderingCore"));
        Assert.Equal(1, lines.Count(line => line == "  VTK::RenderingCore"));
    }

    [Fact]
    public void EmitCMakePresets_ProvidesVisualStudioPresets()
    {
        var emitter = new NativeProjectEmitter();

        var text = emitter.EmitCMakePresets();

        Assert.Contains("\"version\": 6", text);
        Assert.Contains("\"windows-x64\"", text);
        Assert.Contains("\"Visual Studio 18 2026\"", text);
        Assert.Contains("\"windows-x64-vs2022\"", text);
        Assert.Contains("\"Visual Studio 17 2022\"", text);
    }

    [Fact]
    public void EmitApiHeader_ExportsCAbiMacro()
    {
        var emitter = new NativeProjectEmitter();

        var text = emitter.EmitApiHeader();

        Assert.Contains("#pragma once", text);
        Assert.Contains("#define VTKSHARP_API extern \"C\" __declspec(dllexport)", text);
        Assert.Contains("__attribute__((visibility(\"default\")))", text);
    }

}
