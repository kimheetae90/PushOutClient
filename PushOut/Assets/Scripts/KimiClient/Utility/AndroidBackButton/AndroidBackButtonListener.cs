using System;
using System.Collections.Generic;
using UnityEngine;

public class AndroidBackButtonListener : MonoBehaviour
{
    private Action action;

    public void OnEnable()
    {
        Regist();

    }

    public void Regist()
    {
        AndroidBackButtonManager.Instance.Regist(this);
    }

    public void SetAction(Action inAction)
    {
        action = inAction;
    }

    public void OnAndroidBackButton()
    {
        if(action != null)
            action();
    }
}
