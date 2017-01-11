#include <jni.h>
#include <string>
#include <cstdio>
#include <android/log.h>
#include <GLES2/gl2.h>
#include "Unity/IUnityGraphics.h"
#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"

#define  LOG_TAG    "----------------- VREEL: libandroidcppnative"
#define  LOGI(...)  __android_log_print(ANDROID_LOG_INFO,LOG_TAG,__VA_ARGS__)

// DELETE ME...
//#define GLEW_NO_GLU
//#include "GLEW/glew.h"
//#include "GLFW/glfw3.h"
//#include <EGL/egl.h>
// DELETE ME...

// **************************
// Member Variables
// **************************

const int kMaxImageWidth = 10 * 1024;
const int kMaxImageHeight = 5 * 1024;
const int kMaxPixelsPerUpload = 1 * 1024 * 1024;

int m_numInits = 0; // Acts a bit like a reference counter, ensuring only 1 Init() and 1 Terminate() allowed

int m_imageWidth = 0;
int m_imageHeight = 0;

bool m_isLoadingIntoTexture = false;
GLint m_textureLoadingYOffset = 0;

void* m_pTextureHandle;
stbi_uc* m_pWorkingMemory = NULL;

// **************************
// Helper functions
// **************************

static void PrintGLString(const char *name, GLenum s)
{
    const char *v = (const char *) glGetString(s);
    LOGI("GL %s = %s\n", name, v);
}

static void PrintAllGlError()
{
    LOGI("Printing all GL Errors:\n");
    for (GLint error = glGetError(); error; error = glGetError())
    {
        LOGI("  glError (0x%x)\n", error);
    }
}

static void CheckGlError(const char* op)
{
    for (GLint error = glGetError(); error; error = glGetError())
    {
        LOGI("after %s() glError (0x%x)\n", op, error);
    }
}

// Image pixels coming from stb_image.h are upside-down and back-to-front, this function corrects that
void TransferAndCorrectAlignmentFromSrcToDest(int* pImage, int* pDest, int width, int height)
{
    int numPixels = width*height;
    for(int* pSrc = pImage + (numPixels-1); pSrc >= pImage; pSrc -= width)
    {
        for (int* pScanLine = pSrc - (width-1); pScanLine <= pSrc; ++pScanLine)
        {
            *pDest = *pScanLine;
            ++pDest;
        }
    }
}

// **************************
// Private functions - accessed through OnRenderEvent()
// **************************

// These are functions that use OpenGL and hence must be run from the Render Thread!
enum RenderFunctions
{
    kInit = 0,
    kCreateEmptyTexture = 1,
    kLoadScanlinesIntoTextureFromWorkingMemory = 2,
    kTerminate = 3
};

void Init()
{
    if (m_numInits == 0)
    {
        LOGI("Calling Init() in C++ Plugin!");

        GLuint textureId;
        glGenTextures(1, &textureId);
        m_pTextureHandle = (void*) textureId;
        PrintAllGlError();

        LOGI("Genned texture to Handle = %u \n", textureId);

        m_pWorkingMemory = new stbi_uc[kMaxImageWidth * kMaxImageHeight * sizeof(int32_t)];

        LOGI("Finished Init() in C++ Plugin!");
    }

    m_numInits++;
}

void Terminate()
{
    m_numInits--;

    if (m_numInits == 0)
    {
        LOGI("Calling Terminate() in C++ Plugin!");

        delete[] m_pWorkingMemory;

        LOGI("Finished Terminate() in C++ Plugin!");
    }
}

void CreateEmptyTexture()
{
    LOGI("Calling CreateEmptyTexture() in C++ Plugin");

    LOGI("glBindTexture(GL_TEXTURE_2D, textureId)");
    GLuint textureId = (GLuint)(size_t) m_pTextureHandle;
    glBindTexture(GL_TEXTURE_2D, textureId);
    PrintAllGlError();

    LOGI("glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, m_imageWidth, m_imageHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL");
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, m_imageWidth, m_imageHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, NULL); //(unsigned char*) m_pWorkingMemory);
    PrintAllGlError();

    m_isLoadingIntoTexture = true;
    m_textureLoadingYOffset = 0;

    LOGI("Finished CreateEmptyTexture() in C++ Plugin!");
}

