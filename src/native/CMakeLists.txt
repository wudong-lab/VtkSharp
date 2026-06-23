cmake_minimum_required(VERSION 3.25)

project(vtksharp_native LANGUAGES CXX)

find_package(VTK CONFIG REQUIRED COMPONENTS
  CommonCore
  CommonDataModel
  CommonExecutionModel
  FiltersSources
  RenderingCore
  RenderingOpenGL2
  InteractionStyle
  RenderingUI
)

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

add_library(vtksharp_native SHARED
  ${LIB_SRC}
  ${LIB_HEADERS}
)

target_compile_features(vtksharp_native PRIVATE cxx_std_17)

target_include_directories(vtksharp_native
  PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

target_link_libraries(vtksharp_native
  PRIVATE
    VTK::CommonCore
    VTK::CommonDataModel
    VTK::CommonExecutionModel
    VTK::FiltersSources
    VTK::RenderingCore
    VTK::RenderingOpenGL2
    VTK::InteractionStyle
    VTK::RenderingUI
)

vtk_module_autoinit(
  TARGETS vtksharp_native
  MODULES
    VTK::CommonCore
    VTK::CommonDataModel
    VTK::CommonExecutionModel
    VTK::FiltersSources
    VTK::RenderingCore
    VTK::RenderingOpenGL2
    VTK::InteractionStyle
    VTK::RenderingUI
)
