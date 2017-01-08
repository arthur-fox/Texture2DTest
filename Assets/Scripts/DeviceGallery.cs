using UnityEngine;
using UnityEngine.Networking;           //UnityWebRequest
using System.IO;                        //DirectoryInfo
using System.Collections;               //IEnumerator
using System.Collections.Generic;       //List

public class DeviceGallery : MonoBehaviour 
{
    public GameObject[] m_imageSpheres;

    private int m_currPictureIndex = 0;
    private List<string> m_pictureFilePaths;
    private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
    private CoroutineQueue m_coroutineQueue;
    private ThreadJob m_threadJob;
    private CppPlugin m_cppPlugin;

    private AndroidJavaClass m_javaPlugin;

    public void Start()
    {        
        m_pictureFilePaths = new List<string>();

        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();

        m_threadJob = new ThreadJob(this);
        m_cppPlugin = new CppPlugin(this);

        AndroidJNI.AttachCurrentThread();
        m_javaPlugin = new AndroidJavaClass("com.DefaultCompany.Texture2DTest.JavaPlugin");

        string imagesTopLevelDirectory = m_javaPlugin.CallStatic<string>("GetAndroidImagesPath");
        Debug.Log("------- VREEL: TEST - " + imagesTopLevelDirectory);
    }

    public bool IsIndexAtStart()
    {
        return m_currPictureIndex == 0;
    }

    public bool IsIndexAtEnd()
    {
        int numImageSpheres = m_imageSpheres.GetLength(0);
        int numFiles = m_pictureFilePaths.Count;
        return m_currPictureIndex >= (numFiles - numImageSpheres);       
    }

    public void OpenAndroidGallery()
    {
        Debug.Log("------- VREEL: OpenAndroidGallery() called");

        m_currPictureIndex = 0;
        m_pictureFilePaths.Clear();
        string path = "/storage/emulated/0/DCIM/Gear 360/"; //HARDCODED: Hardcoded path to 360 images, 
                                                            //this may need to be changed depending on device used

        Debug.Log("------- VREEL: Storing all FilePaths from directory: " + path);

        StoreAllFilePaths(path);

        int numImageSpheres = m_imageSpheres.GetLength(0);
        LoadPictures(m_currPictureIndex, numImageSpheres);
    }        

    private void StoreAllFilePaths(string path)
    {
        foreach (string filePath in System.IO.Directory.GetFiles(path))
        { 
            if (ImageExtensions.Contains(Path.GetExtension(filePath).ToUpperInvariant())) // Check that the file is indeed an image
            {                
                m_pictureFilePaths.Add(filePath);
            }
        }

        m_pictureFilePaths.Reverse(); // Reversing to have the pictures appear in the order of newest first
    }

    private void LoadPictures(int startingPictureIndex, int numImages)
    {
        Debug.Log(string.Format("------- VREEL: Loading {0} pictures beginning at index {1}. There are {2} pictures in the gallery!", 
            numImages, startingPictureIndex, m_pictureFilePaths.Count));

        Resources.UnloadUnusedAssets();

        int currPictureIndex = startingPictureIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, currPictureIndex++)
        {
            if (currPictureIndex < m_pictureFilePaths.Count)
            {                   
                string filePath = m_pictureFilePaths[currPictureIndex];
                m_coroutineQueue.EnqueueAction(LoadImageInternalPlugin(filePath, sphereIndex));
                m_coroutineQueue.EnqueueWait(2.0f);
            }
            else
            {
                m_imageSpheres[sphereIndex].GetComponent<SelectImage>().Hide();
            }
        }

        Resources.UnloadUnusedAssets();
    }

    private IEnumerator LoadImageInternalPlugin(string filePath, int sphereIndex)
    {   
        yield return m_cppPlugin.LoadImageFromPath(m_threadJob, m_imageSpheres, sphereIndex, filePath);
    }

    private IEnumerator LoadImageInternalUnity(string filePath, int sphereIndex)
    {
        Debug.Log("------- VREEL: Calling LoadPicturesInternalUnity() from filePath: " + filePath);
        
        WWW www = new WWW("file://" + filePath);
        yield return www;

        Debug.Log("------- VREEL: Calling LoadImageIntoTexture()");
        Texture2D myNewTexture2D = new Texture2D(2,2);
        www.LoadImageIntoTexture(myNewTexture2D);
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Finished LoadImageIntoTexture()");

        Debug.Log("------- VREEL: Calling SetImageAndFilePath()");
        m_imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(myNewTexture2D, filePath);
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Finished SetImageAndFilePath()");

        Resources.UnloadUnusedAssets();
    }
}