using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleSystem
{
    private Dictionary<string, Entity> cacheEntityDic;
    private ObjectPooler<string, PushOutForce> pushOutForcePool;
    private Dictionary<string, AIController> aiControllerDic;
    private List<PushOutForce> reserveRemovePushOutForceList;

    private int pushOutForceIDCount = 0;

    public void Initiallize(Dictionary<string, Entity> inEntityDic)
    {
        cacheEntityDic = inEntityDic;
        pushOutForcePool = new ObjectPooler<string, PushOutForce>();
        pushOutForcePool.Initiallize(60);
        aiControllerDic = new Dictionary<string, AIController>();
        reserveRemovePushOutForceList = new List<PushOutForce>();
    }

    public void Update()
    {
        if (aiControllerDic != null)
        {
            foreach(var aiCont in aiControllerDic)
            {
                aiCont.Value.Compute();
            }
        }

        foreach (var node in cacheEntityDic)
        {
            Entity entity = node.Value;
            MoveEntity(entity);
            SetPushOutForce(entity);
        }

        pushOutForcePool.Each((pushOutForce) =>
        {
            ApplyPushOutForce(pushOutForce);

            if (pushOutForce.force == 0)
            {
                reserveRemovePushOutForceList.Add(pushOutForce);
            }
        });

        foreach (var node in reserveRemovePushOutForceList)
        {
            pushOutForcePool.Return(node);
        }

        reserveRemovePushOutForceList.Clear();
    }

    public void AddAIController(string id, AIController aiController)
    {
        aiControllerDic.Add(id, aiController);
    }

    public void RemoveAIController(string id)
    {
        aiControllerDic.Remove(id);
    }

    public void AddPushOutForce(PushOutForce inPushOutForce)
    {
        var pushOutForce = pushOutForcePool.Get(pushOutForceIDCount.ToString());
        pushOutForce.Copy(inPushOutForce);
        pushOutForce.Adjust();
        pushOutForceIDCount = (pushOutForceIDCount + 1) % int.MaxValue;
    }

    private void MoveEntity(Entity entity)
    {
        if (entity == null || entity.state == (int)EEntityState.Idle)
            return;

        entity.positionX += entity.directionX * Time.deltaTime;
        entity.positionY += entity.directionY * Time.deltaTime;
    }

    private void SetPushOutForce(Entity entity)
    {
        if (entity.state == (int)EEntityState.Move)
        {
            entity.clientPushOutTick += Time.deltaTime;
            float gauge = entity.clientPushOutTick;

            if (gauge > PushOutForce.MAX_PUSHOUT_FORCE)
                gauge = PushOutForce.MAX_PUSHOUT_FORCE;
        }
    }

    private void ApplyPushOutForce(PushOutForce pushOutForce)
    {
        if (pushOutForce == null)
        {
            return;
        }

        Entity entity = null;
        if (!cacheEntityDic.TryGetValue(pushOutForce.id, out entity))
        {
            return;
        }

        float applyForce = pushOutForce.force - Convert.ToSingle((DateTime.Now - pushOutForce.createTime).TotalMilliseconds * 0.003);

        entity.positionX += pushOutForce.directionX * applyForce * Time.deltaTime;
        entity.positionY += pushOutForce.directionY * applyForce * Time.deltaTime;

        if (applyForce < 0)
        {
            pushOutForce.force = 0;
        }
    }

    public void Reset()
    {
        pushOutForcePool.Refresh();
    }
}
