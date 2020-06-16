using System;

public enum EEntityState
{
    Idle,
    Move,
    Dead,
}

[Serializable]
public class Entity
{
    public string id;
    public float positionX;
    public float positionY;
    public float directionX;
    public float directionY;
    public int state;
    public long startPushOutTick;
    public string nickName;
    public long spawnTick;
    public int killCount;
    public bool useAD;
    public bool super;
    public float clientPushOutTick;
    public float charID;
    
    public void Adjust()
    {
        positionX = positionX * 0.001f;
        positionY = positionY * 0.001f;
        directionX = directionX * 0.001f;
        directionY = directionY * 0.001f;
    }

    public void Sync(Entity entity)
    {
        positionX = entity.positionX;
        positionY = entity.positionY;
        directionX = entity.directionX;
        directionY = entity.directionY;
        state = entity.state;
        startPushOutTick = entity.startPushOutTick;
        spawnTick = entity.spawnTick;
        killCount = entity.killCount;
        useAD = entity.useAD;
        super = entity.super;
        charID = entity.charID;
    }
}

