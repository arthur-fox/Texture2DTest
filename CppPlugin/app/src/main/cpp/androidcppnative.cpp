#include <jni.h>
#include <string>
#include <cstdio>
#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"

extern "C"
{

int imageWidth = 0;
int imageHeight = 0;

bool CalcAndSetDimensionsFromImageData(void* pRawData, int dataLength)
{
    stbi_uc* pAddress = (stbi_uc*) pRawData;
    int type;

    stbi_info_from_memory(pAddress, dataLength, &imageWidth, &imageHeight, &type);

    return (imageWidth*imageHeight) > 0;
}

int GetImageWidth()
{
    return imageWidth;
}

int GetImageHeight()
{
    return imageHeight;
}

bool LoadImageDataIntoPixels(void* pRawData, void* pPixelData, int dataLength)
{
    stbi_uc* pDataAddress = (stbi_uc*) pRawData;
    stbi_uc* pReturnAddress = (stbi_uc*) pPixelData;
    int width = -1, height = -1, type = -1;

    stbi_uc* image = stbi_load_from_memory(pDataAddress, dataLength, &width, &height, &type, 4);
    //memcpy(pReturnAddress, image, width*height*4);

    int pixels = width*height;
    int* dest = (int*)pReturnAddress;
    for(int* src = ((int*)image) + (pixels-1); src >= ((int*)image); --src)
    {
        *dest = *src;
        ++dest;
    }
    stbi_image_free(image);

    return (width*height) > 0;
}

jstring Java_com_soul_cppplugin_MainActivity_stringFromJNI(JNIEnv *env, jobject /* this */)
{
    std::string hello = "Hello from C++!";
    return env->NewStringUTF(hello.c_str());
}

}