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
    public Dictionary<string, NicknameHUD> CachedNicknameDic { get; private set; }

    public DummyServer server { get; private set; }

    public override void Dispose()
    {
        CachedNicknameDic = null;

        NicknamePool.Clear();
        PushOutEffectPool.Clear();
        ActorPool.Clear();

        foreach (var node in EntitiesDic)
        {
            Entity entity = node.Value;
            if (!GameClient.Instance.ControllerManager.CurrentControllerID.Equals(entity.id))
            {
                GameClient.Instance.ControllerManager.Remove(entity.id);
            }
        }

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

        GameClient.Instance.ControllerManager.Add("player", new PlayerController());
        GameClient.Instance.ControllerManager.CurrentControllerID = "player";

        ActorPool = new ActorObjectPooler();
        PushOutEffectPool = new PushOutEffectPool();
        NicknamePool = new NicknameObjectPool();
        EntitiesDic = new Dictionary<string, Entity>();
        CachedNicknameDic = new Dictionary<string, NicknameHUD>();

        GameObject serverGO = new GameObject();
        server = serverGO.AddComponent<DummyServer>();

        server.OnEnter += CreatePlayer;
        server.OnExit += RemovePlayer;
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

        bool isPlayerController = GameClient.Instance.ControllerManager.CurrentControllerID.Equals(id);
        ControllerBase controller = null;
        if (isPlayerController)
        {
            WeakReference<ControllerBase> controllerRef = null;
            controllerRef = GameClient.Instance.ControllerManager.Get(id);
            if (!controllerRef.TryGetTarget(out controller))
            {
                controller = new PlayerController();
                GameClient.Instance.ControllerManager.Add(id, controller);
            }
        }
        else
        {
            if (GameClient.Instance.ControllerManager.Contain(id))
            {
                Debug.LogError("[PlayMode]Already exist player id : " + id);
                return ;
            }

            controller = new AIController();
            GameClient.Instance.ControllerManager.Add(id, controller);
        }

        EntitiesDic.Add(id, entity);

        Actor actor = ActorPool.Get();
        actor.Initiallize("Devil/devil");
        controller.Possess(actor);
        controller.ReserveHeight(0);
        controller.SetPosition(new Vector2(entity.positionX, entity.positionY));

        NicknameHUD nicknameHUD = NicknamePool.Get();
        nicknameHUD.SetNickname(entity.nickName);
        nicknameHUD.transform.SetParent(actor.transform);
        CachedNicknameDic.Add(entity.id, nicknameHUD);
    }

    public void RemovePlayer(string playerID)
    {
        if (!EntitiesDic.ContainsKey(playerID))
        {
            Debug.LogError("[PlayMode]Doesn't exist player id : " + playerID);
            return;
        }

        EntitiesDic.Remove(playerID);
        WeakReference<ControllerBase> controllerPtr = GameClient.Instance.ControllerManager.Get(playerID);
        ControllerBase controller = null;
        if (!controllerPtr.TryGetTarget(out controller))
        {
            Debug.LogWarning("[PlayMode]Doesn't exist controller! id : " + playerID);
            return;
        }

        WeakReference<Actor> actorPtr = controller.Manumit();
        Actor actor = null;
        if (!actorPtr.TryGetTarget(out actor))
        {
            Debug.LogWarning("[PlayMode]Doesn't exist actor! id : " + playerID);
            return;
        }

        GameClient.Instance.ControllerManager.Remove(playerID);
        controller.ReserveHeight(0);
        actor.Clear();
        ActorPool.Return(actor);

        NicknameHUD nicknameHUD = null;
        if (CachedNicknameDic.TryGetValue(playerID, out nicknameHUD))
        {
            CachedNicknameDic.Remove(playerID);
            NicknamePool.Return(nicknameHUD);
        }
    }
}
