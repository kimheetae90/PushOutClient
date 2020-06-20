using System;
using System.Collections.Generic;
using UnityEngine;

public class DummyServer : MonoBehaviour
{
    public Dictionary<string, Entity> entities;
    public List<PushOutForce> pushOutForceList;
    public List<PushOutForce> reservedRemovePushOutForceList;
    public List<PushOutForce> newPushOutForceList;

    public delegate void DeadDelegate(string deadID);
    public DeadDelegate OnDead;

    public delegate void RetryDelegate(Entity entity);
    public RetryDelegate OnRetry;

    public delegate void EnterDelegate(Entity entity);
    public EnterDelegate OnEnter;

    public delegate void ExitDelegate(string id);
    public ExitDelegate OnExit;

    public delegate void PushOutDelegate(List<PushOutForce> pushOutForceList);
    public PushOutDelegate OnPushOut;

    public delegate void SyncEntityDelegate(Entity entity);
    public SyncEntityDelegate OnSyncEntity;

    private readonly float INNER_DEAD_ZONE_RADIUS = 0.6f;
    private readonly float OUTER_DEAD_ZONE_RADIUS = 2.0f;
    private readonly float MAX_PUSHOUT_FORCE = 1.1f;

    private void Awake()
    {
        entities = new Dictionary<string, Entity>();
        pushOutForceList = new List<PushOutForce>();
        reservedRemovePushOutForceList = new List<PushOutForce>();
        newPushOutForceList = new List<PushOutForce>();
    }

    private void FixedUpdate()
    {
        MoveProc();
        PushOutProc();
        CheckDead();

        foreach(PushOutForce force in reservedRemovePushOutForceList)
        {
            if (pushOutForceList.Contains(force))
                pushOutForceList.Remove(force);
        }

        reservedRemovePushOutForceList.Clear();
    }

    private void MoveProc()
    {
        foreach(var node in entities)
        {
            Entity entity = node.Value;
            entity.positionX += entity.directionX * Time.deltaTime;
            entity.positionY += entity.directionY * Time.deltaTime;
        }
    }

    private void PushOutProc()
    {
        foreach(var node in pushOutForceList)
        {
            PushOutForce force = node;
            AddForce(force);
            if (force.force == 0)
                reservedRemovePushOutForceList.Add(force);
        }
    }


    private void AddForce(PushOutForce pushOutForce)
    {
        float applyForce = pushOutForce.force - Convert.ToSingle((DateTime.Now - pushOutForce.createTime).TotalMilliseconds * 0.003);

        Entity entity = Find(pushOutForce.id);

        if (entity == null)
        {
            pushOutForce.force = 0;
            return;
        }

        entity.positionX += pushOutForce.directionX * applyForce * Time.deltaTime;
        entity.positionY += pushOutForce.directionY * applyForce * Time.deltaTime;

        if (applyForce < 0)
        {
            pushOutForce.force = 0;
        }
    }

    private Entity Find(string id)
    {
        Entity entity = null;
        entities.TryGetValue(id, out entity);
        return entity;
    }

    private void CheckDead()
    {
        foreach (var node in entities)
        {
            Entity entity = node.Value;
            if(entity.state != 2 && CheckEnterDeadZone(entity))
            {
                entity.directionX = 0.0f;
                entity.directionY = 0.0f;
                entity.state = 2;
                if(OnDead != null)
                    OnDead(entity.id);
            }
        }
    }

    private bool CheckEnterDeadZone(Entity entity)
    {
        float distanceWithOrigin = Vector2.Distance(Vector2.zero, new Vector2(entity.positionX * 0.001f, entity.positionY * 0.001f));

        if (distanceWithOrigin <= INNER_DEAD_ZONE_RADIUS)
        {
            return true;
        }

        if (distanceWithOrigin >= OUTER_DEAD_ZONE_RADIUS)
        {
            return true;
        }

        return false;
    }

