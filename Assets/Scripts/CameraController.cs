using UnityEngine;

public class CameraController : MonoBehaviour 
{    
    public float m_rightRate = 1f;

    private bool m_rotating = false;

    public void SetRotating(bool rotating)
    {
        m_rotating = rotating;
    }

	public void Update () 
    {
        if (m_rotating)
        {
            transform.Rotate(Vector3.up * Time.deltaTime * -m_rightRate);
        }              
	}
}