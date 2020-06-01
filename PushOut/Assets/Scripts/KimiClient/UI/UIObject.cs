using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIObject : MonoBehaviour
{
    [SerializeField]
    protected AndroidBackButtonListener androidBackButtonListner;

    public delegate void OnShowDelegate();
    public delegate void OnHideDelegate();

    public OnShowDelegate OnShow;
    public OnHideDelegate OnHide;

    public void Show()
    {
        this.gameObject.SetActive(true);
        if (OnShow != null)
        {
            OnShow();
        }
    }

    public void Hide()
    {
        if(OnHide != null)
        {
            OnHide();
        }
        this.gameObject.SetActive(false);
    }
}