    public void Enter(string id, float posX, float posY, bool useRes = true)
    {
        Entity entity = new Entity();
        entity.state = 0;        
        entity.id = id;
        entity.positionX = posX * 1000;
        entity.positionY = posY * 1000;
        entity.spawnTick = DateTime.Now.Millisecond;
        entities.Add(id, entity);

        if(useRes)
        {
            if (OnEnter != null)
                OnEnter(entity);
        }
        else
        {
            if(OnSyncEntity != null)
                OnSyncEntity(entity);
        }
    }

    public void Enter(Entity inEntity)
    {
        Entity entity = new Entity();
        entity.id = inEntity.id;
        entity.positionX = inEntity.positionX * 1000;
        entity.positionY = inEntity.positionY * 1000;
        entity.spawnTick = DateTime.Now.Millisecond;
        entities.Add(entity.id, entity);
    }

    public void Exit(string id)
    {
        Entity entity = Find(id);
        if (entity == null)
            return;

        entities.Remove(id);
        if(OnExit != null)
            OnExit(id);
    }

    public void Move(string id, float directionX, float directionY)
    {
        Entity entity = Find(id);
        if (entity == null)
            return;

        if (entity.state == 2)
            return;
        int beforeState = entity.state;
        entity.directionX = directionX;
        entity.directionY = directionY;
        entity.state = (Math.Abs(directionX) > 0 || Math.Abs(directionY) > 0) ? 1 : 0;

        if (beforeState == 0 && entity.state == 1)
        {
            var now = DateTime.Now.ToLocalTime();
            var span = (now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
            entity.startPushOutTick = (long)span.TotalMilliseconds;
        }
        else if (beforeState == 1 && entity.state == 0)
        {
            PushOut(entity);
            entity.startPushOutTick = 0;
        }

        if(OnSyncEntity != null)
            OnSyncEntity(entity);
    }

    private void PushOut(Entity entity)
    {
        var now = DateTime.Now.ToLocalTime();
        var span = (now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
        long curTime = (long)span.TotalMilliseconds;
        float force = (curTime - entity.startPushOutTick) * 0.001f;

        if (force > MAX_PUSHOUT_FORCE)
        {
            force = MAX_PUSHOUT_FORCE;
        }

        foreach(var node in entities)
        {
            Entity iterNode = node.Value;
            if(iterNode.id == entity.id)
                continue;

            Vector2 iterPos = new Vector2(iterNode.positionX * 0.001f, iterNode.positionY * 0.001f);
            Vector2 entityPos = new Vector2(entity.positionX * 0.001f, entity.positionY * 0.001f);
            float distance = Vector2.Distance(iterPos, entityPos);
            if (distance > force)
                continue;

            float forceDirX = iterNode.positionX - entity.positionX;
            float forceDirY = iterNode.positionY - entity.positionY;

            float total = (float)Math.Sqrt(forceDirX * forceDirX + forceDirY * forceDirY);

            PushOutForce pushOutForce = new PushOutForce();
            pushOutForce.id = iterNode.id;
            pushOutForce.directionX = forceDirX / total * 1000;
            pushOutForce.directionY = forceDirY / total * 1000;
            pushOutForce.force = (force - distance) * 2.5f + 0.5f;
            pushOutForce.createTime = DateTime.Now;
            newPushOutForceList.Add(pushOutForce);
            pushOutForceList.Add(pushOutForce);
        }

        if(OnPushOut != null)
            OnPushOut(newPushOutForceList);
        newPushOutForceList.Clear();
    }

    public void Retry(string id, float posX, float posY)
    {
        Entity entity = Find(id);
        if (entity == null)
            return;

        entity.positionX = posX * 1000f;
        entity.positionY = posY * 1000f;
        entity.state = 0;
        var now = DateTime.Now.ToLocalTime();
        var span = (now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
        long curTime = (long)span.TotalMilliseconds;
        entity.spawnTick = curTime;
        entity.directionX = 0;
        entity.directionY = 0;
        entity.startPushOutTick = 0;
        entity.clientPushOutTick = 0;

        OnRetry(entity);
    }
}
