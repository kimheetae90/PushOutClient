using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialMode : GameMode
{
    public ActorObjectPooler ActorPool { get; private set; }
    public PushOutEffectPool PushOutEffectPool { get; private set; }
    public NicknameObjectPool NicknamePool { get; private set; }

    public GameObject Map { get; private set; }

    public Dictionary<string, Entity> EntitiesDic { get; private set; }

    public DummyServer server { get; private set; }

    public BattleSystem BattleSystemComponent { get; private set; }

    public override void Dispose()
    {
        NicknamePool.Clear();
        PushOutEffectPool.Clear();
        ActorPool.Clear();

        if (Map != null)
        {
            GameObject.DestroyImmediate(Map);
            Map = null;
        }

        EntitiesDic.Clear();
        if(server != null && server.gameObject != null)
        {
            GameObject.DestroyImmediate(server.gameObject);
        }
        server = null;
    }

    public override void Initiallize()
    {
        ModeName = "TutorialMode";

        AddState("TutorialLoading", new TutorialLoadingState());
        AddState("Tutorial", new TutorialState());

        ActorPool = new ActorObjectPooler();
        PushOutEffectPool = new PushOutEffectPool();
        NicknamePool = new NicknameObjectPool();
        EntitiesDic = new Dictionary<string, Entity>();

        GameObject serverGO = new GameObject();
        server = serverGO.AddComponent<DummyServer>();

        BattleSystemComponent = new BattleSystem();
        BattleSystemComponent.Initiallize(EntitiesDic);

        server.OnEnter += CreatePlayer;
        server.OnExit += RemovePlayer;
    }


    public override void Update()
    {
        BattleSystemComponent.Update();

        foreach (var node in EntitiesDic)
        {
            Entity entity = node.Value;
            Actor actor = ActorPool.Find(entity.id);
            if (actor == null)
            {
                continue;
            }

            if (entity.state == (int)EEntityState.Dead)
            {
                actor.Height -= Time.deltaTime * 5.0f;
            }

            actor.SetPosition(new UnityEngine.Vector3(entity.positionX, actor.Height, entity.positionY));

            GameObject pushOutEffect = PushOutEffectPool.Find(entity.id);
            if (entity.state == (int)EEntityState.Idle || entity.state == (int)EEntityState.Dead)
            {
                entity.clientPushOutTick = 0;
                if (pushOutEffect.transform.localScale.magnitude > 1.0f)
                {
                    actor.ModelAnimator.SetTrigger("Attack");
                }
                pushOutEffect.transform.localScale = new UnityEngine.Vector3(0, 0, 0);
            }
            else if (entity.state == (int)EEntityState.Move)
            {
                entity.clientPushOutTick += Time.deltaTime;
                float gauge = entity.clientPushOutTick;

                if (gauge > PushOutForce.MAX_PUSHOUT_FORCE)
                    gauge = PushOutForce.MAX_PUSHOUT_FORCE;

                pushOutEffect.transform.localScale = new UnityEngine.Vector3(gauge * Actor.ScaleFactor * 2, 0.01f, gauge * Actor.ScaleFactor * 2);
            }
        }
    }

    public void CreatePlayer(Entity inentity)
    {
        Entity entity = new Entity();
        entity.id = inentity.id;
        entity.Sync(inentity);
        entity.Adjust();

        string id = entity.id;
        if (EntitiesDic.ContainsKey(id))
        {
            Debug.LogError("[PlayMode]Already exist player id : " + id);
            return ;
        }

        EntitiesDic.Add(id, entity);
        Actor actor = ActorPool.Get(id);
        actor.Initiallize("Devil/devil");
        
        NicknameHUD nicknameHUD = NicknamePool.Get(id);
        nicknameHUD.SetNickname(entity.nickName);
        nicknameHUD.transform.SetParent(actor.transform);
    }

    public void RemovePlayer(string playerID)
    {
        if (!EntitiesDic.ContainsKey(playerID))
        {
            Debug.LogError("[PlayMode]Doesn't exist player id : " + playerID);
            return;
        }

        EntitiesDic.Remove(playerID);
        Actor actor = ActorPool.Find(playerID);
        if (actor == null)
        {
            Debug.LogWarning("[PlayMode]Doesn't exist actor! id : " + playerID);
            return;
        }

        actor.Height = 0;
        actor.Clear();
        ActorPool.Return(playerID);
        NicknamePool.Return(playerID);
    }
}
