cmake_minimum_required(VERSION 3.25)

project(vtksharp_native LANGUAGES CXX)

find_package(VTK CONFIG REQUIRED COMPONENTS
  CommonCore
  CommonDataModel
  CommonExecutionModel
  FiltersSources
  RenderingCore
)

file(GLOB_RECURSE LIB_SRC CONFIGURE_DEPENDS
    "${CMAKE_CURRENT_SOURCE_DIR}/src/*.cpp"
)

add_library(vtksharp_native SHARED
  ${LIB_SRC}
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
)
