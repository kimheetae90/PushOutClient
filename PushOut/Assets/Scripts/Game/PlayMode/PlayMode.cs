using System;
using System.Collections.Generic;
using UnityEngine;
using UnitySocketIO.Events;

[Serializable]
public class PlayerEnterPacket
{
    public Entity player;
    public string nickname;
}

[Serializable]
public class PlayerExitPacket
{
    public string id;
}

[Serializable]
public class PushOutForceListS2CPacket
{
    public List<PushOutForce> pushOutForceList;
}

[Serializable]
public class PlayerEntityS2CPakcet
{
    public Entity player;
}


[Serializable]
public class PlayerDeadS2CPacket
{
    public string id;
    public string killEntityID;
}

[Serializable]
public class RetryS2CPacket
{
    public Entity player;
    public bool useAD;
}


public class PlayMode : GameMode
{
    public int RoomNumber { get; set; }

    public ActorObjectPooler ActorPool { get; private set; }
    public PushOutEffectPool PushOutEffectPool { get; private set; }
    public NicknameObjectPool NicknamePool { get; private set; }

    public Dictionary<string, Entity> EntitiesDic { get; private set; }

    public EGameButtonFlag GameFlag { get; set; }

    public DummyServer server { get; private set; }

    public bool AIMode { get; private set; }

    public BattleSystem BattleSystemComponent { get; private set; }

    private List<AIContext> aiContextList;
    private List<AIController> aiControllerList;
    public override void Initiallize()
    {
        ModeName = "PlayMode";

        AddState("PlayLoading", new PlayLoadingState());
        AddState("Play", new PlayState());

        ResourceLoader.Instance.Load("Devil/devil");
        ResourceLoader.Instance.Load("Character/Prefabs/Boximon Chopper");
        ResourceLoader.Instance.Load("Character/Prefabs/Boximon Demon");
        ResourceLoader.Instance.Load("Character/Prefabs/Boximon Ghoul");
        ResourceLoader.Instance.Load("Character/Prefabs/Boximon Hellhound");
        ResourceLoader.Instance.Load("Character/Prefabs/Boximon Lava");

        ActorPool = new ActorObjectPooler();
        PushOutEffectPool = new PushOutEffectPool();
        NicknamePool = new NicknameObjectPool();
        EntitiesDic = new Dictionary<string, Entity>();
        BattleSystemComponent = new BattleSystem();
        BattleSystemComponent.Initiallize(EntitiesDic);
        aiControllerList = new List<AIController>();

        Server.Instance.On("PlayerEnterS2C", ReceivePlayerEnter);
        Server.Instance.On("PlayerExitS2C", ReceivePlayerExit);
        Server.Instance.On("PlayerEntityS2C", ReceivePlayerEntity);
        Server.Instance.On("PushOutS2C", ReceivePushOut);
        Server.Instance.On("PlayerDeadS2C", ReceivePlayerDead);
        Server.Instance.On("RetryS2C", ReceiveRetry);
        Server.Instance.On("RetryKeepKillCountS2C", ReceiveRetry);
        Server.Instance.On("ExitRoomS2C", ReceiveExitRoom);

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

    public override void Dispose()
    {
        if (AIMode)
            StopAIMode();

        Server.Instance.Off("PlayerEnterS2C", ReceivePlayerEnter);
        Server.Instance.Off("PlayerExitS2C", ReceivePlayerExit);
        Server.Instance.Off("PlayerEntityS2C", ReceivePlayerEntity);
        Server.Instance.Off("PushOutS2C", ReceivePushOut);
        Server.Instance.Off("PlayerDeadS2C", ReceivePlayerDead);
        Server.Instance.Off("RetryS2C", ReceiveRetry);
        Server.Instance.Off("RetryKeepKillCountS2C", ReceiveRetry);
        Server.Instance.Off("ExitRoomS2C", ReceiveExitRoom);

        ResourceLoader.Instance.Unload("Devil/devil");
        ResourceLoader.Instance.Unload("Character/Prefabs/Boximon Chopper");
        ResourceLoader.Instance.Unload("Character/Prefabs/Boximon Demon");
        ResourceLoader.Instance.Unload("Character/Prefabs/Boximon Ghoul");
        ResourceLoader.Instance.Unload("Character/Prefabs/Boximon Hellhound");
        ResourceLoader.Instance.Unload("Character/Prefabs/Boximon Lava");

        NicknamePool.Clear();
        PushOutEffectPool.Clear();
        ActorPool.Clear();
        EntitiesDic.Clear();
        aiControllerList = null;

        UIManager.Instance.Unload("UI/AIModePanel");
        UIManager.Instance.Unload("UI/Joystick");
        UIManager.Instance.Unload("UI/ResultPopup");
        UIManager.Instance.Unload("UI/LeaderBoard");
        UIManager.Instance.Unload("UI/KillLog");
        UIManager.Instance.Unload("UI/UIRoomNumber");

        ADManager.Instance.HideBanner();
    }

    public override void Update()
    {
        BattleSystemComponent.Update();

        foreach(var node in EntitiesDic)
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

            if (Math.Abs(entity.directionX) > float.Epsilon || Math.Abs(entity.directionY) > float.Epsilon)
            {
                actor.SetRotate(new UnityEngine.Vector3(entity.directionX, 0, entity.directionY));
            }

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
            else if(entity.state == (int)EEntityState.Move)
            {
                entity.clientPushOutTick += Time.deltaTime;
                float gauge = entity.clientPushOutTick;

                if (gauge > PushOutForce.MAX_PUSHOUT_FORCE)
                    gauge = PushOutForce.MAX_PUSHOUT_FORCE;

                pushOutEffect.transform.localScale = new UnityEngine.Vector3(gauge * Actor.ScaleFactor * 2, 0.01f, gauge * Actor.ScaleFactor * 2);
            }
        }
    }
    
