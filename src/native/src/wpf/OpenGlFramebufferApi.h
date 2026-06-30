#pragma once

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#include <windows.h>

#include <gl/GL.h>

class OpenGlFramebufferApi
{
public:
    bool Load();

    void CreateFramebuffer(GLuint* framebuffer) const;
    bool RenderToTexture(GLuint framebuffer, GLuint texture, int width, int height, void (*renderCallback)(void*), void* userData) const;
    void DeleteFramebuffer(GLuint* framebuffer) const;

private:
    using GlGenFramebuffersProc = void(APIENTRY*)(GLsizei, GLuint*);
    using GlBindFramebufferProc = void(APIENTRY*)(GLenum, GLuint);
    using GlFramebufferTexture2DProc = void(APIENTRY*)(GLenum, GLenum, GLenum, GLuint, GLint);
    using GlCheckFramebufferStatusProc = GLenum(APIENTRY*)(GLenum);
    using GlDeleteFramebuffersProc = void(APIENTRY*)(GLsizei, const GLuint*);

    GlGenFramebuffersProc m_glGenFramebuffers = nullptr;
    GlBindFramebufferProc m_glBindFramebuffer = nullptr;
    GlFramebufferTexture2DProc m_glFramebufferTexture2D = nullptr;
    GlCheckFramebufferStatusProc m_glCheckFramebufferStatus = nullptr;
    GlDeleteFramebuffersProc m_glDeleteFramebuffers = nullptr;
};
