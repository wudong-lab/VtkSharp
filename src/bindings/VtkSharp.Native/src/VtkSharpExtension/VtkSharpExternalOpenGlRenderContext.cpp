#include "VtkSharpExternalOpenGlRenderContext.h"

#include <vtkCallbackCommand.h>
#include <vtkCommand.h>
#include <vtkGenericOpenGLRenderWindow.h>
#include <vtkOpenGLRenderWindow.h>
#include <vtkRenderer.h>
#include <vtkSmartPointer.h>

#include <algorithm>
#include <cstring>

using VtkCallbackProc = void (*)(vtkObject*, unsigned long, void*, void*);

struct VtkSharpExternalOpenGlRenderContext
{
    void* UserData = nullptr;
    VtkSharpOpenGlLoadSymbolProc LoadSymbol = nullptr;
    VtkSharpOpenGlMakeCurrentProc MakeCurrent = nullptr;
    VtkSharpOpenGlIsCurrentProc IsCurrent = nullptr;
    VtkSharpOpenGlFrameProc Frame = nullptr;

    vtkSmartPointer<vtkGenericOpenGLRenderWindow> RenderWindow;
    vtkSmartPointer<vtkRenderer> Renderer;
    vtkSmartPointer<vtkCallbackCommand> MakeCurrentCallback;
    vtkSmartPointer<vtkCallbackCommand> IsCurrentCallback;
    vtkSmartPointer<vtkCallbackCommand> SupportsOpenGlCallback;
    vtkSmartPointer<vtkCallbackCommand> IsDirectCallback;
    vtkSmartPointer<vtkCallbackCommand> FrameCallback;

    char LastError[256] = {};

    bool Initialize();
    vtkSmartPointer<vtkCallbackCommand> CreateCallback(VtkCallbackProc callback);
    void SetError(const char* message);
};

namespace
{
    void OnMakeCurrent(vtkObject*, unsigned long, void* clientData, void*)
    {
        auto* context = static_cast<VtkSharpExternalOpenGlRenderContext*>(clientData);
        if (context && context->MakeCurrent)
        {
            context->MakeCurrent(context->UserData);
        }
    }

    void OnIsCurrent(vtkObject*, unsigned long, void* clientData, void* callData)
    {
        auto* context = static_cast<VtkSharpExternalOpenGlRenderContext*>(clientData);
        auto* isCurrent = static_cast<bool*>(callData);
        if (context && context->IsCurrent && isCurrent)
        {
            *isCurrent = context->IsCurrent(context->UserData) != 0;
        }
    }

    void OnSupportsOpenGl(vtkObject*, unsigned long, void*, void* callData)
    {
        auto* supportsOpenGl = static_cast<int*>(callData);
        if (supportsOpenGl)
        {
            *supportsOpenGl = 1;
        }
    }

    void OnIsDirect(vtkObject*, unsigned long, void*, void* callData)
    {
        auto* isDirect = static_cast<int*>(callData);
        if (isDirect)
        {
            *isDirect = 1;
        }
    }

    void OnFrame(vtkObject*, unsigned long, void* clientData, void*)
    {
        auto* context = static_cast<VtkSharpExternalOpenGlRenderContext*>(clientData);
        if (context && context->Frame)
        {
            context->Frame(context->UserData);
        }
    }
}

