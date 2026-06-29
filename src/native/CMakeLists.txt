cmake_minimum_required(VERSION 3.25)

set(VTKSHARP_NATIVE_TARGET VtkSharp.Native)

project(${VTKSHARP_NATIVE_TARGET} LANGUAGES CXX)

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

add_library(${VTKSHARP_NATIVE_TARGET} SHARED
  ${LIB_SRC}
  ${LIB_HEADERS}
)

target_compile_features(${VTKSHARP_NATIVE_TARGET} PRIVATE cxx_std_17)

target_include_directories(${VTKSHARP_NATIVE_TARGET}
  PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/include
)

target_link_libraries(${VTKSHARP_NATIVE_TARGET}
  PRIVATE
    ${VTKSHARP_VTK_TARGETS}
)

if (WIN32)
  target_link_libraries(${VTKSHARP_NATIVE_TARGET}
    PRIVATE
      d3d9
      gdi32
      opengl32
      user32
  )
endif()

vtk_module_autoinit(
  TARGETS ${VTKSHARP_NATIVE_TARGET}
  MODULES
    ${VTKSHARP_VTK_TARGETS}
)
