#include "vtksharp_string.h"

#include <cstdlib>

VTKSHARP_API void VtkSharpUtf8String_Free(VtkSharpUtf8String* value) noexcept
{
    if (value == nullptr)
        return;

    std::free(value->Data);
    value->Data = nullptr;
    value->Length = 0;
}
