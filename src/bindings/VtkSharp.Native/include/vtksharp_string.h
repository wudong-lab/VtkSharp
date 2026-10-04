#pragma once

#include "vtksharp_api.h"

#include <cstddef>
#include <cstdlib>
#include <cstring>

struct VtkSharpUtf8String
{
    char* Data;
    std::size_t Length;
};

inline void VtkSharpUtf8String_CopyFrom(VtkSharpUtf8String* value, const char* data, std::size_t length) noexcept
{
    value->Data = nullptr;
    value->Length = 0;

    if (data == nullptr || length == 0)
        return;

    auto* buffer = static_cast<char*>(std::malloc(length));
    if (buffer == nullptr)
        return;

    std::memcpy(buffer, data, length);
    value->Data = buffer;
    value->Length = length;
}

VTKSHARP_API void VtkSharpUtf8String_Free(VtkSharpUtf8String* value) noexcept;
