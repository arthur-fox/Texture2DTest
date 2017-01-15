using UnityEngine;
using System;                         // IntPtr
using System.Text;                    // StringBuilder
using System.IO;                      // Stream
using System.Collections;             // IEnumerator
using System.Runtime.InteropServices; // DllImport

public class CppPlugin
{
    // **************************
    // C++ Plugin declerations
    // **************************

    [DllImport ("androidcppnative")]
    private static extern IntPtr GetRenderEventFunc();

    [DllImport ("androidcppnative")]
    private static extern IntPtr GetCurrStoredTexturePtr();

    [DllImport ("androidcppnative")]
    private static extern int GetCurrStoredImageWidth();

    [DllImport ("androidcppnative")]
    private static extern int GetCurrStoredImageHeight();

    [DllImport ("androidcppnative")]
    private static extern bool IsLoadingIntoTexture();

    [DllImport ("androidcppnative")]
    private static extern bool LoadIntoWorkingMemoryFromImagePath(StringBuilder filePath);

    [DllImport ("androidcppnative")]
    private static extern bool LoadIntoWorkingMemoryFromImageData(IntPtr pRawData, int dataLength);

    // **************************
    // Member Variables
    // **************************

    private const float kFrameWaitTime = 2/60.0f; // wait 2 frames, to ensure GL call has gone through
    private MonoBehaviour m_owner = null;

    // These are functions that use OpenGL and hence must be run from the Render Thread!
    enum RenderFunctions
    {
        kInit = 0,
        kCreateEmptyTexture = 1,
        kLoadScanlinesIntoTextureFromWorkingMemory = 2,
        kTerminate = 3
    };

    // **************************
    // Public functions
    // **************************

    // FUNCTION ORDER:
    // (1) Init() calls glGenTextures() and allocates memory;
    // (2) LoadIntoWorkingMemoryFromImagePath() calls through to stbi_load() and sets pixels into m_pWorkingMemory
    // (3) CreateEmptyTexture() calls glTexImage2D() hence allocating the actual texture
    // (4) LoadScanlinesIntoTextureFromWorkingMemory() is called repeatedly until all scanlines are uploaded to the texture through glTexSubImage2D
    // (5) Finally CreateExternalTexture() is called with the texture that’s been created beneath us! 

    public CppPlugin(MonoBehaviour owner)
    {
        m_owner = owner;
        Debug.Log("------- Texture2DTest: A CppPlugin was created by = " + m_owner.name);

        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kInit);

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
        
    ~CppPlugin()
    {
        Debug.Log("------- Texture2DTest: A CppPlugin was destructed by = " + m_owner.name);

        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kTerminate);
    }

    public IEnumerator LoadImageFromPath(ThreadJob threadJob, GameObject[] imageDisplays, int imageIndex, string filePath)
    {
        yield return new WaitForEndOfFrame();
        Debug.Log("------- Texture2DTest: Calling LoadPicturesInternal() from filePath: " + filePath);
        StringBuilder filePathForCpp = new StringBuilder(filePath);

        Debug.Log("------- Texture2DTest: Calling LoadIntoWorkingMemoryFromImagePath(), on background thread!");
        yield return threadJob.WaitFor();
        bool ranJobSuccessfully = false;
        threadJob.Start( () => 
            ranJobSuccessfully = LoadIntoWorkingMemoryFromImagePath(filePathForCpp)
        );
        yield return threadJob.WaitFor();
        Debug.Log("------- Texture2DTest: Finished LoadIntoWorkingMemoryFromImagePath(), ran Job Successully = " + ranJobSuccessfully); 


        Debug.Log("------- Texture2DTest: Calling CreateEmptyTexture()");
        yield return new WaitForEndOfFrame();
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kCreateEmptyTexture);
        yield return new WaitForSeconds(kFrameWaitTime); // These waits need to be longer to ensure that GL.IssuePluginEvent() has gone through!
        Debug.Log("------- Texture2DTest: Finished CreateEmptyTexture(), Texture Handle = " + GetCurrStoredTexturePtr() );


        Debug.Log("------- Texture2DTest: Calling LoadScanlinesIntoTextureFromWorkingMemory()");
        while (IsLoadingIntoTexture())
        {            
            GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kLoadScanlinesIntoTextureFromWorkingMemory);
            yield return new WaitForSeconds(kFrameWaitTime);
        }
        Debug.Log("------- Texture2DTest: Finished LoadScanlinesIntoTextureFromWorkingMemory()");


        Debug.Log("------- Texture2DTest: Calling CreateExternalTexture(), size of Texture is Width x Height = " + GetCurrStoredImageWidth() + " x " + GetCurrStoredImageHeight());
        yield return new WaitForEndOfFrame();
        Texture2D newTexture =
            Texture2D.CreateExternalTexture(
                GetCurrStoredImageWidth(), 
                GetCurrStoredImageHeight(), 
                TextureFormat.RGBA32,           // Default textures have a format of ARGB32
                false,
                false,
                GetCurrStoredTexturePtr()
            );
        yield return new WaitForEndOfFrame();
        Debug.Log("------- Texture2DTest: Finished CreateExternalTexture()!");


        Debug.Log("------- Texture2DTest: Calling SetImageAndFilePath()");
        imageDisplays[imageIndex].GetComponent<SelectImage>().SetImageAndFilePath(newTexture, filePath);
        yield return new WaitForEndOfFrame();
        Debug.Log("------- Texture2DTest: Finished SetImageAndFilePath()");

        Resources.UnloadUnusedAssets();
    }
}