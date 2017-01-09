#include <jni.h>
#include <string>
#include <cstdio>
#include <android/log.h>
#include <GLES2/gl2.h>
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

bool m_initialised = false;
int m_imageWidth = 0;
int m_imageHeight = 0;

void* m_textureHandle;

// **************************
// Helper functions
// **************************

void TransferPixelsFromSrcToDest(int* pImage, int* pDest, int width, int height)
{
    //memcpy(pDest, pImage, numPixels);

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

// **************************
// Public functions
// **************************

extern "C"
{

void Init()
{
    if (!m_initialised)
    {
        //TODO: Any neccessary initialisation
        m_initialised = true;
    }
}

int GetStoredImageWidth()
{
    return m_imageWidth;
}

int GetStoredImageHeight()
{
    return m_imageHeight;
}

bool CalcAndSetDimensionsFromImagePath(char* pFileName)
{
    m_imageWidth = m_imageHeight = 0;
    int type;

    stbi_info(pFileName, &m_imageWidth, &m_imageHeight, &type);

    return (m_imageWidth*m_imageHeight) > 0;
}

bool CalcAndSetDimensionsFromImageData(void* pRawData, int dataLength)
{
    m_imageWidth = m_imageHeight = 0;
    stbi_uc* pAddress = (stbi_uc*) pRawData;
    int type;

    stbi_info_from_memory(pAddress, dataLength, &m_imageWidth, &m_imageHeight, &type);

    return (m_imageWidth*m_imageHeight) > 0;
}

bool LoadIntoPixelsFromImagePath(char* pFileName, void* pPixelData)
{
    int width = -1, height = -1, type = -1;

    stbi_uc* pImage = stbi_load(pFileName, &width, &height, &type, 4);
    TransferPixelsFromSrcToDest((int*) pImage, (int*) pPixelData, width, height);
    stbi_image_free(pImage);

    return (width*height) > 0;
}

bool LoadIntoPixelsFromImageData(void* pRawData, void* pPixelData, int dataLength)
{
    stbi_uc* pDataAddress = (stbi_uc*) pRawData;
    int width = -1, height = -1, type = -1;

    stbi_uc* pImage = stbi_load_from_memory(pDataAddress, dataLength, &width, &height, &type, 4);
    TransferPixelsFromSrcToDest((int*) pImage, (int*) pPixelData, width, height);
    stbi_image_free(pImage);

    return (width*height) > 0;
}

void* LoadIntoTextureFromImagePath(char* pFileName)
{
    LOGI("Calling LoadIntoPixelsFromImagePath() in C++!");

    int width = -1, height = -1, type = -1;

    //GLuint textureId;
    //glGenTextures(1, &textureId);

    //glActiveTexture(GL_TEXTURE0);
    //glBindTexture(GL_TEXTURE_2D, textureId);

    GLuint gltex = (GLuint)(size_t)(m_textureHandle);
    glBindTexture(GL_TEXTURE_2D, gltex);

    LOGI("Bound texture to Handle = %u \n", gltex);

    //glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR_MIPMAP_LINEAR);
    //glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);

    stbi_uc* pImage = stbi_load(pFileName, &width, &height, &type, 4);

    glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA,
                 GL_UNSIGNED_BYTE, (unsigned char*) pImage);

    stbi_image_free(pImage);

    LOGI("Image had Width = %d, Height = %d, Type = %d\n", width, height, type);

    glBindTexture(GL_TEXTURE_2D, 0);

    PrintAllGlError();
    LOGI("Finished LoadIntoPixelsFromImagePath() in C++!");

    return m_textureHandle;
}

void SetTextureVars(void* textureHandle, int width, int height)
{
    m_textureHandle = textureHandle;
    //m_imageWidth = width;
    //m_imageHeight = height;
}

jstring Java_com_soul_cppplugin_MainActivity_stringFromJNI(JNIEnv *env, jobject /* this */)
{
    std::string hello = "Hello from C++!";
    return env->NewStringUTF(hello.c_str());
}

}