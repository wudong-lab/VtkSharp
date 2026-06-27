namespace VtkSharp.Generator.Core.Generation;

public sealed class NativeProjectEmitter
{
    public string EmitCMakeLists(string nativeLibraryName)
        => $$"""
           cmake_minimum_required(VERSION 3.25)

           project({{nativeLibraryName}} LANGUAGES CXX)

           include(${CMAKE_CURRENT_SOURCE_DIR}/vtksharp.modules.generated.cmake)

           find_package(VTK CONFIG REQUIRED COMPONENTS ${VTKSHARP_VTK_COMPONENTS})

           file(GLOB_RECURSE LIB_SRC CONFIGURE_DEPENDS
               "${CMAKE_CURRENT_SOURCE_DIR}/src/*.cpp"
           )

           file(GLOB_RECURSE LIB_HEADERS CONFIGURE_DEPENDS
               "${CMAKE_CURRENT_SOURCE_DIR}/include/*.h"
           )

           source_group(
               TREE "${CMAKE_CURRENT_SOURCE_DIR}/src"
               PREFIX "src"
               FILES ${LIB_SRC}
           )

           source_group(
               TREE "${CMAKE_CURRENT_SOURCE_DIR}/include"
               PREFIX "include"
               FILES ${LIB_HEADERS}
           )

           add_library({{nativeLibraryName}} SHARED
             ${LIB_SRC}
             ${LIB_HEADERS}
           )

           target_compile_features({{nativeLibraryName}} PRIVATE cxx_std_17)

           target_include_directories({{nativeLibraryName}}
             PRIVATE
               ${CMAKE_CURRENT_SOURCE_DIR}/include
           )

           target_link_libraries({{nativeLibraryName}}
             PRIVATE
               ${VTKSHARP_VTK_TARGETS}
           )

           vtk_module_autoinit(
             TARGETS {{nativeLibraryName}}
             MODULES
               ${VTKSHARP_VTK_TARGETS}
           )
           """ + "\n";

    public string EmitCMakePresets()
        => """
           {
             "version": 6,
             "configurePresets": [
               {
                 "name": "windows-x64-vs2026",
                 "displayName": "Windows x64 (Visual Studio 2026)",
                 "generator": "Visual Studio 18 2026",
                 "architecture": "x64",
                 "binaryDir": "${sourceDir}/out/build/windows-x64-vs2026"
               },
               {
                 "name": "windows-x64-vs2022",
                 "displayName": "Windows x64 (Visual Studio 2022)",
                 "generator": "Visual Studio 17 2022",
                 "architecture": "x64",
                 "binaryDir": "${sourceDir}/out/build/windows-x64-vs2022"
               }
             ],
             "buildPresets": [
               {
                 "name": "windows-x64-vs2026-debug",
                 "configurePreset": "windows-x64-vs2026",
                 "configuration": "Debug"
               },
               {
                 "name": "windows-x64-vs2026-release",
                 "configurePreset": "windows-x64-vs2026",
                 "configuration": "Release"
               },
               {
                 "name": "windows-x64-vs2022-debug",
                 "configurePreset": "windows-x64-vs2022",
                 "configuration": "Debug"
               },
               {
                 "name": "windows-x64-vs2022-release",
                 "configurePreset": "windows-x64-vs2022",
                 "configuration": "Release"
               }
             ]
           }
           """;

    public string EmitApiHeader()
        => """
           #pragma once

           #if defined(_WIN32)
           #define VTKSHARP_API extern "C" __declspec(dllexport)
           #else
           #define VTKSHARP_API extern "C" __attribute__((visibility("default")))
           #endif
           """;
}
