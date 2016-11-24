using UnityEngine;
using UnityEngine.Networking;        //UnityWebRequest
using System.IO;                     //DirectoryInfo
using System.Collections;            //IEnumerator
using System.Collections.Generic;    //List
using System.Threading;              //Threading

public class DeviceGallery : MonoBehaviour 
{
    public GameObject[] m_imageSpheres;

    private int m_currPictureIndex = 0;
    private List<string> m_pictureFilePaths;
    private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
    private CoroutineQueue coroutineQueue;

    public void Start()
    {
        m_pictureFilePaths = new List<string>();
        coroutineQueue = new CoroutineQueue( this );
        coroutineQueue.StartLoop();
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
                coroutineQueue.EnqueueAction(LoadPicturesInternal(filePath, sphereIndex));
                coroutineQueue.EnqueueWait(2.0f);
            }
            else
            {
                m_imageSpheres[sphereIndex].GetComponent<SelectImage>().Hide();
            }
        }

        Resources.UnloadUnusedAssets();
    }

    private IEnumerator LoadPicturesInternal(string filePath, int sphereIndex)
    {        
        Debug.Log("------- VREEL: Loading from filePath: " + filePath);

        WWW www = new WWW("file://" + filePath);
        yield return www;

        // BLOCK: This calls through to the offending code
        m_imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(www, filePath);
    }

    private IEnumerator LoadPicturesInternal2(Texture2D source, string filePath, int sphereIndex)
    {
        int textureWidth = source.width;
        int textureHeight = source.height;

        Debug.Log("------- VREEL: Downloaded texture is being copied, Width x Height= " 
            + textureWidth + " x " + textureHeight + " ; Size in pixels = " 
            + textureWidth * textureHeight );

        Texture2D myTexture = new Texture2D(textureWidth, textureHeight, source.format, false);
        yield return myTexture;

        const int kNumIterationsPerFrame = 400000;
        int iterationCounter = 0;
        Color tempSourceColor = Color.black;

        Debug.Log("------- VREEL: Entering LoadPicturesInternal2 loop");
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {                
                //Debug.Log("------- VREEL: GetPixel(" + x + "," + y + ")");
                tempSourceColor = source.GetPixel(x, y);

                //Debug.Log("------- VREEL: SetPixel(" + x + "," + y + ")");
                myTexture.SetPixel(x, y, tempSourceColor);

                iterationCounter++;

                if (iterationCounter % kNumIterationsPerFrame == 0)
                {
                    Debug.Log("------- VREEL: Yielding LoadPicturesInternal2 at Iteration number: " 
                        + iterationCounter + " Pixel: (" + x + "," + y + ")");
                    yield return new WaitForEndOfFrame(); 
                }
            }
        }

        //Apply changes to the Texture
        myTexture.Apply();

        m_imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(myTexture, filePath);

        Resources.UnloadUnusedAssets();
    }
}