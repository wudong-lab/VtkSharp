#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>

#include <gl/GL.h>

class OpenGlFramebuffer
{
public:
    bool Load();

    bool Create();
    GLuint GetTexture() const;
    bool RenderToTexture(int width, int height, void (*renderCallback)(void*), void* userData) const;
    void Release();

private:
    using GlGenFramebuffersProc = void(APIENTRY*)(GLsizei, GLuint*);
    using GlBindFramebufferProc = void(APIENTRY*)(GLenum, GLuint);
    using GlFramebufferTexture2DProc = void(APIENTRY*)(GLenum, GLenum, GLenum, GLuint, GLint);
    using GlCheckFramebufferStatusProc = GLenum(APIENTRY*)(GLenum);
    using GlDeleteFramebuffersProc = void(APIENTRY*)(GLsizei, const GLuint*);

    GLuint m_texture = 0;
    GLuint m_framebuffer = 0;

    GlGenFramebuffersProc glGenFramebuffers = nullptr;
    GlBindFramebufferProc glBindFramebuffer = nullptr;
    GlFramebufferTexture2DProc glFramebufferTexture2D = nullptr;
    GlCheckFramebufferStatusProc glCheckFramebufferStatus = nullptr;
    GlDeleteFramebuffersProc glDeleteFramebuffers = nullptr;
};
