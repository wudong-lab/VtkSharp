namespace VtkSharp.Generator.Core.Generation;

public sealed class NativeProjectEmitter
{
    public string EmitCMakeLists(string nativeLibraryName)
        => $$"""
           cmake_minimum_required(VERSION 3.25)

           set(VTKSHARP_NATIVE_TARGET {{nativeLibraryName}})

           project(${VTKSHARP_NATIVE_TARGET} LANGUAGES CXX)

           include(${CMAKE_CURRENT_SOURCE_DIR}/vtksharp.modules.generated.cmake)

           set(VTKSHARP_EXTRA_NATIVE_SOURCES "" CACHE STRING "Additional native source files")
           set(VTKSHARP_EXTRA_NATIVE_HEADERS "" CACHE STRING "Additional native header files")
           set(VTKSHARP_EXTRA_INCLUDE_DIRECTORIES "" CACHE STRING "Additional native include directories")
           set(VTKSHARP_EXTRA_VTK_COMPONENTS "" CACHE STRING "Additional VTK components")
           set(VTKSHARP_EXTRA_NATIVE_LIBRARIES "" CACHE STRING "Additional native libraries")

           set(VTKSHARP_ALL_VTK_COMPONENTS
             ${VTKSHARP_VTK_COMPONENTS}
             ${VTKSHARP_EXTRA_VTK_COMPONENTS}
           )
           list(REMOVE_DUPLICATES VTKSHARP_ALL_VTK_COMPONENTS)

           set(VTKSHARP_EXTRA_VTK_TARGETS "")
           foreach(VTKSHARP_EXTRA_VTK_COMPONENT IN LISTS VTKSHARP_EXTRA_VTK_COMPONENTS)
             list(APPEND VTKSHARP_EXTRA_VTK_TARGETS "VTK::${VTKSHARP_EXTRA_VTK_COMPONENT}")
           endforeach()

           set(VTKSHARP_ALL_VTK_TARGETS
             ${VTKSHARP_VTK_TARGETS}
             ${VTKSHARP_EXTRA_VTK_TARGETS}
           )
           list(REMOVE_DUPLICATES VTKSHARP_ALL_VTK_TARGETS)

           find_package(VTK CONFIG REQUIRED COMPONENTS ${VTKSHARP_ALL_VTK_COMPONENTS})

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

           add_library(${VTKSHARP_NATIVE_TARGET} SHARED
             ${LIB_SRC}
             ${LIB_HEADERS}
             ${VTKSHARP_EXTRA_NATIVE_SOURCES}
             ${VTKSHARP_EXTRA_NATIVE_HEADERS}
           )

           if(VTKSHARP_EXTRA_NATIVE_SOURCES OR VTKSHARP_EXTRA_NATIVE_HEADERS)
             source_group("extensions" FILES
               ${VTKSHARP_EXTRA_NATIVE_SOURCES}
               ${VTKSHARP_EXTRA_NATIVE_HEADERS}
             )
           endif()

           target_compile_features(${VTKSHARP_NATIVE_TARGET} PRIVATE cxx_std_17)

           target_include_directories(${VTKSHARP_NATIVE_TARGET}
             PRIVATE
               ${CMAKE_CURRENT_SOURCE_DIR}/include
               ${VTKSHARP_EXTRA_INCLUDE_DIRECTORIES}
           )

           target_link_libraries(${VTKSHARP_NATIVE_TARGET}
             PRIVATE
               ${VTKSHARP_ALL_VTK_TARGETS}
               ${VTKSHARP_EXTRA_NATIVE_LIBRARIES}
           )

           vtk_module_autoinit(
             TARGETS ${VTKSHARP_NATIVE_TARGET}
             MODULES
               ${VTKSHARP_ALL_VTK_TARGETS}
           )
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
