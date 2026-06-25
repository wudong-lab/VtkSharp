cmake_minimum_required(VERSION 3.20)
project(dump_colors CXX)

set(VTK_DIR "C:/Program Files/VTK/lib/cmake/vtk-9.5" CACHE PATH "VTK cmake dir")
find_package(VTK REQUIRED COMPONENTS CommonColor)

add_executable(dump_colors dump_colors.cpp)
target_link_libraries(dump_colors PRIVATE ${VTK_LIBRARIES})
vtk_module_autoinit(TARGETS dump_colors MODULES ${VTK_LIBRARIES})
set_target_properties(dump_colors PROPERTIES CXX_STANDARD 17)
