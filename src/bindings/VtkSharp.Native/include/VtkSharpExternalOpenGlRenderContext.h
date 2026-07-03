#pragma once

#if defined(_WIN32)
#if defined(VTKSHARP_EXTERNAL_OPENGL_RENDER_CONTEXT_IMPORTS)
#define VTKSHARP_EXTERNAL_OPENGL_RENDER_CONTEXT_API extern "C" __declspec(dllimport)
#else
#define VTKSHARP_EXTERNAL_OPENGL_RENDER_CONTEXT_API extern "C" __declspec(dllexport)
#endif
#else
#define VTKSHARP_EXTERNAL_OPENGL_RENDER_CONTEXT_API extern "C" __attribute__((visibility("default")))
#endif

struct VtkSharpExternalOpenGlRenderContext;

using VtkSharpOpenGlLoadSymbolProc = void* (*)(void* userData, const char* name);
using VtkSharpOpenGlMakeCurrentProc = int (*)(void* userData);
using VtkSharpOpenGlIsCurrentProc = int (*)(void* userData);
using VtkSharpOpenGlFrameProc = void (*)(void* userData);

VTKSHARP_EXTERNAL_OPENGL_RENDER_CONTEXT_API VtkSharpExternalOpenGlRenderContext*
VtkSharpExternalOpenGlRenderContext_New(
    void* userData,
    VtkSharpOpenGlLoadSymbolProc loadSymbol,
    VtkSharpOpenGlMakeCurrentProc makeCurrent,
    VtkSharpOpenGlIsCurrentProc isCurrent,
    VtkSharpOpenGlFrameProc frame);

VTKSHARP_EXTERNAL_OPENGL_RENDER_CONTEXT_API void VtkSharpExternalOpenGlRenderContext_Delete(
    VtkSharpExternalOpenGlRenderContext* context);

VTKSHARP_EXTERNAL_OPENGL_RENDER_CONTEXT_API void* VtkSharpExternalOpenGlRenderContext_GetRenderWindow(
    VtkSharpExternalOpenGlRenderContext* context);

VTKSHARP_EXTERNAL_OPENGL_RENDER_CONTEXT_API void* VtkSharpExternalOpenGlRenderContext_GetRenderer(
    VtkSharpExternalOpenGlRenderContext* context);

VTKSHARP_EXTERNAL_OPENGL_RENDER_CONTEXT_API bool VtkSharpExternalOpenGlRenderContext_InitializeFromCurrentContext(
    VtkSharpExternalOpenGlRenderContext* context);

VTKSHARP_EXTERNAL_OPENGL_RENDER_CONTEXT_API void VtkSharpExternalOpenGlRenderContext_SetSize(
    VtkSharpExternalOpenGlRenderContext* context,
    int width,
    int height);

VTKSHARP_EXTERNAL_OPENGL_RENDER_CONTEXT_API bool VtkSharpExternalOpenGlRenderContext_Render(
    VtkSharpExternalOpenGlRenderContext* context);

VTKSHARP_EXTERNAL_OPENGL_RENDER_CONTEXT_API const char* VtkSharpExternalOpenGlRenderContext_GetLastError(
    VtkSharpExternalOpenGlRenderContext* context);
