using System;
using UnityEngine;

public class ControllerBase
{
    public string UserID { get; set; }

    protected WeakReference<Actor> controlActor;

    public bool IsControlling
    {
        get
        {
            if(controlActor == null)
            {
                return false;
            }

            Actor actor = null;
            return controlActor.TryGetTarget(out actor);
        }
    }

    public void Initiallize(string userID)
    {
        if(userID.Equals(string.Empty))
        {
            Debug.LogError("[ControllerBase]UserID is Empty!");
            return;
        }

        UserID = userID;
    }
    
    public bool Possess(Actor possessActor)
    {
        if(controlActor != null)
        {
            Debug.LogWarning("[ControllerBase]Already control Actor!");
            return false;
        }

        controlActor = new WeakReference<Actor>(possessActor);
        return true;
    }

    public WeakReference<Actor> Manumit()
    {
        WeakReference<Actor> returnActor = GetControlActor();
        controlActor = null;
        return returnActor;
    }
    
    public WeakReference<Actor> GetControlActor()
    {
        Actor returnActor = null;
        if (controlActor == null || !controlActor.TryGetTarget(out returnActor))
        {
            Debug.LogWarning("[ControllerBase]Didn't control Actor!");
            return null;
        }
        
        return controlActor;
    }
    
    public void ReserveHeight(float yPosition)
    {
        Actor currentActor = null;
        if (controlActor == null || !controlActor.TryGetTarget(out currentActor))
        {
            Debug.LogWarning("[ControllerBase]Didn't control Actor!");
            return;
        }

        currentActor.Height = yPosition;
    }

    public void SetPosition(Vector2 inputPosition)
    {
        Actor currentActor = null;
        if (controlActor == null || !controlActor.TryGetTarget(out currentActor))
        {
            Debug.LogWarning("[ControllerBase]Didn't control Actor!");
            return;
        }
        Vector3 newPosition = new Vector3(inputPosition.x, currentActor.Height, inputPosition.y);
        currentActor.SetPosition(newPosition);
    }

    public void SetRotate(Vector2 inputDirection)
    {
        Actor currentActor = null;
        if (controlActor == null || !controlActor.TryGetTarget(out currentActor))
        {
            Debug.LogWarning("[ControllerBase]Didn't control Actor!");
            return;
        }

        currentActor.SetRotate(new Vector3(inputDirection.x, 0, inputDirection.y));
    }
}
