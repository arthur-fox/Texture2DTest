using UnityEngine;
using System.Collections;            //IEnumerator

public class SelectImage : MonoBehaviour 
{
    public float m_scalingFactor = 0.88f;

    private float m_defaultScale = 1.0f;
    private Texture2D m_imageSphereTexture;
    private string m_imageFilePath; 
    private string kEmptyString = "emptyString";

    public void Awake()
    {
        m_imageSphereTexture = new Texture2D(2,2);
    }

    public string GetImageFilePath()
    {
        return m_imageFilePath;
    }

    public void SetImageAndFilePath(byte[] textureStream, string filePath)
    {
        m_imageFilePath = filePath;
        m_imageSphereTexture.LoadImage(textureStream);

        Debug.Log("------- VREEL: Finished Loading Image, texture width x height:  " + m_imageSphereTexture.width + " x " + m_imageSphereTexture.height);

        StartCoroutine(AnimateSetTexture());
    }

    public void SetImageAndFilePath(Texture2D texture, string filePath)
    {
        m_imageFilePath = filePath;
        m_imageSphereTexture = texture;

        StartCoroutine(AnimateSetTexture());
    }

    public void SetImageAndFilePath(ref WWW www, string filePath)
    {
        m_imageFilePath = filePath;
        www.LoadImageIntoTexture(m_imageSphereTexture);

        StartCoroutine(AnimateSetTexture());
    }

    public void Hide()
    {
        m_imageFilePath = kEmptyString;

        StartCoroutine(AnimateHide());
    }

    private IEnumerator AnimateSetTexture()
    {        
        const float kMinShrink = 0.05f; // Minimum value you the sphere can shrink to...
        while (transform.localScale.magnitude > kMinShrink)
        {
            transform.localScale = transform.localScale * m_scalingFactor;
            yield return new WaitForEndOfFrame();
        }

        gameObject.GetComponent<MeshRenderer>().material.mainTexture = m_imageSphereTexture;

        while (transform.localScale.magnitude < m_defaultScale)
        {
            transform.localScale = transform.localScale / m_scalingFactor;
            yield return new WaitForEndOfFrame();
        }

        transform.localScale = new Vector3(m_defaultScale, m_defaultScale, m_defaultScale);
    }

    private IEnumerator AnimateHide()
    {        
        const float kMinShrink = 0.005f; // Minimum value you the sphere can shrink to...
        while (transform.localScale.magnitude > kMinShrink)
        {
            transform.localScale = transform.localScale * m_scalingFactor;
            yield return new WaitForEndOfFrame();
        }
    }
}