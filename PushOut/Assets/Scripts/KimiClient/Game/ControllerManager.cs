using System.Collections.Generic;
using UnityEngine;
using System;

public class ControllerManager
{
    private Dictionary<string, ControllerBase> controllerDic; // key : id

    private string currentControllerID;
    public string CurrentControllerID
    {
        set
        {
            string id = value;
            if (controllerDic.ContainsKey(id))
            {
                currentControllerID = id;
            }
            else
            {
                Debug.LogError("[ControllerManager]Doesn't contain id : " + id);
            }
        }

        get {
            if(controllerDic.ContainsKey(currentControllerID))
            {
                return currentControllerID;
            }
            else
            {
                currentControllerID = string.Empty;
                return string.Empty;
            }
        }
    }
    
    public void Initiallize()
    {
        controllerDic = new Dictionary<string, ControllerBase>();
        currentControllerID = string.Empty;
    }

    public void Add(string id, ControllerBase controller)
    {
        if(controllerDic.ContainsKey(id))
        {
            Debug.LogWarning("[ControllerManager]ID : " + id + " already contain!");
            return;
        }

        controller.Initiallize(id);
        controllerDic.Add(id, controller);
    }

    public void Remove(string id)
    {
        if (!controllerDic.ContainsKey(id))
        {
            Debug.LogWarning("[ControllerManager]ID : " + id + " doesn't contain!");
            return;
        }

        controllerDic.Remove(id);
    }

    public bool Contain(string id)
    {
        return controllerDic.ContainsKey(id);
    }

    public WeakReference<ControllerBase> Get(string id)
    {
        ControllerBase returnController = null;
        if(!controllerDic.TryGetValue(id, out returnController))
        {
            return null;
        }

        return new WeakReference<ControllerBase>(returnController);
    }

    public void Reset()
    {
        controllerDic.Clear();
        currentControllerID = string.Empty;
    }
}
