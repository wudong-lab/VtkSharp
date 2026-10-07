using VtkSharp.Generator.Core.Generation;

namespace VtkSharp.Generator.Tests;

[TestClass]
public sealed class NativeProjectEmitterTests
{
    [TestMethod]
    public void EmitCMakeLists_UsesGeneratedModuleVariables()
    {
        var emitter = new NativeProjectEmitter();

        var text = emitter.EmitCMakeLists("VtkSharp.Native");

        Assert.Contains("include(${CMAKE_CURRENT_SOURCE_DIR}/vtksharp.modules.generated.cmake)", text);
        Assert.Contains("find_package(VTK CONFIG REQUIRED COMPONENTS ${VTKSHARP_ALL_VTK_COMPONENTS})", text);
        Assert.Contains("foreach(VTKSHARP_NATIVE_TARGET IN LISTS VTKSHARP_NATIVE_TARGETS)", text);
        Assert.Contains("add_library(${VTKSHARP_NATIVE_TARGET} SHARED", text);
        Assert.Contains("target_link_libraries(${VTKSHARP_NATIVE_TARGET} PRIVATE ${VTKSHARP_MODULE_TARGETS} ${VTKSHARP_EXTRA_NATIVE_LIBRARIES})", text);
        Assert.Contains("MSVC_RUNTIME_LIBRARY", text);
        Assert.Contains("vtk_module_autoinit(", text);
        Assert.IsTrue(text.EndsWith("\n", StringComparison.Ordinal));
    }

    [TestMethod]
    public void EmitCMakePresets_ProvidesVisualStudioPresets()
    {
        var emitter = new NativeProjectEmitter();

        var text = emitter.EmitCMakePresets();

        Assert.Contains("\"version\": 6", text);
        Assert.Contains("\"win-x64-vs2026\"", text);
        Assert.Contains("\"Visual Studio 18 2026\"", text);
        Assert.Contains("\"win-x64-vs2022\"", text);
        Assert.Contains("\"win-x64-vs2026-dynamic\"", text);
        Assert.Contains("out/build/dynamic/win-x64-vs2026", text);
        Assert.Contains("\"Visual Studio 17 2022\"", text);
        Assert.Contains("\"win-x64-vs2026-debug\"", text);
        Assert.Contains("\"win-x64-vs2022-debug\"", text);
        Assert.Contains("\"win-x64-vs2026-dynamic-release\"", text);
        Assert.IsTrue(text.EndsWith("\n", StringComparison.Ordinal));
    }

    [TestMethod]
    public void EmitApiHeader_ExportsCAbiMacro()
    {
        var emitter = new NativeProjectEmitter();

        var text = emitter.EmitApiHeader();

        Assert.Contains("#pragma once", text);
        Assert.Contains("#define VTKSHARP_API extern \"C\" __declspec(dllexport)", text);
        Assert.Contains("__attribute__((visibility(\"default\")))", text);
    }

}