bool VtkSharpExternalOpenGlRenderContext::Initialize()
{
    if (!this->LoadSymbol || !this->MakeCurrent || !this->IsCurrent)
    {
        this->SetError("External OpenGL callbacks were not provided.");
        return false;
    }

    this->RenderWindow = vtkSmartPointer<vtkGenericOpenGLRenderWindow>::New();
    this->Renderer = vtkSmartPointer<vtkRenderer>::New();

    this->MakeCurrentCallback = this->CreateCallback(&OnMakeCurrent);
    this->RenderWindow->AddObserver(vtkCommand::WindowMakeCurrentEvent, this->MakeCurrentCallback);

    this->IsCurrentCallback = this->CreateCallback(&OnIsCurrent);
    this->RenderWindow->AddObserver(vtkCommand::WindowIsCurrentEvent, this->IsCurrentCallback);

    this->SupportsOpenGlCallback = this->CreateCallback(&OnSupportsOpenGl);
    this->RenderWindow->AddObserver(vtkCommand::WindowSupportsOpenGLEvent, this->SupportsOpenGlCallback);

    this->IsDirectCallback = this->CreateCallback(&OnIsDirect);
    this->RenderWindow->AddObserver(vtkCommand::WindowIsDirectEvent, this->IsDirectCallback);

    this->FrameCallback = this->CreateCallback(&OnFrame);
    this->RenderWindow->AddObserver(vtkCommand::WindowFrameEvent, this->FrameCallback);

    this->RenderWindow->SetOpenGLSymbolLoader(
        [](void* userData, const char* name) -> vtkOpenGLRenderWindow::VTKOpenGLAPIProc
        {
            auto* context = static_cast<VtkSharpExternalOpenGlRenderContext*>(userData);
            return reinterpret_cast<vtkOpenGLRenderWindow::VTKOpenGLAPIProc>(
                context->LoadSymbol(context->UserData, name));
        },
        this);

    this->RenderWindow->AddRenderer(this->Renderer);
    this->RenderWindow->SetFrameBlitModeToBlitToCurrent();
    this->RenderWindow->FramebufferFlipYOff();
    return true;
}

vtkSmartPointer<vtkCallbackCommand> VtkSharpExternalOpenGlRenderContext::CreateCallback(VtkCallbackProc callback)
{
    auto command = vtkSmartPointer<vtkCallbackCommand>::New();
    command->SetClientData(this);
    command->SetCallback(callback);
    return command;
}

void VtkSharpExternalOpenGlRenderContext::SetError(const char* message)
{
    std::strncpy(this->LastError, message, sizeof(this->LastError) - 1);
    this->LastError[sizeof(this->LastError) - 1] = '\0';
}

VtkSharpExternalOpenGlRenderContext* VtkSharpExternalOpenGlRenderContext_New(
    void* userData,
    VtkSharpOpenGlLoadSymbolProc loadSymbol,
    VtkSharpOpenGlMakeCurrentProc makeCurrent,
    VtkSharpOpenGlIsCurrentProc isCurrent,
    VtkSharpOpenGlFrameProc frame)
{
    auto* context = new VtkSharpExternalOpenGlRenderContext();
    context->UserData = userData;
    context->LoadSymbol = loadSymbol;
    context->MakeCurrent = makeCurrent;
    context->IsCurrent = isCurrent;
    context->Frame = frame;

    if (!context->Initialize())
    {
        delete context;
        return nullptr;
    }

    return context;
}

void VtkSharpExternalOpenGlRenderContext_Delete(VtkSharpExternalOpenGlRenderContext* context)
{
    delete context;
}

void* VtkSharpExternalOpenGlRenderContext_GetRenderWindow(VtkSharpExternalOpenGlRenderContext* context)
{
    return context && context->RenderWindow ? context->RenderWindow.GetPointer() : nullptr;
}

void* VtkSharpExternalOpenGlRenderContext_GetRenderer(VtkSharpExternalOpenGlRenderContext* context)
{
    return context && context->Renderer ? context->Renderer.GetPointer() : nullptr;
}

bool VtkSharpExternalOpenGlRenderContext_InitializeFromCurrentContext(VtkSharpExternalOpenGlRenderContext* context)
{
    if (!context || !context->RenderWindow)
    {
        return false;
    }

    const bool initialized = context->RenderWindow->InitializeFromCurrentContext();
    if (!initialized)
    {
        context->SetError("Failed to initialize VTK from the current OpenGL context.");
    }

    return initialized;
}

void VtkSharpExternalOpenGlRenderContext_SetSize(
    VtkSharpExternalOpenGlRenderContext* context,
    int width,
    int height)
{
    if (!context || !context->RenderWindow)
    {
        return;
    }

    context->RenderWindow->SetSize(std::max(1, width), std::max(1, height));
}

bool VtkSharpExternalOpenGlRenderContext_Render(VtkSharpExternalOpenGlRenderContext* context)
{
    if (!context || !context->RenderWindow)
    {
        return false;
    }

    context->RenderWindow->Render();
    return true;
}

const char* VtkSharpExternalOpenGlRenderContext_GetLastError(VtkSharpExternalOpenGlRenderContext* context)
{
    return context ? context->LastError : "External OpenGL render context is null.";
}
