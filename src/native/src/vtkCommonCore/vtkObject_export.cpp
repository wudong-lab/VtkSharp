#include "vtksharp_api.h"
#include <cstdint>
#include <vtkCallbackCommand.h>
#include <vtkObject.h>

VTKSHARP_API void vtkObject_Modified(vtkObject* self) { self->Modified(); }

using VtkSharpObserverCallback = void (*)(vtkObject* caller, uint32_t eventId, void* clientData);

struct VtkSharpObserverContext
{
    VtkSharpObserverCallback Callback;
    void* ClientData;
};

VTKSHARP_API uint64_t vtkObject_AddObserverCallback(
    vtkObject* self,
    uint32_t eventId,
    VtkSharpObserverCallback callback,
    void* clientData,
    float priority)
{
    auto context = new VtkSharpObserverContext{ callback, clientData };
    auto command = vtkCallbackCommand::New();
    command->SetClientData(context);
    command->SetClientDataDeleteCallback([](void* data) {
        delete static_cast<VtkSharpObserverContext*>(data);
    });
    command->SetCallback([](vtkObject* caller, unsigned long eid, void* callbackClientData, void* callData) {
        (void)callData;
        auto context = static_cast<VtkSharpObserverContext*>(callbackClientData);
        context->Callback(caller, static_cast<uint32_t>(eid), context->ClientData);
    });

    auto tag = self->AddObserver(static_cast<unsigned long>(eventId), command, priority);
    command->Delete();
    return static_cast<uint64_t>(tag);
}

VTKSHARP_API void vtkObject_RemoveObserver(vtkObject* self, uint64_t tag)
{
    self->RemoveObserver(static_cast<unsigned long>(tag));
}
