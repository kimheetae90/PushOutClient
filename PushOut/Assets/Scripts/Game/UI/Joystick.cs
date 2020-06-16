using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Joystick : UIObject, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public RectTransform rootTransform;
    public Image background;
    public Image controller;

    private Vector2 inputVector;
    private Vector2 pivotPos;

    private bool isInput = false;
    
    private void FixedUpdate()
    {
        if(isInput)
        {
            InputHelper.Instance.OnDirection(inputVector);
        }
    }

    public void SetEnable(bool isEnable)
    {
        rootTransform.gameObject.SetActive(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        inputVector = (eventData.position - pivotPos).normalized;
        controller.rectTransform.anchoredPosition = new Vector3(inputVector.x * (background.rectTransform.sizeDelta.x / 3), inputVector.y * (background.rectTransform.sizeDelta.y / 3));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isInput = true;
        rootTransform.position = eventData.position;
        pivotPos = eventData.position;
        OnDrag(eventData);
        InputHelper.Instance.OnDirection(inputVector);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        controller.rectTransform.anchoredPosition = Vector2.zero;
        InputHelper.Instance.OnDirection(inputVector);
        isInput = false;
    }
    
    public Vector2 GetInputVector()
    {
        return inputVector;
    }
}
