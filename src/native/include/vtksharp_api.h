#pragma once

#if defined(_WIN32)
#define VTKSHARP_API extern "C" __declspec(dllexport)
#else
#define VTKSHARP_API extern "C" __attribute__((visibility("default")))
#endif
