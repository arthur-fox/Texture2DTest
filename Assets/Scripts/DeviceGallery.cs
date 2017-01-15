using UnityEngine;
using UnityEngine.Networking;           //UnityWebRequest
using System;                           //IntPtr
using System.IO;                        //DirectoryInfo
using System.Collections;               //IEnumerator
using System.Collections.Generic;       //List

public class DeviceGallery : MonoBehaviour 
{
    public GameObject[] m_imageDisplays;

    private int m_currPictureIndex = 0;
    private List<string> m_pictureFilePaths;
    private CoroutineQueue m_coroutineQueue;
    private ThreadJob m_threadJob;
    private CppPlugin m_cppPlugin;

    public void Start()
    {        
        m_pictureFilePaths = new List<string>();

        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();

        m_threadJob = new ThreadJob(this);
        m_cppPlugin = new CppPlugin(this);
    }

    public bool IsIndexAtStart()
    {
        return m_currPictureIndex == 0;
    }

    public bool IsIndexAtEnd()
    {
        int numImageDisplays = m_imageDisplays.GetLength(0);
        int numFiles = m_pictureFilePaths.Count;
        return m_currPictureIndex >= (numFiles - numImageDisplays);       
    }

    public void OpenAndroidGallery()
    {
        m_coroutineQueue.EnqueueAction(OpenAndroidGalleryInternal());
    }        

    private IEnumerator OpenAndroidGalleryInternal()
    {
        Debug.Log("------- Texture2DTest: OpenAndroidGallery() called");

        m_currPictureIndex = 0;
        m_pictureFilePaths.Clear();
        string path = "/storage/emulated/0/DCIM/"; //HARDCODED: Hardcoded path, 
        //this may need to be changed depending on device used

        Debug.Log("------- Texture2DTest: Storing all FilePaths from directory: " + path);

        bool foundJob = false;
        m_threadJob.Start( () => 
            foundJob = StoreAllFilePaths(path)
        );
        yield return m_threadJob.WaitFor();

        int numImageDisplays = m_imageDisplays.GetLength(0);
        LoadPictures(m_currPictureIndex, numImageDisplays);
    }

    private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
    private bool StoreAllFilePaths(string path)
    {
        foreach (string filePath in System.IO.Directory.GetFiles(path, "*", SearchOption.AllDirectories))
        { 
            if (ImageExtensions.Contains(Path.GetExtension(filePath).ToUpperInvariant())) // Check that the file is indeed an image
            {                
                m_pictureFilePaths.Add(filePath); // We only need a single image in m_pictureFilePaths
                break;
            }
        }

        m_pictureFilePaths.Reverse(); // Reversing to have the pictures appear in the order of newest first

        return m_pictureFilePaths.Count > 0;
    }

    private void LoadPictures(int startingPictureIndex, int numImages)
    {
        Debug.Log(string.Format("------- Texture2DTest: Loading {0} pictures beginning at index {1}. There are {2} pictures in the gallery!", 
            numImages, startingPictureIndex, m_pictureFilePaths.Count));

        Resources.UnloadUnusedAssets();

        int currPictureIndex = startingPictureIndex;
        for (int imageIndex = 0; imageIndex < numImages; imageIndex++, currPictureIndex++)
        {
            if (currPictureIndex < m_pictureFilePaths.Count)
            {                   
                string filePath = m_pictureFilePaths[currPictureIndex];
                m_coroutineQueue.EnqueueAction(LoadImageInternalPluginCpp(filePath, imageIndex));
                m_coroutineQueue.EnqueueWait(2.0f);
            }
            else
            {
                m_imageDisplays[imageIndex].GetComponent<SelectImage>().Hide();
            }
        }

        Resources.UnloadUnusedAssets();
    }

    private IEnumerator LoadImageInternalPluginCpp(string filePath, int imageIndex)
    {   
        Debug.Log("------- Texture2DTest: Called LoadImageInternalPluginCpp()");
        yield return m_cppPlugin.LoadImageFromPath(m_threadJob, m_imageDisplays, imageIndex, filePath);
    }

    private IEnumerator LoadImageInternalUnity(string filePath, int imageIndex)
    {
        Debug.Log("------- Texture2DTest: Calling LoadPicturesInternalUnity() from filePath: " + filePath);
        
        WWW www = new WWW("file://" + filePath);
        yield return www;

        Debug.Log("------- Texture2DTest: Calling LoadImageIntoTexture()");
        Texture2D myNewTexture2D = new Texture2D(2,2);
        www.LoadImageIntoTexture(myNewTexture2D);
        yield return new WaitForEndOfFrame();
        Debug.Log("------- Texture2DTest: Finished LoadImageIntoTexture()");

        Debug.Log("------- Texture2DTest: Calling SetImageAndFilePath()");
        m_imageDisplays[imageIndex].GetComponent<SelectImage>().SetImageAndFilePath(myNewTexture2D, filePath);
        yield return new WaitForEndOfFrame();
        Debug.Log("------- Texture2DTest: Finished SetImageAndFilePath()");

        Resources.UnloadUnusedAssets();
    }
}