using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;   //UnityEvent
using System;

public class MenuButton : MonoBehaviour
{   
    public UnityEvent OnButtonSelectedFunc;              // This event is triggered when the selection of the button has finished.

    private bool m_gazeOver = false;                     // Whether the user is looking at the VRInteractiveItem currently.
    private bool m_buttonDown = false;                   // Whether the user is pushing the VRInteractiveItem down.

    private void OnMouseOver()
    {            
        m_gazeOver = true;
    }

    private void OnMouseExit()
    {            
        m_gazeOver = false;
    }

    private void OnMouseDown()
    {            
        m_buttonDown = true;
    }

    private void OnMouseUp()
    {
        if (m_buttonDown && m_gazeOver)
        {
            if (OnButtonSelectedFunc != null)
            {
                OnButtonSelectedFunc.Invoke();
            }
        }      

        m_buttonDown = false;
    }
}   