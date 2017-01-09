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
    private static extern int Init();

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
    private static extern IntPtr LoadIntoTextureFromImagePath(StringBuilder filePath);

    [DllImport ("androidcppnative")]
    private static extern void SetTextureVars(IntPtr textureHandle, int width, int height);
    //WIP

    // **************************
    // Member Variables
    // **************************

    private MonoBehaviour m_owner = null;

    // WIP
    private Texture2D m_testTexture;
    private const int kMaxTextureWidth = 8 * 1024; // 7200
    private const int kMaxTextureHeight = 4 * 1024; // 3600
    // WIP

    // **************************
    // Public functions
    // **************************

    public CppPlugin(MonoBehaviour owner)
    {
        m_owner = owner;
        Debug.Log("------- VREEL: A CppPlugin was created by = " + m_owner.name);

        Init();

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // WIP
        /*
        // Allocate enough memory through Marshal.AllocHGlobal() to cater for the largest Texture size
        int sizeOfUnmanagedMemoryBlock = kMaxTextureWidth * kMaxTextureHeight * Marshal.SizeOf(typeof(Color32));
        m_pUnmanagedTextureMemory = Marshal.AllocHGlobal(sizeOfUnmanagedMemoryBlock + kMaxTextureWidth);
        */

        m_testTexture = new Texture2D(kMaxTextureWidth, kMaxTextureHeight);
        SetTextureVars(m_testTexture.GetNativeTexturePtr(), kMaxTextureWidth, kMaxTextureHeight);
        // WIP
    }
           

    // WIP
    public IEnumerator LoadImageFromPath(ThreadJob threadJob, GameObject[] imageSpheres, int sphereIndex, string filePath)
    {
        Debug.Log("------- VREEL: Calling LoadPicturesInternal() from filePath: " + filePath);
        StringBuilder filePathForCpp = new StringBuilder(filePath);

        Debug.Log("------- VREEL: Calling CalcAndSetDimensionsFromImagePath(), on background thread!");
        bool ranJobSuccessfully = false;
        threadJob.Start( () => 
            ranJobSuccessfully = CalcAndSetDimensionsFromImagePath(filePathForCpp)
        );
        yield return threadJob.WaitFor();
        Debug.Log("------- VREEL: Finished CalcAndSetDimensionsFromImagePath(), ran Job Successully = " + ranJobSuccessfully); 

        Debug.Log("------- VREEL: Calling LoadIntoPixelsFromImagePath(), on background thread!");
        yield return new WaitForEndOfFrame();
        IntPtr textureHandle = IntPtr.Zero;
        threadJob.Start( () => 
            textureHandle = LoadIntoTextureFromImagePath(filePathForCpp)
        );
        yield return threadJob.WaitFor();
        Debug.Log("------- VREEL: Finished LoadIntoPixelsFromImagePath(), Texture Handle = " + textureHandle);

        Debug.Log("------- VREEL: Calling CreateExternalTexture(), size of Texture is Width x Height = " + GetStoredImageWidth() + " x " + GetStoredImageHeight());
        yield return new WaitForEndOfFrame();
        Texture2D testTexture =
            Texture2D.CreateExternalTexture(
                GetStoredImageWidth(), 
                GetStoredImageHeight(), 
                TextureFormat.RGBA32,           // This param seems wrong - Default textures have a format of ARGB32
                false,
                false,
                textureHandle                   // m_testTexture.GetNativeTexturePtr()
            );
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Finished CreateExternalTexture(),!");

        /*
        Debug.Log("------- VREEL: Calling Apply()");
        testTexture.Apply();
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Finished Apply() on myNewTexture2D!");
        */

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