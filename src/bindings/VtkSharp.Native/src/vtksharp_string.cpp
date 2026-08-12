#include "vtksharp_string.h"

#include <cstdlib>
#include <cstring>

void VtkSharpUtf8String_CopyFrom(VtkSharpUtf8String* value, const char* data, std::size_t length) noexcept
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

VTKSHARP_API void VtkSharpUtf8String_Free(VtkSharpUtf8String* value) noexcept
{
    if (value == nullptr)
        return;

    std::free(value->Data);
    value->Data = nullptr;
    value->Length = 0;
}
