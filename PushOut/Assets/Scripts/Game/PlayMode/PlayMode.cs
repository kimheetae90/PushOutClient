using System;
using System.Collections.Generic;
using UnityEngine;
using UnitySocketIO.Events;

public class PlayMode : GameMode
{
    public int RoomNumber { get; set; }

    public ActorObjectPooler ActorPool { get; private set; }
    public PushOutEffectPool PushOutEffectPool { get; private set; }
    public NicknameObjectPool NicknamePool { get; private set; }

    public GameObject Map { get; private set; }

    public Dictionary<string, Entity> EntitiesDic { get; private set; }
    public Dictionary<string, NicknameHUD> CachedNicknameDic { get; private set; }
    
    public EGameButtonFlag GameFlag { get; set; }

    public DummyServer server { get; private set; }

    public bool AIMode { get; private set; }

    public override void Initiallize()
    {
        ModeName = "PlayMode";

        AddState("PlayLoading", new PlayLoadingState());
        AddState("Play", new PlayState());

        ActorPool = new ActorObjectPooler();
        PushOutEffectPool = new PushOutEffectPool();
        NicknamePool = new NicknameObjectPool();
        EntitiesDic = new Dictionary<string, Entity>();
        CachedNicknameDic = new Dictionary<string, NicknameHUD>();

        if (GameFlag != EGameButtonFlag.PublicJoin)
        {
            Action EnableBannerActive = () => { ADManager.Instance.ShowBanner(); };
            ADManager.Instance.ShowReward(EnableBannerActive, EnableBannerActive, EnableBannerActive);
        }
        else
        {
            ADManager.Instance.ShowBanner();
        }
    }
    
    public void StartAIMode()
    {
        GameObject serverGO = new GameObject();
        server = serverGO.AddComponent<DummyServer>();

        server.OnEnter += CreateAI;
        server.OnExit += RemovePlayer;
        AIMode = true;
    }

    public void StopAIMode()
    {
        server.OnEnter -= CreateAI;
        server.OnExit -= RemovePlayer;

        if (server != null && server.gameObject != null)
        {
            GameObject.DestroyImmediate(server.gameObject);
        }
        server = null;
        AIMode = false;
    }

    public Entity CreatePlayer(Entity entity, string nickname)
    {
        entity.Adjust();

        string id = entity.id;
        if (EntitiesDic.ContainsKey(id))
        {
            Debug.LogError("[PlayMode]Already exist player id : " + id);
            return null;
        }

        entity.nickName = nickname;

        EntitiesDic.Add(id, entity);
        Actor actor = ActorPool.Get();
        actor.Initiallize(GetCharacterResourcePath(entity.charID));

        bool isPlayerController = GameClient.Instance.ControllerManager.CurrentControllerID.Equals(id);
        ControllerBase controller = null;
        if (isPlayerController)
        {
            WeakReference<ControllerBase> controllerRef = null;
            controllerRef = GameClient.Instance.ControllerManager.Get(id);
            if(!controllerRef.TryGetTarget(out controller))
            {
#if QAMode
                controller = new AutoPlayerController();
#else
                controller = new PlayerController();
#endif
            }
        }
        else
        {
            if (GameClient.Instance.ControllerManager.Contain(id))
            {
                Debug.LogError("[PlayMode]Already exist player id : " + id);
                return null;
            }

            controller = new PlayerController();
            GameClient.Instance.ControllerManager.Add(id, controller);
        }

        controller.Possess(actor);
        controller.ReserveHeight(0);
        controller.SetPosition(new Vector2(entity.positionX, entity.positionY));

        NicknameHUD nicknameHUD = NicknamePool.Get();
        nicknameHUD.SetNickname(entity.nickName);
        nicknameHUD.transform.SetParent(actor.transform);
        CachedNicknameDic.Add(entity.id, nicknameHUD);

        SetVisible(id, !entity.super);

        return entity;
    }

    public void RemovePlayer(string playerID)
    {
        if(!EntitiesDic.ContainsKey(playerID))
        {
            Debug.LogError("[PlayMode]Doesn't exist player id : " + playerID);
            return;
        }

        EntitiesDic.Remove(playerID);
        WeakReference<ControllerBase> controllerPtr = GameClient.Instance.ControllerManager.Get(playerID);
        ControllerBase controller = null;
        if(!controllerPtr.TryGetTarget(out controller))
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
        if(CachedNicknameDic.TryGetValue(playerID, out nicknameHUD))
        {
            CachedNicknameDic.Remove(playerID);
            NicknamePool.Return(nicknameHUD);
        }
    }

    public void CreateAI(Entity inEntity)
    {
        Entity entity = new Entity();
        entity.id = inEntity.id;
        entity.Sync(inEntity);
        entity.Adjust();

        string id = entity.id;
        if (EntitiesDic.ContainsKey(id))
        {
            Debug.LogError("[PlayMode]Already exist player id : " + id);
            return;
        }

        if (GameClient.Instance.ControllerManager.Contain(id))
        {
            Debug.LogError("[PlayMode]Already exist player id : " + id);
            return;
        }

        AIController controller = new AIController();
        GameClient.Instance.ControllerManager.Add(id, controller);

        EntitiesDic.Add(id, entity);

        Actor actor = ActorPool.Get();
        actor.Initiallize("Devil/devil");
        controller.Possess(actor);
        controller.SetPosition(new Vector2(entity.positionX, entity.positionY));

        controller.ReserveHeight(0);

        NicknameHUD nicknameHUD = NicknamePool.Get();
        nicknameHUD.SetNickname(entity.nickName);
        nicknameHUD.transform.SetParent(actor.transform);
        CachedNicknameDic.Add(entity.id, nicknameHUD);
    }

    public override void Dispose()
    {
        CachedNicknameDic = null;

        NicknamePool.Clear();
        PushOutEffectPool.Clear();
        ActorPool.Clear();

        foreach (var node in EntitiesDic)
        {
            Entity entity = node.Value;
            if(!GameClient.Instance.ControllerManager.CurrentControllerID.Equals(entity.id))
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

        if(AIMode)
            StopAIMode();

        ADManager.Instance.HideBanner();
    }

    public void SetVisible(string id, bool isVisible)
    {
        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(id);
        ControllerBase controller = null;
        if (controllerRef == null || !controllerRef.TryGetTarget(out controller))
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        WeakReference<Actor> actorRef = controller.GetControlActor();
        Actor actor = null;
        if (actorRef == null || !actorRef.TryGetTarget(out actor))
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        bool isPlayerController = GameClient.Instance.ControllerManager.CurrentControllerID.Equals(id);
        if (isPlayerController)
        {
            actor.SetAlpha(!isVisible);
        }
        else
        {
            actor.MeshRenderer.enabled = isVisible;
        }

        if (CachedNicknameDic.ContainsKey(id))
        {
            CachedNicknameDic[id].gameObject.SetActive(isVisible);
        }
    }

    private string GetCharacterResourcePath(float charID)
    {
        if(charID >= 0 && charID <= 1)
        {
            return "Character/Prefabs/Boximon Chopper";
        }
        else if (charID > 1 && charID <= 2)
        {
            return "Character/Prefabs/Boximon Demon";
        }
        else if (charID > 2 && charID <= 3)
        {
            return "Character/Prefabs/Boximon Ghoul";
        }
        else if (charID > 3 && charID <= 4)
        {
            return "Character/Prefabs/Boximon Hellhound";
        }
        else if (charID > 4 && charID <= 5)
        {
            return "Character/Prefabs/Boximon Lava";
        }

        return string.Empty;
    }
}
