using UnityEngine;
using System;                         // IntPtr
using System.Runtime.InteropServices; // DllImport
using System.Collections;             // IEnumerator
using System.Collections.Generic;     // List
using System.IO;                      // Stream
using Amazon;                         // UnityInitializer
using Amazon.CognitoIdentity;         // CognitoAWSCredentials
using Amazon.S3;                      // AmazonS3Client
using Amazon.S3.Model;                // ListBucketsRequest

//using System.Linq;  // Take

public class AWSS3Client : MonoBehaviour 
{   
    public string m_s3BucketName = null;
    public GameObject[] m_imageSpheres; 

    private AmazonS3Client m_s3Client = null;
    private CognitoAWSCredentials m_credentials = null;

    private int m_currS3ImageIndex = 0;
    private List<string> m_s3ImageFilePaths;
    private CoroutineQueue m_coroutineQueue;
    private ThreadJob m_threadJob; 

    // androidCppNative - C++ Plugin declerations
    [DllImport ("androidcppnative")]
    private static extern bool CalcAndSetDimensionsFromImageData(IntPtr rawDataPtr, int length);

    [DllImport ("androidcppnative")]
    private static extern int GetImageWidth();

    [DllImport ("androidcppnative")]
    private static extern int GetImageHeight();

    [DllImport ("androidcppnative")]
    private static extern bool LoadImageDataIntoPixels(IntPtr rawDataPtr, IntPtr resultPtr, int length);

    void Start() 
	{
		UnityInitializer.AttachToGameObject(this.gameObject);

        m_credentials = new CognitoAWSCredentials (
            "eu-west-1:1f9f6bd1-3cfe-43c2-afbc-3e06d8d1fe27", // Identity Pool ID
            RegionEndpoint.EUWest1 // Region
        );
        m_s3Client = new AmazonS3Client (m_credentials, RegionEndpoint.EUWest1);

        m_s3ImageFilePaths = new List<string>();

        m_coroutineQueue = new CoroutineQueue( this );
        m_coroutineQueue.StartLoop();

        m_threadJob = new ThreadJob( this );
	}

    public bool IsIndexAtStart()
    {
        return m_currS3ImageIndex == 0;
    }

    public bool IsIndexAtEnd()
    {
        int numImageSpheres = m_imageSpheres.GetLength(0);
        int numFiles = m_s3ImageFilePaths.Count;
        return m_currS3ImageIndex >= (numFiles - numImageSpheres);       
    }

    private static readonly List<string> ImageExtensions = new List<string> { ".JPG", ".JPE", ".BMP", ".GIF", ".PNG" };
    public void DownloadAllImages()
    {           
        Debug.Log("------- VREEL: Fetching all the Objects from" + m_s3BucketName);

        var request = new ListObjectsRequest()
        {
            BucketName = m_s3BucketName
        };

        m_s3Client.ListObjectsAsync(request, (responseObject) =>
        {
            if (responseObject.Exception == null)
            {
                Debug.Log("------- VREEL: Got Response, printing now!");

                responseObject.Response.S3Objects.ForEach((s3object) =>
                {
                    if (ImageExtensions.Contains(Path.GetExtension(s3object.Key).ToUpperInvariant())) // Check that the file is indeed an image
                    {   
                        m_s3ImageFilePaths.Add(s3object.Key);
                        Debug.Log("------- VREEL: Fetched " + s3object.Key);
                    }
                });                                  

                DownloadAllImagesInternal();
            }
            else
            {
                Debug.Log("------- VREEL: Got an Exception calling 'ListObjectsAsync()'");
            }
        });
    }       
        
    private void DownloadAllImagesInternal()
    {
        int numImageSpheres = m_imageSpheres.GetLength(0);
        DownloadImagesAndSetSpheres(m_currS3ImageIndex, numImageSpheres);
    }

    private void DownloadImagesAndSetSpheres(int startingPictureIndex, int numImages)
    {
        Debug.Log(string.Format("------- VREEL: Downloading {0} pictures beginning at index {1}. There are {2} pictures in the S3 bucket!", 
            numImages, startingPictureIndex, m_s3ImageFilePaths.Count));

        Resources.UnloadUnusedAssets();

        int currPictureIndex = startingPictureIndex;
        for (int sphereIndex = 0; sphereIndex < numImages; sphereIndex++, currPictureIndex++)
        {
            if (currPictureIndex < m_s3ImageFilePaths.Count)
            {   
                Debug.Log("------- VREEL: Loop iteration: " + sphereIndex);
                string filePath = m_s3ImageFilePaths[currPictureIndex];
                DownloadImage(filePath, sphereIndex, currPictureIndex, numImages);
            }
            else
            {
                m_imageSpheres[sphereIndex].GetComponent<SelectImage>().Hide();
            }
        }

        Resources.UnloadUnusedAssets();
    }

