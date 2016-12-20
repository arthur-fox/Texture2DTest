#include <jni.h>
#include <string>
#include <cstdio>
#define STB_IMAGE_IMPLEMENTATION
#include "stb_image.h"

extern "C"
{

int imageWidth = 0;
int imageHeight = 0;

void CalcImageDimensions(void* pRawData, int dataLength)
{
    stbi_uc* pAddress = (stbi_uc*) pRawData;
    int type;

    stbi_info_from_memory(pAddress, dataLength, &imageWidth, &imageHeight, &type);
}

int GetImageWidth()
{
    return imageWidth;
}

int GetImageHeight()
{
    return imageHeight;
}

int PluginThreadFunc(void* pRawData, void* pReturnData, int dataLength)
{
    stbi_uc* pDataAddress = (stbi_uc*) pRawData;
    stbi_uc* pReturnAddress = (stbi_uc*) pReturnData;
    int width = -1, height = -1, type = -1;

    // These Printf's aren't appearing in LogCat unfortunately =/
    //fprintf(stdout, "VREEL PLUGIN 1 - STDOUT");
    //fprintf(stderr, "VREEL PLUGIN 2 - STDERR");
    //freopen("VReelDebug.txt", "a", stdout);
    //printf("SVREEL PLUGIN 3 - DEBUG\n");
    //fprintf(stdout, "VREEL PLUGIN 4 - STDOUT");

    // Type returns 3 channels. I've tried setting the final component to 0, 3, 4 but it doesn't fix the problem...
    pReturnAddress = stbi_load_from_memory(pDataAddress, dataLength, &width, &height, &type, 0);
    return type; //(width*height);
}

std::string MyPluginString()
{
    int i = 11;
    return "11"; //std::to_string(i);
}

jstring Java_com_soul_cppplugin_MainActivity_stringFromJNI(JNIEnv *env, jobject /* this */)
{
    std::string hello = "Hello from C++" + MyPluginString() + "!";
    return env->NewStringUTF(hello.c_str());
}

}