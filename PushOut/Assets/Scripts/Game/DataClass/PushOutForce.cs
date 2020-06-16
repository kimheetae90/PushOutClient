using System;

[Serializable]
public class PushOutForce
{
    public float directionX;
    public float directionY;
    public string id;
    public float force;
    public DateTime createTime;
    
    public static float MAX_PUSHOUT_FORCE = 1.0f;
    
    public void Adjust()
    {
        directionX *= 0.001f;
        directionY *= 0.001f;
        force *= 0.001f;
    }

    public void Copy(PushOutForce copy)
    {
        directionX = copy.directionX;
        directionY = copy.directionY;
        id = copy.id;
        force = copy.force;
        createTime = DateTime.Now;
    }
}