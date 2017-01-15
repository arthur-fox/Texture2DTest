using UnityEngine;
using System.Collections;            //IEnumerator

public class SelectImage : MonoBehaviour 
{
    public float m_scalingFactor = 0.88f;
    public float m_defaultScale = 1.0f;

    private Texture2D m_imageTexture;
    private string m_imageFilePath; 
    private string kEmptyString = "emptyString";

    public void Awake()
    {
        m_imageTexture = new Texture2D(2,2);
    }

    public string GetImageFilePath()
    {
        return m_imageFilePath;
    }

    public void SetImageAndFilePath(Texture2D texture, string filePath)
    {
        m_imageFilePath = filePath;
        m_imageTexture = texture;

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

        gameObject.GetComponent<MeshRenderer>().material.mainTexture = m_imageTexture;

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