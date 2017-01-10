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
    private static extern IntPtr GetStoredTexturePtr();

    [DllImport ("androidcppnative")]
    private static extern int GetStoredImageWidth();

    [DllImport ("androidcppnative")]
    private static extern int GetStoredImageHeight();

    [DllImport ("androidcppnative")]
    private static extern bool CalcAndSetDimensionsFromImagePath(StringBuilder filePath);

    [DllImport ("androidcppnative")]
    private static extern bool CalcAndSetDimensionsFromImageData(IntPtr rawDataPtr, int length);   

    [DllImport ("androidcppnative")]
    private static extern bool LoadIntoPixelsFromImagePath(StringBuilder filePath, IntPtr resultPtr);

    [DllImport ("androidcppnative")]
    private static extern bool LoadIntoPixelsFromImageData(IntPtr rawDataPtr, IntPtr resultPtr, int length);

    //WIP
    [DllImport ("androidcppnative")]
    private static extern bool LoadIntoWorkingMemoryFromImagePath(StringBuilder filePath);

    [DllImport ("androidcppnative")]
    private static extern IntPtr GetRenderEventFunc();
    //WIP

    // **************************
    // Member Variables
    // **************************

    private MonoBehaviour m_owner = null;

    // These are functions that use OpenGL and hence must be run from the Render Thread!
    enum RenderFunctions
    {
        kInit = 0,
        kLoadIntoTextureFromWorkingMemory = 1
    };

    // **************************
    // Public functions
    // **************************

    public CppPlugin(MonoBehaviour owner)
    {
        m_owner = owner;
        Debug.Log("------- VREEL: A CppPlugin was created by = " + m_owner.name);

        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kInit);

        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }
           

    // FUNCTION ORDER:
    // - INIT() - Gen textures + allocate memory
    // - LoadIntoWorkingMemoryFromImagePath(filePathForCpp) - image pixels are now in memory
    // - LoadIntoTextureFromWorkingMemory()

    // CURRENT PROBLEMS:
    // - The texture being created with CreateExternalTexture() is being copied (so we run into the same old synchronous problem)
    // - Currently I'm not doing any chuncking, i'm simply calling glTexImage2D() on the Render Thread [I'm not sure how big a problem this is]

    // WIP
    public IEnumerator LoadImageFromPath(ThreadJob threadJob, GameObject[] imageSpheres, int sphereIndex, string filePath)
    {
        Debug.Log("------- VREEL: Calling LoadPicturesInternal() from filePath: " + filePath);
        StringBuilder filePathForCpp = new StringBuilder(filePath);

        Debug.Log("------- VREEL: Calling LoadIntoWorkingMemoryFromImagePath(), on background thread!");
        bool ranJobSuccessfully = false;
        threadJob.Start( () => 
            ranJobSuccessfully = LoadIntoWorkingMemoryFromImagePath(filePathForCpp)
        );
        yield return threadJob.WaitFor();
        Debug.Log("------- VREEL: Finished LoadIntoWorkingMemoryFromImagePath(), ran Job Successully = " + ranJobSuccessfully); 

        //yield return new WaitForSeconds(10); // This is here to see if there's a frameout caused from above function...

        Debug.Log("------- VREEL: Calling kLoadIntoTextureFromWorkingMemory(), on background thread!");
        yield return new WaitForEndOfFrame();
        GL.IssuePluginEvent(GetRenderEventFunc(), (int)RenderFunctions.kLoadIntoTextureFromWorkingMemory);
        /*
        threadJob.Start( () =>             
            textureHandle = LoadIntoTextureFromImagePath(filePathForCpp)
        );
        yield return threadJob.WaitFor();
        */
        Debug.Log("------- VREEL: Finished kLoadIntoTextureFromWorkingMemory(), Texture Handle = " + GetStoredTexturePtr() );

        //yield return new WaitForSeconds(10); // This is here to see if there's a frameout caused from above function...

        Debug.Log("------- VREEL: Calling CreateExternalTexture(), size of Texture is Width x Height = " + GetStoredImageWidth() + " x " + GetStoredImageHeight());
        yield return new WaitForEndOfFrame();
        Texture2D testTexture =
            Texture2D.CreateExternalTexture(
                GetStoredImageWidth(), 
                GetStoredImageHeight(), 
                TextureFormat.RGBA32,           // Default textures have a format of ARGB32
                false,
                false,
                GetStoredTexturePtr()
            );
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Finished CreateExternalTexture()!");

        Debug.Log("------- VREEL: BLOCKING OPERATION START - Calling SetImageAndFilePath()");
        imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(testTexture, filePath);
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: BLOCKING OPERATION END - Finished SetImageAndFilePath()");

        Resources.UnloadUnusedAssets();
    }
    // WIP




    public IEnumerator LoadImageFromPath_Original(ThreadJob threadJob, GameObject[] imageSpheres, int sphereIndex, string filePath)
    {
        Debug.Log("------- VREEL: Calling LoadPicturesInternal() from filePath: " + filePath);

        Debug.Log("------- VREEL: Calling CalcAndSetDimensionsFromImagePath(), on background thread!");
        StringBuilder filePathForCpp = new StringBuilder(filePath);
        bool ranJobSuccessfully = false;
        threadJob.Start( () => 
            ranJobSuccessfully = CalcAndSetDimensionsFromImagePath(filePathForCpp)
        );
        yield return threadJob.WaitFor();
        Debug.Log("------- VREEL: Finished CalcAndSetDimensionsFromImagePath(), ran Job Successully = " + ranJobSuccessfully); 


        Debug.Log("------- VREEL: BLOCKING OPERATION START - Creating Texture unfortunately blocks, size of Texture is Width x Height = " + GetStoredImageWidth() + " x " + GetStoredImageHeight());
        yield return new WaitForEndOfFrame();
        Texture2D myNewTexture2D = new Texture2D(GetStoredImageWidth(), GetStoredImageHeight());
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: BLOCKING OPERATION END - Created Creation!");

        Debug.Log("------- VREEL: Calling LoadIntoPixelsFromImagePath(), on background thread!");
        Color32[] pixels = myNewTexture2D.GetPixels32(0);
        GCHandle pixelsDataHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        IntPtr pixelsPtr = pixelsDataHandle.AddrOfPinnedObject();
        yield return new WaitForEndOfFrame();

        ranJobSuccessfully = false;
        threadJob.Start( () => 
            ranJobSuccessfully = LoadIntoPixelsFromImagePath(filePathForCpp, pixelsPtr)
        );
        yield return threadJob.WaitFor();
        Debug.Log("------- VREEL: Finished LoadIntoPixelsFromImagePath(), ran Job Successully = " + ranJobSuccessfully);


        Debug.Log("------- VREEL: Calling SetPixels32() and Apply()");
        myNewTexture2D.SetPixels32(pixels);
        yield return new WaitForEndOfFrame();
        myNewTexture2D.Apply();
        yield return new WaitForEndOfFrame();
        pixelsDataHandle.Free();
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Finished SetPixels32() and Apply() on myNewTexture2D!");


        Debug.Log("------- VREEL: BLOCKING OPERATION START - Calling SetImageAndFilePath()");
        imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(myNewTexture2D, filePath);
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: BLOCKING OPERATION END - Finished SetImageAndFilePath()");

        Resources.UnloadUnusedAssets();
    }

    public IEnumerator LoadImageFromStream(ThreadJob threadJob, Stream imageStream, GameObject[] imageSpheres, int sphereIndex, string filePath)
    {        
        Debug.Log("------- VREEL: ConvertStreamAndSetImage for " + filePath);

        Debug.Log("------- VREEL: Calling ToByteArray(), on background thread!");
        bool ranJobSuccessfully = false;
        byte[] myBinary = null;
        threadJob.Start( () => 
            ranJobSuccessfully = ToByteArray(imageStream, ref myBinary)
        );
        yield return threadJob.WaitFor(); //yield return StartCoroutine(m_threadJob.WaitFor());
        Debug.Log("------- VREEL: Finished ToByteArray(), ran Job Successully = " + ranJobSuccessfully); 


        Debug.Log("------- VREEL: Calling CalcAndSetDimensionsFromImageData(), on background thread!");
        GCHandle rawDataHandle = GCHandle.Alloc(myBinary, GCHandleType.Pinned);
        IntPtr rawDataPtr = rawDataHandle.AddrOfPinnedObject();

        ranJobSuccessfully = false;
        threadJob.Start( () => 
            ranJobSuccessfully = CalcAndSetDimensionsFromImageData(rawDataPtr, myBinary.Length)
        );
        yield return threadJob.WaitFor();
        Debug.Log("------- VREEL: Finished CalcAndSetDimensionsFromImageData(), ran Job Successully = " + ranJobSuccessfully); 


        Debug.Log("------- VREEL: BLOCKING OPERATION START - Creating Texture unfortunately blocks, size of Texture is Width x Height = " + GetStoredImageWidth() + " x " + GetStoredImageHeight());
        yield return new WaitForEndOfFrame();
        Texture2D myNewTexture2D = new Texture2D(GetStoredImageWidth(), GetStoredImageHeight());
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: BLOCKING OPERATION END - Created Creation!");


        Debug.Log("------- VREEL: Calling LoadDataIntoPixels(), on background thread!");
        Color32[] pixels = myNewTexture2D.GetPixels32(0);
        GCHandle pixelsDataHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        IntPtr pixelsPtr = pixelsDataHandle.AddrOfPinnedObject();
        yield return new WaitForEndOfFrame();

        ranJobSuccessfully = false;
        threadJob.Start( () => 
            ranJobSuccessfully = LoadIntoPixelsFromImageData(rawDataPtr, pixelsPtr, myBinary.Length) 
        );
        yield return threadJob.WaitFor();
        Debug.Log("------- VREEL: Finished LoadDataIntoPixels(), ran Job Successully = " + ranJobSuccessfully);


        Debug.Log("------- VREEL: Calling SetPixels32() and Apply()");
        myNewTexture2D.SetPixels32(pixels);
        yield return new WaitForEndOfFrame();
        myNewTexture2D.Apply();
        yield return new WaitForEndOfFrame();
        pixelsDataHandle.Free();
        rawDataHandle.Free();
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Finished SetPixels32() and Apply() on myNewTexture2D!");


        Debug.Log("------- VREEL: Calling SetImageAndFilePath()");
        imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(myNewTexture2D, filePath);
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