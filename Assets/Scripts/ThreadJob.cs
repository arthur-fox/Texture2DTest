using UnityEngine;
using System;                   // Func
using System.Collections;       // IEnumerator
using System.Threading;         // Thread

public class ThreadJob
{
    // **************************
    // Member Variables
    // **************************

    // Thread-safe "IsDone" check!
    public bool IsDone                     
    {
        get
        {
            bool tmp;
            lock (m_handle)
            {
                tmp = m_isDoneFlag;
            }
            return tmp;
        }
        set
        {
            lock (m_handle)
            {
                m_isDoneFlag = value;
            }
        }
    }

    private bool m_isDoneFlag = true;
    private object m_handle = new object();
    private MonoBehaviour m_owner = null;
    private Func<object> m_threadFunc;

    // **************************
    // Public functions
    // **************************

    public ThreadJob(MonoBehaviour owner)
    {
        m_owner = owner;
        Debug.Log("------- VREEL: A ThreadJob was created by = " + m_owner.name);
    }

    public void Start(Func<object> threadFunc)
    {
        Debug.Log("------- VREEL: Start on Thread has been called!");

        //TODO: Make the ThreadFunc have its own "IsDone" flag, 
        //      because the current method means that only one ThreadFunc is allowed to run at a time...
        m_threadFunc = threadFunc;
        IsDone = false;
        ThreadPool.QueueUserWorkItem(Run);
    }

    public IEnumerator WaitFor()
    {
        while(!IsDone)
        {
            yield return null;
        }
    }

    // **************************
    // Private/Helper functions
    // **************************

    private void Run(object stateInfo)
    {
        Debug.Log("------- VREEL: Began Running function on background thread!");

        m_threadFunc();
        IsDone = true;

        Debug.Log("------- VREEL: Finished Running function on background thread!");
    }
}