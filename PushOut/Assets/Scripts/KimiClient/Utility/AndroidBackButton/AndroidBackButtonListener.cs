using System;
using System.Collections.Generic;
using UnityEngine;

public class AndroidBackButtonListener : MonoBehaviour
{
    private Func<bool> action;

    public void OnEnable()
    {
        Regist();
    }

    public void OnDisable()
    {
        Remove();
    }

    public void Regist()
    {
        AndroidBackButtonManager.Instance.Regist(this);
    }

    public void Remove()
    {
        AndroidBackButtonManager.Instance.Remove(this);
    }

    public void SetAction(Func<bool> inAction)
    {
        action = inAction;
    }

    public bool OnAndroidBackButton()
    {
        if(action != null)
        {
            return action();
        }

        return true;
    }
}
