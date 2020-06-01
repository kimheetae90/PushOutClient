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
    
    public void Copy(PushOutForce copy)
    {
        directionX = copy.directionX * 0.001f;
        directionY = copy.directionY * 0.001f;
        id = copy.id;
        force = copy.force * 0.001f;
        createTime = DateTime.Now;
    }
}