using System;
using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using UnityEngine;

public class CameraHelper : MonoBehaviour
{
    public static CameraHelper Instance;

    public Camera currentCamera;

    private Transform monitorObject;
    private Coroutine shakeCoroutine;
    private Vector3 pivotPosition;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if(monitorObject != null)
        {
            this.transform.position = monitorObject.transform.position;
        }
    }

    public void Monitor(Transform inMonitorObject)
    {
        monitorObject = inMonitorObject;

        CameraSetting setting = inMonitorObject.GetComponentInChildren<CameraSetting>();

        if(setting == null)
        {
            currentCamera.transform.position = Vector3.zero;
            currentCamera.transform.rotation = Quaternion.identity;
        }
        else
        {
            setting.SetCamera(currentCamera);
            pivotPosition = currentCamera.transform.localPosition;
        }
    }

    public void Freeze()
    {
        monitorObject = null;
    }

    public void Shake(float value)
    {
        if(shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            currentCamera.transform.localPosition = pivotPosition;
        }
        shakeCoroutine = StartCoroutine(CoShake(value));
    }

    IEnumerator CoShake(float value)
    {
        float endValue = value * 0.1f;
        bool isPivotPosition = true;
        while (true)
        {
            if(!isPivotPosition)
            {
                isPivotPosition = true;
                currentCamera.transform.localPosition = pivotPosition;
            }
            else
            {
                isPivotPosition = false;
                Vector2 vector = GenerateDirection();
                vector *= value;
                value *= 0.8f;
                if (value < endValue)
                    break;
                currentCamera.transform.localPosition = new Vector3(pivotPosition.x + vector.x, pivotPosition.y, pivotPosition.z + vector.y);
            }
            yield return null;
        }
    }

    private Vector2 GenerateDirection()
    {
        float xdir = UnityEngine.Random.Range(0, 1.0f);
        float ydir = UnityEngine.Random.Range(0, 1.0f);

        Vector2 vector = new Vector2(xdir, ydir);
        return vector.normalized;
    }
}
