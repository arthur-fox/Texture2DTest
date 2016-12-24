using UnityEngine;
using System.Collections;             // IEnumerator
using System.Collections.Generic;     // List
using System.IO;                      // Stream
using Amazon;                         // UnityInitializer
using Amazon.CognitoIdentity;         // CognitoAWSCredentials
using Amazon.S3;                      // AmazonS3Client
using Amazon.S3.Model;                // ListBucketsRequest

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
    private CppPlugin m_cppPlugin;

    void Start() 
	{
		UnityInitializer.AttachToGameObject(this.gameObject);

        m_credentials = new CognitoAWSCredentials (
            "eu-west-1:1f9f6bd1-3cfe-43c2-afbc-3e06d8d1fe27", // Identity Pool ID
            RegionEndpoint.EUWest1 // Region
        );
        m_s3Client = new AmazonS3Client (m_credentials, RegionEndpoint.EUWest1);

        m_s3ImageFilePaths = new List<string>();

        m_coroutineQueue = new CoroutineQueue(this);
        m_coroutineQueue.StartLoop();

        m_threadJob = new ThreadJob(this);
        m_cppPlugin = new CppPlugin(this);
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
                    m_coroutineQueue.EnqueueAction(LoadImageInternalPlugin(response, sphereIndex, fullFilePath));
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

    private IEnumerator LoadImageInternalPlugin(Amazon.S3.Model.GetObjectResponse response, int sphereIndex, string fullFilePath)
    {        
        Debug.Log("------- VREEL: ConvertStreamAndSetImage for " + fullFilePath);

        using (var stream = response.ResponseStream)
        {
            yield return m_cppPlugin.LoadImageFromStream(m_threadJob, stream, m_imageSpheres, sphereIndex, fullFilePath);
        }
    }

    private IEnumerator LoadImageInternalUnity(Amazon.S3.Model.GetObjectResponse response, int sphereIndex, string fullFilePath)
    {
        Debug.Log("------- VREEL: ConvertStreamAndSetImage for " + fullFilePath);

        const int kNumIterationsPerFrame = 150;
        byte[] myBinary = null;
        using (var stream = response.ResponseStream)
        {            
            using( MemoryStream ms = new MemoryStream() )
            {
                int iterations = 0;
                int byteCount = 0;
                do
                {
                    byte[] buf = new byte[1024];
                    byteCount = stream.Read(buf, 0, 1024);
                    ms.Write(buf, 0, byteCount);
                    iterations++;
                    if (iterations % kNumIterationsPerFrame == 0)
                    {                        
                        yield return new WaitForEndOfFrame();
                    }
                } 
                while(stream.CanRead && byteCount > 0);

                myBinary = ms.ToArray();
            }
        }

        // The following is generally coming out to around 6-7MB in size...
        Debug.Log("------- VREEL: Finished iterating, length of byte[] is " + myBinary.Length);

        Texture2D newImage = new Texture2D(2,2); 
        newImage.LoadImage(myBinary);
        m_imageSpheres[sphereIndex].GetComponent<SelectImage>().SetImageAndFilePath(newImage, fullFilePath);
        yield return new WaitForEndOfFrame();

        Debug.Log("------- VREEL: Finished Setting Image!");

        Resources.UnloadUnusedAssets();
    }
}