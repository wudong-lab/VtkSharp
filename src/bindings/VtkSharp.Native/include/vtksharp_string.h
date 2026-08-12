#pragma once

#include "vtksharp_api.h"

#include <cstddef>

struct VtkSharpUtf8String
{
    char* Data;
    std::size_t Length;
};

void VtkSharpUtf8String_CopyFrom(VtkSharpUtf8String* value, const char* data, std::size_t length) noexcept;
VTKSHARP_API void VtkSharpUtf8String_Free(VtkSharpUtf8String* value) noexcept;
