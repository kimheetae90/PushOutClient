using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMessageBox : MonoBehaviour
{
    public static UIMessageBox Instance;

    public Text desc;
    public GameObject OkButton;
    public GameObject CancelButton;
    public GameObject CloseButton;

    private Action okAction;
    private Action cancelAction;
    private Action closeAction;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        this.gameObject.AddComponent<AndroidBackButtonListener>();
        gameObject.SetActive(false);
        gameObject.transform.localScale = new Vector3(1, 1, 1);
    }

    public void Show(string text, Action ok, Action cancel)
    {
        transform.SetAsLastSibling();
        AndroidBackButtonListener androidBackButtonListner = this.gameObject.GetComponent<AndroidBackButtonListener>();
        androidBackButtonListner.SetAction(OnBackButtonCancel);
        gameObject.SetActive(true);
        OkButton.SetActive(true);
        CancelButton.SetActive(true);
        CloseButton.SetActive(false);

        desc.text = text;
        okAction = ok;
        cancelAction = cancel;
    }

    public void Show(string text, Action close = null)
    {
        transform.SetAsLastSibling();
        AndroidBackButtonListener androidBackButtonListner = this.gameObject.GetComponent<AndroidBackButtonListener>();
        androidBackButtonListner.SetAction(OnBackButtonClose);
        gameObject.SetActive(true);
        OkButton.SetActive(false);
        CancelButton.SetActive(false);
        CloseButton.SetActive(true);

        desc.text = text;

        okAction = null;
        cancelAction = null;
        closeAction = close;
    }

    public void OnClickOk()
    {
        if (okAction != null)
            okAction();

        gameObject.SetActive(false);
    }

    public bool OnBackButtonCancel()
    {
        if (!gameObject.activeSelf)
            return false;

        OnClickCancel();

        return true;
    }

    public bool OnBackButtonClose()
    {
        if (!gameObject.activeSelf)
            return false;

        OnClickClose();

        return true;
    }

    public void OnClickCancel()
    {
        if (cancelAction != null)
            cancelAction();

        gameObject.SetActive(false);
    }

    public void OnClickClose()
    {
        if (closeAction != null)
            closeAction();

        gameObject.SetActive(false);
    }
}