    private void DownloadImage(string filePath, int sphereIndex, int pictureIndex, int numImages)
    {
        string fullFilePath = m_s3BucketName + filePath;
        string logString01 = string.Format("------- VREEL: Fetching {0} from bucket {1}", filePath, m_s3BucketName);       
        Debug.Log(logString01);

        m_s3Client.GetObjectAsync(m_s3BucketName, filePath, (s3ResponseObj) =>
        {               
            var response = s3ResponseObj.Response;
            if (response.ResponseStream != null)
            {   
                bool requestStillValid = (m_currS3ImageIndex <= pictureIndex) &&  (pictureIndex < m_currS3ImageIndex + numImages); // Request no longer valid as user pressed Next or Previous arrows
                string logString02 = string.Format("------- VREEL: Checking validity returned '{0}' when checking that {1} <= {2} < {1}+{3}", requestStillValid, m_currS3ImageIndex, pictureIndex, numImages); 
                Debug.Log(logString02);
                if (requestStillValid)
                {
                    m_coroutineQueue.EnqueueAction(ConvertStreamAndSetImage(response, sphereIndex, fullFilePath));
                    m_coroutineQueue.EnqueueWait(2.0f);

                    Debug.Log("------- VREEL: Successfully downloaded and set " + fullFilePath);
                }
                else
                {
                    Debug.Log("------- VREEL: Downloaded item successfully but was thrown away because user has moved to previous or next: " + fullFilePath);
                }

                Resources.UnloadUnusedAssets();
            }
            else
            {
                Debug.Log("------- VREEL: Got an Exception downloading " + fullFilePath);
            }
        });
    }

    private IEnumerator ConvertStreamAndSetImage(Amazon.S3.Model.GetObjectResponse response, int sphereIndex, string fullFilePath)
    {        
        Debug.Log("------- VREEL: ConvertStreamAndSetImage for " + fullFilePath);


        Debug.Log("------- VREEL: Calling ToByteArray(), on background thread!");
        bool ranJobSuccessfully = false;
        byte[] myBinary = null;
        using (var stream = response.ResponseStream)
        {   
            m_threadJob.Start( () => 
                ranJobSuccessfully = ToByteArray(stream, ref myBinary)
            );
            yield return StartCoroutine(m_threadJob.WaitFor());
        }
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Finished ToByteArray(), ran Job Successully = " + ranJobSuccessfully); 


        Debug.Log("------- VREEL: Calling CalcAndSetDimensionsFromImageData(), on background thread!");
        GCHandle rawDataHandle = GCHandle.Alloc(myBinary, GCHandleType.Pinned);
        IntPtr rawDataPtr = rawDataHandle.AddrOfPinnedObject();

        ranJobSuccessfully = false;
        m_threadJob.Start( () => 
            ranJobSuccessfully = CalcAndSetDimensionsFromImageData(rawDataPtr, myBinary.Length)
        );
        yield return StartCoroutine(m_threadJob.WaitFor());
        Debug.Log("------- VREEL: Finished CalcAndSetDimensionsFromImageData(), ran Job Successully = " + ranJobSuccessfully); 


        Debug.Log("------- VREEL: BLOCKING OPERATION START - Creating Texture unfortunately blocks, size of Texture is Width x Height = " + GetImageWidth() + " x " + GetImageHeight());
        yield return new WaitForEndOfFrame();
        Texture2D myNewTexture2D = new Texture2D(GetImageWidth(), GetImageHeight());
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: BLOCKING OPERATION END - Created Creation!");


        Debug.Log("------- VREEL: Calling LoadDataIntoPixels(), on background thread!");
        Color32[] pixels = myNewTexture2D.GetPixels32(0);
        GCHandle pixelsDataHandle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        IntPtr pixelsPtr = pixelsDataHandle.AddrOfPinnedObject();
        yield return new WaitForEndOfFrame();

        ranJobSuccessfully = false;
        m_threadJob.Start( () => 
            ranJobSuccessfully = LoadImageDataIntoPixels(rawDataPtr, pixelsPtr, myBinary.Length) 
        );
        yield return StartCoroutine(m_threadJob.WaitFor());
        Debug.Log("------- VREEL: Finished LoadDataIntoPixels(), ran Job Successully = " + ranJobSuccessfully);


        Debug.Log("------- VREEL: Calling SetPixels32() and Apply()");
        myNewTexture2D.SetPixels32(pixels);
        yield return new WaitForEndOfFrame();
        myNewTexture2D.Apply();
        yield return new WaitForEndOfFrame();
        rawDataHandle.Free();
        yield return new WaitForEndOfFrame();
        Debug.Log("------- VREEL: Finished SetPixels32() and Apply() on myNewTexture2D!");


        Debug.Log("------- VREEL: Calling SetImageAndFilePath()");
        m_imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(myNewTexture2D, fullFilePath);
        yield return new WaitForEndOfFrame();

        Debug.Log("------- VREEL: Finished Setting Image!");
        Resources.UnloadUnusedAssets();
    }
        
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

        //Array.Reverse(outBinary, 0, outBinary.Length);

        return true;
    }        


    // Attempting to Get the Image facing the correct way up...
    /*
    private bool ToByteArrayReverse(Stream stream, ref byte[] outBinary)
    {               
        const int kBlockSize = 1024;

        long currStreamPos = stream.Length;
        stream.Seek(0, SeekOrigin.End);
        byte[] buffer = new byte[kBlockSize];
        int readLength = kBlockSize;
        using( MemoryStream ms = new MemoryStream() ) 
        {
            int iteration = 0;

            Debug.Log("------- VREEL: Pos = " + currStreamPos + " Iteration = " + iteration);
            
            while(currStreamPos > 0) 
            {
                readLength = (int)(currStreamPos < kBlockSize ? currStreamPos : kBlockSize);
                currStreamPos -= readLength;
                stream.Seek(currStreamPos, SeekOrigin.Begin);

                int read = stream.Read(buffer, 0, readLength);
                byte[] reversed = buffer.Take(read).Reverse().ToArray();
                ms.Write(reversed, 0, read);

                iteration++;
                if (iteration % 100 == 0)
                {
                    Debug.Log("------- VREEL: Pos = " + currStreamPos + " Iteration = " + iteration);
                }
            } 
                
            outBinary = ms.ToArray();
        }

        return true;
    }
    */
}