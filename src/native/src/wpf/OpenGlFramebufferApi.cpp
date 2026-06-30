#include "OpenGlFramebufferApi.h"

#ifndef GL_FRAMEBUFFER
#define GL_FRAMEBUFFER 0x8D40
#endif

#ifndef GL_COLOR_ATTACHMENT0
#define GL_COLOR_ATTACHMENT0 0x8CE0
#endif

#ifndef GL_FRAMEBUFFER_COMPLETE
#define GL_FRAMEBUFFER_COMPLETE 0x8CD5
#endif

namespace
{
template <typename T>
T LoadOpenGLProc(const char* name)
{
    return reinterpret_cast<T>(::wglGetProcAddress(name));
}
}

bool OpenGlFramebufferApi::Load()
{
    this->m_glGenFramebuffers = LoadOpenGLProc<GlGenFramebuffersProc>("glGenFramebuffers");
    this->m_glBindFramebuffer = LoadOpenGLProc<GlBindFramebufferProc>("glBindFramebuffer");
    this->m_glFramebufferTexture2D = LoadOpenGLProc<GlFramebufferTexture2DProc>("glFramebufferTexture2D");
    this->m_glCheckFramebufferStatus = LoadOpenGLProc<GlCheckFramebufferStatusProc>("glCheckFramebufferStatus");
    this->m_glDeleteFramebuffers = LoadOpenGLProc<GlDeleteFramebuffersProc>("glDeleteFramebuffers");

    return this->m_glGenFramebuffers &&
        this->m_glBindFramebuffer &&
        this->m_glFramebufferTexture2D &&
        this->m_glCheckFramebufferStatus &&
        this->m_glDeleteFramebuffers;
}

void OpenGlFramebufferApi::CreateRenderTarget(GLuint* texture, GLuint* framebuffer) const
{
    ::glGenTextures(1, texture);
    ::glBindTexture(GL_TEXTURE_2D, *texture);
    ::glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    ::glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    ::glBindTexture(GL_TEXTURE_2D, 0);

    this->m_glGenFramebuffers(1, framebuffer);
}

bool OpenGlFramebufferApi::RenderToTexture(
    GLuint framebuffer,
    GLuint texture,
    int width,
    int height,
    void (*renderCallback)(void*),
    void* userData) const
{
    this->m_glBindFramebuffer(GL_FRAMEBUFFER, framebuffer);
    this->m_glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, texture, 0);

    const bool isComplete = this->m_glCheckFramebufferStatus(GL_FRAMEBUFFER) == GL_FRAMEBUFFER_COMPLETE;
    if (isComplete)
    {
        ::glViewport(0, 0, width, height);
        renderCallback(userData);
        ::glFinish();
    }

    this->m_glBindFramebuffer(GL_FRAMEBUFFER, 0);
    return isComplete;
}

void OpenGlFramebufferApi::DeleteRenderTarget(GLuint* texture, GLuint* framebuffer) const
{
    if (*framebuffer)
    {
        this->m_glDeleteFramebuffers(1, framebuffer);
        *framebuffer = 0;
    }

    if (*texture)
    {
        ::glDeleteTextures(1, texture);
        *texture = 0;
    }
}
