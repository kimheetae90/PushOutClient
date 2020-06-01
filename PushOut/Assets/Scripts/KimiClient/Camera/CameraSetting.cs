using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSetting : MonoBehaviour
{
    public Transform cameraTransform;

    public void SetCamera(Camera camera)
    {
        camera.transform.localPosition = cameraTransform.localPosition;
        camera.transform.localRotation = cameraTransform.localRotation;
    }
}
