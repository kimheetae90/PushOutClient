using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputHelper : MonoBehaviour
{
    public static InputHelper Instance;

    public delegate void InputDirection(Vector2 direction);
    public InputDirection DirectionDelegate;

    Vector2 prefDirection = Vector2.zero;

    private void Awake()
    {
        Instance = this;
    }

    private void FixedUpdate()
    {
        Vector2 direction = Vector2.zero;

        if(Input.GetKey(KeyCode.A))
        {
            direction.x = -1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            direction.x = 1;
        }
        else
        {
            direction.x = 0;
        }

        if (Input.GetKey(KeyCode.S))
        {
            direction.y = -1;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            direction.y = 1;
        }
        else
        {
            direction.y = 0;
        }

        if(!(prefDirection.x == direction.y && prefDirection.y == direction.y))
        {
            prefDirection = direction;
            OnDirection(direction);
        }
    }

    public void OnDirection(Vector2 direction)
    {
        if(DirectionDelegate != null)
            DirectionDelegate(direction);
    }
}
