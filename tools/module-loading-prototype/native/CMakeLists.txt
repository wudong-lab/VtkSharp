cmake_minimum_required(VERSION 3.25)
project(VtkSharp.ModuleLoadingPrototype LANGUAGES CXX)

find_package(VTK CONFIG REQUIRED COMPONENTS
  CommonCore
  FiltersSources
  InteractionStyle
  RenderingCore
  RenderingLabel
  RenderingOpenGL2
  RenderingUI
)

function(add_prototype_target target source)
  add_library(${target} SHARED ${source})
  target_compile_features(${target} PRIVATE cxx_std_17)
  target_link_libraries(${target} PRIVATE ${ARGN})
endfunction()

add_prototype_target(VtkSharp.Native.CommonCore commoncore.cpp VTK::CommonCore)
add_prototype_target(VtkSharp.Native.FiltersSources filters.cpp VTK::FiltersSources)
add_prototype_target(VtkSharp.Native.Rendering rendering.cpp
  VTK::InteractionStyle
  VTK::RenderingCore
  VTK::RenderingLabel
  VTK::RenderingOpenGL2
  VTK::RenderingUI
)

vtk_module_autoinit(
  TARGETS VtkSharp.Native.CommonCore VtkSharp.Native.FiltersSources
  MODULES VTK::CommonCore VTK::FiltersSources
)
vtk_module_autoinit(
  TARGETS VtkSharp.Native.Rendering
  MODULES VTK::InteractionStyle VTK::RenderingCore VTK::RenderingLabel
    VTK::RenderingOpenGL2 VTK::RenderingUI
)
