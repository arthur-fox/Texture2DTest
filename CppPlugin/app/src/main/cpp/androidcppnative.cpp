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

int m_numInits = 0; // Acts a bit like a reference counter, ensuring only 1 Init() and 1 Terminate() allowed

int m_imageWidth = 0;
int m_imageHeight = 0;

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
    kLoadIntoTextureFromWorkingMemory = 1,
    kTerminate = 2
};

// DELETE ME
//glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
//glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

//stbi_info(pFileName, &m_imageWidth, &m_imageHeight, &type);
//stbi_info_from_memory(pAddress, dataLength, &m_imageWidth, &m_imageHeight, &type);
// DELETE ME

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

void LoadIntoTextureFromWorkingMemory()
{
    LOGI("Calling LoadIntoTextureFromWorkingMemory() in C++ Plugin");

    LOGI("glBindTexture(GL_TEXTURE_2D, textureId)");
    GLuint textureId = (GLuint)(size_t) m_pTextureHandle;
    glBindTexture(GL_TEXTURE_2D, textureId);
    PrintAllGlError();

    //glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, width, height, GL_RGBA, GL_UNSIGNED_BYTE, (unsigned char*) pPixelData);
    LOGI("glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, m_imageWidth, m_imageHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, (unsigned char*) m_pWorkingMemory)");
    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, m_imageWidth, m_imageHeight, 0, GL_RGBA, GL_UNSIGNED_BYTE, (unsigned char*) m_pWorkingMemory);
    PrintAllGlError();

    LOGI("glBindTexture(GL_TEXTURE_2D, 0)");
    glBindTexture(GL_TEXTURE_2D, 0);
    PrintAllGlError();

    LOGI("Finished LoadIntoPixelsFromImagePath() in C++ Plugin!");
}

static void UNITY_INTERFACE_API OnRenderEvent(int eventID)
{
    if (eventID == kInit)
    {
        Init();
    }
    else if (eventID == kLoadIntoTextureFromWorkingMemory)
    {
        LoadIntoTextureFromWorkingMemory();
    }
}

// **************************
// Public functions
// **************************

extern "C"
{

UnityRenderingEvent GetRenderEventFunc() //UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
{
    return OnRenderEvent;
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