    private void ReceivePlayerEnter(SocketIOEvent e)
    {
        Debug.Log("[PacketReceive]PlayLoading Player Enter received: " + e.name + " " + e.data);
        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]PlayerEnter Data is Null!");
            Server.Instance.Disconnect();
            return;
        }

        PlayerEnterPacket playerEnterPacket = JsonUtility.FromJson<PlayerEnterPacket>(e.data);

        var player = playerEnterPacket.player;
        var nickname = playerEnterPacket.nickname;

        CreatePlayer(player, nickname);

        if (AIMode)
            StopAIMode();
    }

    private void ReceivePlayerExit(SocketIOEvent e)
    {
        Debug.Log("[PacketReceive]PlayLoading Player Exit received: " + e.name + " " + e.data);
        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]PlayerExit Data is Null!");
            Server.Instance.Disconnect();
            return;
        }

        PlayerExitPacket packet = JsonUtility.FromJson<PlayerExitPacket>(e.data);

        string player = packet.id;

        RemovePlayer(player);

        if (EntitiesDic.Count == 1)
        {
            StartAIMode();
        }
    }

    private void ReceivePlayerEntity(SocketIOEvent e)
    {
        if (AIMode)
            return;

        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]PlayerEntity Data is Null!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        PlayerEntityS2CPakcet packet = JsonUtility.FromJson<PlayerEntityS2CPakcet>(e.data);

        SyncEntity(packet.player);
    }

    private void ReceivePushOut(SocketIOEvent e)
    {
        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]PushOut Data is Null!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        var pushOutForceList = JsonUtility.FromJson<PushOutForceListS2CPacket>(e.data); // e.data.GetField("pushOutForceList").list;

        foreach (var node in pushOutForceList.pushOutForceList)
        {
            BattleSystemComponent.AddPushOutForce(node);
        }
    }

    private void ReceivePlayerDead(SocketIOEvent e)
    {
        if (AIMode)
            return;

        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]Dead Data is Null!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        PlayerDeadS2CPacket packet = JsonUtility.FromJson<PlayerDeadS2CPacket>(e.data);

        Actor actor = ActorPool.Find(packet.id);
        if (actor == null)
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + packet.id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        Entity entity = null;
        if (!EntitiesDic.TryGetValue(packet.id, out entity))
        {
            Debug.LogError("[PacketReceive]Entity doesn't exist! id : " + packet.id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        GameObject pushOutEffect = PushOutEffectPool.Find(packet.id);
        if (pushOutEffect == null)
        {
            Debug.LogError("[PacketReceive]PushOut Effect doesn't exist! id : " + packet.id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        Actor killactor = ActorPool.Find(packet.killEntityID);
        Entity killentity = null;
        EntitiesDic.TryGetValue(packet.killEntityID, out killentity);
        if (killentity != null)
        {
            killentity.killCount++;
        }

        entity.state = (int)EEntityState.Dead;
        entity.directionX = 0.0f;
        entity.directionY = 0.0f;
        entity.killCount = 0;

        actor.ModelAnimator.SetBool("Dead", true);
        actor.ModelAnimator.SetBool("Move", false);

        pushOutEffect.transform.localScale = UnityEngine.Vector3.zero;
    }


    private void ReceiveRetry(SocketIOEvent e)
    {
        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]Retry Data is Null!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        RetryS2CPacket packet = JsonUtility.FromJson<RetryS2CPacket>(e.data);
        var player = packet.player;

        Entity entity = SyncEntity(player);
        Actor actor = ActorPool.Find(player.id);
        if (actor == null)
        {
            Debug.LogError("[PlayState]Player Actor doesn't exist! Camera Setting Fail!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }
        actor.Height = 0;
        SetVisible(entity.id, !packet.useAD);
    }

    private void ReceiveExitRoom(SocketIOEvent e)
    {
        Server.Instance.Disconnect();
    }

    public void StartAIMode()
    {
#if DUMMY_CLIENT
        return;
#endif

        GameObject serverGO = new GameObject();
        server = serverGO.AddComponent<DummyServer>();

        AIMode = true;

        server.OnEnter += CreateAI;
        server.OnExit += RemovePlayer;
        server.OnSyncEntity += OnSync;
        server.OnPushOut += OnPushOut;
        server.OnDead += OnDead;
        server.OnRetry += OnRetry;

        aiContextList = new List<AIContext>();
        aiContextList.Add(new AIContext("AI1", 0, 1.3f));
        aiContextList.Add(new AIContext("AI2", 0, -1.3f));
        aiContextList.Add(new AIContext("AI3", 1.3f, 0));
        aiContextList.Add(new AIContext("AI4", -1.3f, 0));

        NicknameHUD hud = null;
        foreach (AIContext context in aiContextList)
        {
            server.Enter(context.id, context.posX, context.posY);
            hud = NicknamePool.Find(context.id);
            if (hud != null)
            {
                hud.SetRankTextActive(false);
                hud.SetNickname(context.id);
            }
        }

        Entity entity = null;
        if (EntitiesDic.TryGetValue(GameClient.Instance.UserInfo.UserID, out entity))
        {
            server.Enter(entity.id, 0.4f, 1.1f, false);
        }

        hud = NicknamePool.Find(GameClient.Instance.UserInfo.UserID);
        if (hud != null)
        {
            hud.SetRankTextActive(false);
        }

        BattleSystemComponent.Reset();

        Actor actor = ActorPool.Find(GameClient.Instance.UserInfo.UserID);
        CameraHelper.Instance.Monitor(actor.transform);
        SetVisible(GameClient.Instance.UserInfo.UserID, true);

        UIAIModePanel aiModePanel = UIManager.Instance.Load("UI/AIModePanel") as UIAIModePanel;
        aiModePanel.Show();
        Joystick joystick = UIManager.Instance.Load("UI/Joystick") as Joystick;
        joystick.rootTransform.gameObject.SetActive(true);
        UIResultPopup resultPopup = UIManager.Instance.Load("UI/ResultPopup") as UIResultPopup;
        resultPopup.Hide();
        UILeaderBoard leaderBoard = UIManager.Instance.Load("UI/LeaderBoard") as UILeaderBoard;
        leaderBoard.Hide();
        UIKillLog killLog = UIManager.Instance.Load("UI/KillLog") as UIKillLog;
        killLog.Hide();
    }

    public void StopAIMode()
    {
#if DUMMY_CLIENT
        return;
#endif

        foreach (AIContext context in aiContextList)
        {
            server.Exit(context.id);
            BattleSystemComponent.RemoveAIController(context.id);
        }

        server.OnEnter -= CreateAI;
        server.OnExit -= RemovePlayer;
        server.OnSyncEntity -= OnSync;
        server.OnPushOut -= OnPushOut;
        server.OnDead -= OnDead;
        server.OnRetry -= OnRetry;

        if (server != null && server.gameObject != null)
        {
            GameObject.DestroyImmediate(server.gameObject);
        }
        server = null;
        AIMode = false;

        BattleSystemComponent.Reset();

        aiControllerList.Clear();
        aiContextList.Clear();
        aiContextList = null;

        SetVisible(GameClient.Instance.UserInfo.UserID, false);


        NicknameHUD hud = NicknamePool.Find(GameClient.Instance.UserInfo.UserID);
        if (hud != null)
        {
            hud.SetRankTextActive(true);
        }

        UIAIModePanel aiModePanel = UIManager.Instance.Load("UI/AIModePanel") as UIAIModePanel;
        aiModePanel.Hide();
        UIManager.Instance.Unload("UI/AIModePanel");

        UILeaderBoard leaderBoard = UIManager.Instance.Load("UI/LeaderBoard") as UILeaderBoard;
        leaderBoard.Show();
        UIKillLog killLog = UIManager.Instance.Load("UI/KillLog") as UIKillLog;
        killLog.Show();
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
        SetPlayer(entity);
        SetVisible(id, !entity.super);

        return entity;
    }

    private void SetPlayer(Entity entity)
    {
        string id = entity.id;
        EntitiesDic.Add(id, entity);

        Actor actor = ActorPool.Get(id);
        actor.Initiallize(GetCharacterResourcePath(entity.charID));
        actor.SetPosition(new UnityEngine.Vector3(entity.positionX, 0, entity.positionY));

        NicknameHUD nicknameHUD = NicknamePool.Get(id);
        nicknameHUD.SetNickname(entity.nickName);
        nicknameHUD.transform.SetParent(actor.transform);

        GameObject pushOutEffect = PushOutEffectPool.Get(id);
        pushOutEffect.transform.SetParent(actor.transform);
        pushOutEffect.transform.localPosition = UnityEngine.Vector3.zero;
        pushOutEffect.transform.localScale = UnityEngine.Vector3.zero;
    }

    public void RemovePlayer(string playerID)
    {
        if(!EntitiesDic.ContainsKey(playerID))
        {
            Debug.LogError("[PlayMode]Doesn't exist player id : " + playerID);
            return;
        }

        EntitiesDic.Remove(playerID);
        Actor actor = ActorPool.Find(playerID);
        if (actor != null)
        {
            actor.Clear();
            ActorPool.Return(playerID);
        }

        NicknameHUD nicknameHUD = NicknamePool.Find(playerID);
        if(nicknameHUD != null)
        {
            NicknamePool.Return(playerID);
        }

        GameObject pushOutEffect = PushOutEffectPool.Find(playerID);
        if(pushOutEffect != null)
        {
            PushOutEffectPool.Return(playerID);
        }
    }

    public void CreateAI(Entity inEntity)
    {
        Entity entity = new Entity();
        entity.id = inEntity.id;
        entity.charID = -1;
        entity.Sync(inEntity);
        entity.Adjust();

        string id = entity.id;
        if (EntitiesDic.ContainsKey(id))
        {
            Debug.LogError("[PlayMode]Already exist player id : " + id);
            return;
        }

        SetPlayer(entity);

        AIController aiController = new AIController();
        aiController.CachePlayModeResource(id, server, entity.positionX, entity.positionY);
        BattleSystemComponent.AddAIController(id, aiController);
    }

    public void SetVisible(string id, bool isVisible)
    {
        Actor actor = ActorPool.Find(id);
        bool isPlayerController = GameClient.Instance.UserInfo.UserID.Equals(id);
        if (isPlayerController)
        {
            actor.SetAlpha(!isVisible);
        }
        else
        {
            actor.MeshRenderer.enabled = isVisible;
        }

        NicknameHUD nickName = NicknamePool.Find(id);
        if (nickName != null)
        {
            nickName.gameObject.SetActive(isVisible);
        }
    }

    private void OnPushOut(List<PushOutForce> pushOutForceList)
    {
        foreach (var node in pushOutForceList)
        {
            PushOutForce newPushOut = new PushOutForce();
            newPushOut.Copy(node);
            newPushOut.force *= 1000f;
            BattleSystemComponent.AddPushOutForce(newPushOut);
        }
    }

    private void OnDead(string deadID)
    {
        Actor actor = ActorPool.Find(deadID);
        if (actor == null)
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + deadID);
            return;
        }

        Entity entity = null;
        if (!EntitiesDic.TryGetValue(deadID, out entity))
        {
            Debug.LogError("[PacketReceive]Entity doesn't exist! id : " + deadID);
            return;
        }

        GameObject pushOutEffect = PushOutEffectPool.Find(deadID);
        if (pushOutEffect != null)
        {
            pushOutEffect.transform.localScale = UnityEngine.Vector3.zero;
        }

        entity.state = (int)EEntityState.Dead;
        entity.directionX = 0.0f;
        entity.directionY = 0.0f;
        entity.killCount = 0;

        actor.ModelAnimator.SetBool("Dead", true);

        if (GameClient.Instance.UserInfo.UserID.Equals(deadID))
        {
            CameraHelper.Instance.Freeze();
            server.Retry(deadID, 0.4f, 1.1f);
        }
    }

    private void OnRetry(Entity inentity)
    {
        Entity dummyEntity = new Entity();
        dummyEntity.id = inentity.id;
        dummyEntity.Sync(inentity);
        Entity entity = SyncEntity(dummyEntity);

        Actor actor = ActorPool.Find(entity.id);
        if (actor == null)
        {
            Debug.LogError("[TUtorialState]Player Actor doesn't exist! Camera Setting Fail!");
            return;
        }

        actor.Height = 0;

        if(GameClient.Instance.UserInfo.UserID.Equals(entity.id))
            CameraHelper.Instance.Monitor(actor.transform);
    }

    private void OnSync(Entity entity)
    {
        if (entity == null)
            return;

        Entity dummyEntity = new Entity();

        dummyEntity.id = entity.id;
        dummyEntity.Sync(entity);
        SyncEntity(dummyEntity);
    }

    public Entity SyncEntity(Entity dummyEntity)
    {
        dummyEntity.Adjust();
        Entity entity = null;
        string id = dummyEntity.id;
        if (!EntitiesDic.TryGetValue(id, out entity))
        {
            return null;
        }

        EEntityState beforeState = (EEntityState)entity.state;
        EEntityState afterState = (EEntityState)dummyEntity.state;

        entity.Sync(dummyEntity);

        Actor actor = ActorPool.Find(id);
        if (actor == null)
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + id);
            Server.Instance.Disconnect();
            return null;
        }

        if (beforeState == EEntityState.Idle && afterState == EEntityState.Move)
        {
            actor.ModelAnimator.SetBool("Move", true);
            SetVisible(id, true);
            actor.MeshRenderer.enabled = true;
        }
        else if (beforeState == EEntityState.Move && afterState == EEntityState.Idle)
        {
            actor.ModelAnimator.SetBool("Move", false);
        }
        else if (beforeState == EEntityState.Dead && afterState != EEntityState.Dead)
        {
            actor.ModelAnimator.SetBool("Dead", false);
        }

        return entity;
    }

    private string GetCharacterResourcePath(float charID)
    {
        if(charID == -1)
        {
            return "Devil/devil";
        }
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
