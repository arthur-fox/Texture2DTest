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
        Debug.Log("------- VREEL: A CppPlugin was created by = " + m_owner.name);

        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kInit);

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
        
    ~CppPlugin()
    {
        Debug.Log("------- VREEL: A CppPlugin was destructed by = " + m_owner.name);

        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kTerminate);
    }

    public IEnumerator LoadImageFromPath(ThreadJob threadJob, GameObject[] imageSpheres, int sphereIndex, string filePath)
    {
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Calling LoadPicturesInternal() from filePath: " + filePath);
        StringBuilder filePathForCpp = new StringBuilder(filePath);

        Debug.Log("------- VREEL: Calling LoadIntoWorkingMemoryFromImagePath(), on background thread!");
        yield return threadJob.WaitFor();
        bool ranJobSuccessfully = false;
        threadJob.Start( () => 
            ranJobSuccessfully = LoadIntoWorkingMemoryFromImagePath(filePathForCpp)
        );
        yield return threadJob.WaitFor();
        Debug.Log("------- VREEL: Finished LoadIntoWorkingMemoryFromImagePath(), ran Job Successully = " + ranJobSuccessfully); 


        Debug.Log("------- VREEL: Calling CreateEmptyTexture()");
        yield return new WaitForEndOfFrame();
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kCreateEmptyTexture);
        yield return new WaitForSeconds(0.1f); // These waits need to be longer to ensure that GL.IssuePluginEvent() has gone through!
        Debug.Log("------- VREEL: Finished CreateEmptyTexture(), Texture Handle = " + GetCurrStoredTexturePtr() );


        Debug.Log("------- VREEL: Calling LoadScanlinesIntoTextureFromWorkingMemory()");
        while (IsLoadingIntoTexture())
        {            
            GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kLoadScanlinesIntoTextureFromWorkingMemory);
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("------- VREEL: Finished LoadScanlinesIntoTextureFromWorkingMemory()");


        Debug.Log("------- VREEL: Calling CreateExternalTexture(), size of Texture is Width x Height = " + GetCurrStoredImageWidth() + " x " + GetCurrStoredImageHeight());
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
        Debug.Log("------- VREEL: Finished CreateExternalTexture()!");


        Debug.Log("------- VREEL: Calling SetImageAndFilePath()");
        imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(newTexture, filePath);
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Finished SetImageAndFilePath()");

        Resources.UnloadUnusedAssets();
    }   

    public IEnumerator LoadImageFromStream(ThreadJob threadJob, Stream imageStream, GameObject[] imageSpheres, int sphereIndex, string filePath)
    {        
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: ConvertStreamAndSetImage for " + filePath);

        Debug.Log("------- VREEL: Calling ToByteArray(), on background thread!");
        yield return threadJob.WaitFor();
        bool ranJobSuccessfully = false;
        byte[] myBinary = null;
        threadJob.Start( () => 
            ranJobSuccessfully = ToByteArray(imageStream, ref myBinary)
        );
        yield return threadJob.WaitFor();
        Debug.Log("------- VREEL: Finished ToByteArray(), ran Job Successully = " + ranJobSuccessfully);

        Debug.Log("------- VREEL: Calling LoadIntoWorkingMemoryFromImagePath(), on background thread!");
        GCHandle rawDataHandle = GCHandle.Alloc(myBinary, GCHandleType.Pinned);
        IntPtr rawDataPtr = rawDataHandle.AddrOfPinnedObject();
        yield return threadJob.WaitFor();
        ranJobSuccessfully = false;
        threadJob.Start( () => 
            ranJobSuccessfully = LoadIntoWorkingMemoryFromImageData(rawDataPtr, myBinary.Length)
        );
        yield return threadJob.WaitFor();
        rawDataHandle.Free();
        Debug.Log("------- VREEL: Finished LoadIntoWorkingMemoryFromImagePath(), ran Job Successully = " + ranJobSuccessfully); 


        Debug.Log("------- VREEL: Calling CreateEmptyTexture()");
        yield return new WaitForEndOfFrame();
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kCreateEmptyTexture);
        yield return new WaitForSeconds(0.1f); // These waits need to be longer to ensure that GL.IssuePluginEvent() has gone through!
        Debug.Log("------- VREEL: Finished CreateEmptyTexture(), Texture Handle = " + GetCurrStoredTexturePtr() );


        Debug.Log("------- VREEL: Calling LoadScanlinesIntoTextureFromWorkingMemory()");
        while (IsLoadingIntoTexture())
        {            
            GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kLoadScanlinesIntoTextureFromWorkingMemory);
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("------- VREEL: Finished LoadScanlinesIntoTextureFromWorkingMemory()");


        Debug.Log("------- VREEL: Calling CreateExternalTexture(), size of Texture is Width x Height = " + GetCurrStoredImageWidth() + " x " + GetCurrStoredImageHeight());
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
        Debug.Log("------- VREEL: Finished CreateExternalTexture()!");


        Debug.Log("------- VREEL: Calling SetImageAndFilePath()");
        imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(newTexture, filePath);
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Finished SetImageAndFilePath()");

        Resources.UnloadUnusedAssets();
    }        

    // **************************
    // Private/Helper functions
    // **************************

    private bool ToByteArray(Stream stream, ref byte[] outBinary)
    {                
        const int kBlockSize = 1024;
        byte[] buf = new byte[kBlockSize];
        using( MemoryStream ms = new MemoryStream() ) 
        {            
            int byteCount = 0;
            do
            {
                byteCount = stream.Read(buf, 0, kBlockSize);
                ms.Write(buf, 0, byteCount);
            }
            while(stream.CanRead && byteCount > 0);

            outBinary = ms.ToArray();
        }

        return true;
    }
}