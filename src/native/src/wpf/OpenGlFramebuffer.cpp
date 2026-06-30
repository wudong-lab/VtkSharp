#include "OpenGlFramebuffer.h"

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

bool OpenGlFramebuffer::Load()
{
    this->glGenFramebuffers = LoadOpenGLProc<GlGenFramebuffersProc>("glGenFramebuffers");
    this->glBindFramebuffer = LoadOpenGLProc<GlBindFramebufferProc>("glBindFramebuffer");
    this->glFramebufferTexture2D = LoadOpenGLProc<GlFramebufferTexture2DProc>("glFramebufferTexture2D");
    this->glCheckFramebufferStatus = LoadOpenGLProc<GlCheckFramebufferStatusProc>("glCheckFramebufferStatus");
    this->glDeleteFramebuffers = LoadOpenGLProc<GlDeleteFramebuffersProc>("glDeleteFramebuffers");

    return this->glGenFramebuffers &&
        this->glBindFramebuffer &&
        this->glFramebufferTexture2D &&
        this->glCheckFramebufferStatus &&
        this->glDeleteFramebuffers;
}

void OpenGlFramebuffer::Create()
{
    this->Release();

    ::glGenTextures(1, &this->m_texture);
    ::glBindTexture(GL_TEXTURE_2D, this->m_texture);
    ::glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
    ::glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
    ::glBindTexture(GL_TEXTURE_2D, 0);

    this->glGenFramebuffers(1, &this->m_framebuffer);
}

GLuint OpenGlFramebuffer::GetTexture() const
{
    return this->m_texture;
}

bool OpenGlFramebuffer::RenderToTexture(
    int width,
    int height,
    void (*renderCallback)(void*),
    void* userData) const
{
    this->glBindFramebuffer(GL_FRAMEBUFFER, this->m_framebuffer);
    this->glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, this->m_texture, 0);

    const bool isComplete = this->glCheckFramebufferStatus(GL_FRAMEBUFFER) == GL_FRAMEBUFFER_COMPLETE;
    if (isComplete)
    {
        ::glViewport(0, 0, width, height);
        renderCallback(userData);
        ::glFinish();
    }

    this->glBindFramebuffer(GL_FRAMEBUFFER, 0);
    return isComplete;
}

void OpenGlFramebuffer::Release()
{
    if (this->m_framebuffer)
    {
        this->glDeleteFramebuffers(1, &this->m_framebuffer);
        this->m_framebuffer = 0;
    }

    if (this->m_texture)
    {
        ::glDeleteTextures(1, &this->m_texture);
        this->m_texture = 0;
    }
}