// This function is called repeatedly like a for-loop with the variable m_textureLoadingYOffset updating every iteration
void LoadScanlinesIntoTextureFromWorkingMemory()
{
    LOGI("Calling LoadScanlinesIntoTextureFromWorkingMemory() in C++ Plugin");

    LOGI("glBindTexture(GL_TEXTURE_2D, textureId)");
    GLuint textureId = (GLuint)(size_t) m_pTextureHandle;
    glBindTexture(GL_TEXTURE_2D, textureId);
    PrintAllGlError();

    // Each iteration we upload up to kMaxPixelsPerUpload worth of width-long scanlines,
    //  up until the last one where we only upload the remaining scanlines
    const GLint kIdealNumberOfScanlinesToUpload = kMaxPixelsPerUpload/m_imageWidth;
    GLsizei height = (m_textureLoadingYOffset + kIdealNumberOfScanlinesToUpload < m_imageHeight)
                     ? kIdealNumberOfScanlinesToUpload
                     : (m_imageHeight - m_textureLoadingYOffset);

    LOGI("glTexSubImage2D(GL_TEXTURE_2D, 0, 0, %d, m_imageWidth, %d, GL_RGBA, GL_UNSIGNED_BYTE, pImage", m_textureLoadingYOffset, height);
    unsigned int* pImage = (unsigned int*) m_pWorkingMemory + (m_textureLoadingYOffset * m_imageWidth);
    glTexSubImage2D(GL_TEXTURE_2D, 0, 0, m_textureLoadingYOffset, m_imageWidth, height, GL_RGBA, GL_UNSIGNED_BYTE, pImage);
    PrintAllGlError();

    m_textureLoadingYOffset += kIdealNumberOfScanlinesToUpload;
    if (m_textureLoadingYOffset > m_imageHeight)
    {
        m_isLoadingIntoTexture = false;
    }

    LOGI("Finished LoadScanlinesIntoTextureFromWorkingMemory() in C++ Plugin! Loading in progress = %d", m_isLoadingIntoTexture);
}

static void UNITY_INTERFACE_API OnRenderEvent(int eventID)
{
    if (eventID == kInit)
    {
        Init();
    }
    else if (eventID == kCreateEmptyTexture)
    {
        CreateEmptyTexture();
    }
    else if (eventID == kLoadScanlinesIntoTextureFromWorkingMemory)
    {
        LoadScanlinesIntoTextureFromWorkingMemory();
    }
    else if (eventID == kTerminate)
    {
        Terminate();
    }
}

// **************************
// Public functions
// **************************

extern "C"
{

UnityRenderingEvent GetRenderEventFunc()
{
    return OnRenderEvent;
}

bool IsLoadingIntoTexture()
{
    return m_isLoadingIntoTexture;
}

void* GetStoredTexturePtr()
{
    return m_pTextureHandle;
}

int GetStoredImageWidth()
{
    return m_imageWidth;
}

int GetStoredImageHeight()
{
    return m_imageHeight;
}

bool LoadIntoWorkingMemoryFromImagePath(char* pFileName)
{
    LOGI("Calling LoadIntoWorkingMemoryFromImagePath() in C++ Plugin");

    int type = -1;
    m_imageWidth = 0, m_imageHeight = 0;

    stbi_uc* pImage = stbi_load(pFileName, &m_imageWidth, &m_imageHeight, &type, 4); // Forcing 4-components per pixel RGBA
    TransferAndCorrectAlignmentFromSrcToDest((int*) pImage, (int*) m_pWorkingMemory, m_imageWidth, m_imageHeight);
    stbi_image_free(pImage);

    LOGI("Image Loaded has Width = %d, Height = %d, Type = %d\n", m_imageWidth, m_imageHeight, type);

    LOGI("Finished LoadIntoWorkingMemoryFromImagePath() in C++ Plugin!");

    return (m_imageWidth*m_imageHeight) > 0;
}

bool LoadIntoWorkingMemoryFromImageData(void* pRawData, int dataLength)
{
    LOGI("Calling LoadIntoWorkingMemoryFromImageData() in C++ Plugin");

    int type = -1;
    m_imageWidth = 0, m_imageHeight = 0;

    stbi_uc* pImage = stbi_load_from_memory((stbi_uc*) pRawData, dataLength, &m_imageWidth, &m_imageHeight, &type, 4); // Forcing 4-components per pixel RGBA
    TransferAndCorrectAlignmentFromSrcToDest((int*) pImage, (int*) m_pWorkingMemory, m_imageWidth, m_imageHeight);
    stbi_image_free(pImage);

    LOGI("Image Loaded has Width = %d, Height = %d, Type = %d\n", m_imageWidth, m_imageHeight, type);

    LOGI("Finished LoadIntoWorkingMemoryFromImageData() in C++ Plugin!");

    return (m_imageWidth*m_imageHeight) > 0;
}

jstring Java_com_soul_cppplugin_MainActivity_stringFromJNI(JNIEnv *env, jobject /* this */)
{
    std::string hello = "Hello from C++!";
    return env->NewStringUTF(hello.c_str());
}

}