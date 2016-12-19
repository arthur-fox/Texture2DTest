#include <jni.h>
#include <string>

extern "C"
{

int MyPluginInt()
{
    int i = 11;
    return i;
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