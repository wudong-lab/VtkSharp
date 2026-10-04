namespace VtkSharp.Generator.Core.Generation;

public sealed class NativeProjectEmitter
{
    public string EmitCMakeLists(string nativeLibraryName)
        => $$$"""
           cmake_minimum_required(VERSION 3.25)

           project({{{nativeLibraryName}}} LANGUAGES CXX)

           include(${CMAKE_CURRENT_SOURCE_DIR}/vtksharp.modules.generated.cmake)

           find_package(VTK CONFIG REQUIRED COMPONENTS ${VTKSHARP_VTK_COMPONENTS})

           foreach(VTKSHARP_NATIVE_TARGET IN LISTS VTKSHARP_NATIVE_TARGETS)
             string(REPLACE "." "_" VTKSHARP_TARGET_KEY "${VTKSHARP_NATIVE_TARGET}")
             string(REPLACE "-" "_" VTKSHARP_TARGET_KEY "${VTKSHARP_TARGET_KEY}")
             set(VTKSHARP_MODULES_VARIABLE "VTKSHARP_NATIVE_TARGET_${VTKSHARP_TARGET_KEY}_MODULES")
             set(VTKSHARP_AUTOINIT_VARIABLE "VTKSHARP_NATIVE_TARGET_${VTKSHARP_TARGET_KEY}_AUTOINIT_MODULES")
             set(VTKSHARP_SOURCES_VARIABLE "VTKSHARP_NATIVE_TARGET_${VTKSHARP_TARGET_KEY}_SOURCES")

             set(VTKSHARP_MODULE_TARGETS "")
             foreach(VTKSHARP_COMPONENT IN LISTS ${VTKSHARP_MODULES_VARIABLE})
               list(APPEND VTKSHARP_MODULE_TARGETS "VTK::${VTKSHARP_COMPONENT}")
             endforeach()
             set(VTKSHARP_AUTOINIT_TARGETS "")
             foreach(VTKSHARP_COMPONENT IN LISTS ${VTKSHARP_AUTOINIT_VARIABLE})
               list(APPEND VTKSHARP_AUTOINIT_TARGETS "VTK::${VTKSHARP_COMPONENT}")
             endforeach()

             add_library(${VTKSHARP_NATIVE_TARGET} SHARED ${${VTKSHARP_SOURCES_VARIABLE}})
             target_compile_features(${VTKSHARP_NATIVE_TARGET} PRIVATE cxx_std_17)
             target_include_directories(${VTKSHARP_NATIVE_TARGET} PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/include)
             target_link_libraries(${VTKSHARP_NATIVE_TARGET} PRIVATE ${VTKSHARP_MODULE_TARGETS})

             if(MSVC)
               set_property(TARGET ${VTKSHARP_NATIVE_TARGET}
                 PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL"
               )
             endif()

             vtk_module_autoinit(
               TARGETS ${VTKSHARP_NATIVE_TARGET}
               MODULES ${VTKSHARP_AUTOINIT_TARGETS}
             )
           endforeach()
           """ + "\n";

    public string EmitCMakePresets()
        => """
           {
             "version": 6,
             "configurePresets": [
               {
                 "name": "win-x64-vs2026",
                 "displayName": "Windows x64 (Visual Studio 2026)",
                 "generator": "Visual Studio 18 2026",
                 "architecture": "x64",
                 "binaryDir": "${sourceDir}/out/build/win-x64-vs2026"
               },
               {
                 "name": "win-x64-vs2022",
                 "displayName": "Windows x64 (Visual Studio 2022)",
                 "generator": "Visual Studio 17 2022",
                 "architecture": "x64",
                 "binaryDir": "${sourceDir}/out/build/win-x64-vs2022"
               },
               {
                 "name": "win-x64-vs2026-dynamic",
                 "displayName": "Windows x64 dynamic VTK (Visual Studio 2026)",
                 "generator": "Visual Studio 18 2026",
                 "architecture": "x64",
                 "binaryDir": "${sourceDir}/out/build/dynamic/win-x64-vs2026"
               },
               {
                 "name": "win-x64-vs2022-dynamic",
                 "displayName": "Windows x64 dynamic VTK (Visual Studio 2022)",
                 "generator": "Visual Studio 17 2022",
                 "architecture": "x64",
                 "binaryDir": "${sourceDir}/out/build/dynamic/win-x64-vs2022"
               }
             ],
             "buildPresets": [
               {
                 "name": "win-x64-vs2026-debug",
                 "configurePreset": "win-x64-vs2026",
                 "configuration": "Debug"
               },
               {
                 "name": "win-x64-vs2026-release",
                 "configurePreset": "win-x64-vs2026",
                 "configuration": "Release"
               },
               {
                 "name": "win-x64-vs2022-debug",
                 "configurePreset": "win-x64-vs2022",
                 "configuration": "Debug"
               },
               {
                 "name": "win-x64-vs2022-release",
                 "configurePreset": "win-x64-vs2022",
                 "configuration": "Release"
               },
               {
                 "name": "win-x64-vs2026-dynamic-debug",
                 "configurePreset": "win-x64-vs2026-dynamic",
                 "configuration": "Debug"
               },
               {
                 "name": "win-x64-vs2026-dynamic-release",
                 "configurePreset": "win-x64-vs2026-dynamic",
                 "configuration": "Release"
               },
               {
                 "name": "win-x64-vs2022-dynamic-debug",
                 "configurePreset": "win-x64-vs2022-dynamic",
                 "configuration": "Debug"
               },
               {
                 "name": "win-x64-vs2022-dynamic-release",
                 "configurePreset": "win-x64-vs2022-dynamic",
                 "configuration": "Release"
               }
             ]
           }
           """ + "\n";

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
