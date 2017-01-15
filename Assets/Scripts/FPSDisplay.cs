using UnityEngine;
using System.Collections;

public class FPSDisplay : MonoBehaviour
{
    private const float kFrameOutThreshold = 56.0f;

    private float deltaTime = 0.0f;

    void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;

        float fps = 1.0f / deltaTime;
        if (fps < kFrameOutThreshold)
        {
            Debug.Log("------- Texture2DTest: We are Framing out at FPS = " + fps);
        }
    }

    void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;

        Rect rect = new Rect(0, 0, w, h * 2 / 50);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = h * 2 / 50;

        if (fps < kFrameOutThreshold)
        {
            style.normal.textColor = new Color (0.5f, 0.0f, 0.0f, 1.0f);
        }
        else 
        {
            style.normal.textColor = new Color (0.0f, 0.0f, 0.0f, 1.0f);
        }

        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        GUI.Label(rect, text, style);
    }
}