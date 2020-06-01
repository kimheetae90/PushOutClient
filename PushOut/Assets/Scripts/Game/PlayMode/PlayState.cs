using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnitySocketIO.Events;

[Serializable]
public class PushOutForceListS2CPacket
{
    public List<PushOutForce> pushOutForceList;
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

[Serializable]
public class PlayerEntityS2CPakcet
{
    public Entity player;
}

[Serializable]
public class PlayerChangeMovementC2SPakcet
{
    public float directionX;
    public float directionY;
}

public struct AIContext
{
    public string id;
    public float posX;
    public float posY;

    public AIContext(string id, float posX, float posY)
    {
        this.id = id;
        this.posX = posX;
        this.posY = posY;
    }
}


public class PlayState : FSMState
{
    private PlayMode cachedMode;
    private Entity cachedPlayerEntity;
    private ControllerBase cachedPlayerController;

    private Entity dummyEntity;
    private Vector2 prevDirection;
    private ObjectPooler<PushOutForce> pushOutForcePool;
    private List<PushOutForce> reserveRemovePushOutForceList;
    private Dictionary<string, GameObject> attachedPushOutEffect;
    
    private WeakReference<UIResultPopup> resultPopupRef;
    private WeakReference<UILeaderBoard> leaderBoardRef;
    private WeakReference<UIKillLog> killLogRef;

    private List<Entity> orderedBySpawnTimeEntityList;
    private List<Entity> deadEntityList;

    private List<AIContext> aiContextList;
    private List<AIController> aiControllerList;

    public override void Enter()
    {
        Debug.Log("Enter");

        Server.Instance.On("PlayerEnterS2C", ReceivePlayerEnter);
        Server.Instance.On("PlayerExitS2C", ReceivePlayerExit);
        Server.Instance.On("PlayerEntityS2C", ReceivePlayerEntity);
        Server.Instance.On("PushOutS2C", ReceivePushOut);
        Server.Instance.On("PlayerDeadS2C", ReceivePlayerDead);
        Server.Instance.On("RetryS2C", ReceiveRetry);
        Server.Instance.On("RetryKeepKillCountS2C", ReceiveRetry);
        Server.Instance.On("connectionS2C", LoginSuccess);
        Server.Instance.On("ExitRoomS2C", ReceiveExitRoom);

        cachedMode = Base as PlayMode;

        if (!cachedMode.EntitiesDic.TryGetValue(GameClient.Instance.ControllerManager.CurrentControllerID, out cachedPlayerEntity))
        {
            Debug.LogError("[PlayState]Player Entity doesn't exist!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(GameClient.Instance.ControllerManager.CurrentControllerID);
        if (controllerRef == null || !controllerRef.TryGetTarget(out cachedPlayerController))
        {
            Debug.LogError("[PlayState]Player Controller doesn't exist!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

#if QAMode
        AutoPlayerController qaController = cachedPlayerController as AutoPlayerController;
        if (qaController != null)
            qaController.CachePlayModeResource();
#endif

        InputHelper.Instance.DirectionDelegate += InputDirection;

        dummyEntity = new Entity();
        pushOutForcePool = new ObjectPooler<PushOutForce>();
        pushOutForcePool.Initiallize(100);
        reserveRemovePushOutForceList = new List<PushOutForce>();
        attachedPushOutEffect = new Dictionary<string, GameObject>();
        orderedBySpawnTimeEntityList = new List<Entity>();
        deadEntityList = new List<Entity>();
        aiControllerList = new List<AIController>();
        
        leaderBoardRef = new WeakReference<UILeaderBoard>(UIManager.Instance.Load("UI/LeaderBoard") as UILeaderBoard);
        killLogRef = new WeakReference<UIKillLog>(UIManager.Instance.Load("UI/KillLog") as UIKillLog);
        UIRoomNumber uiRoomNumber = UIManager.Instance.Load("UI/UIRoomNumber") as UIRoomNumber;
        uiRoomNumber.SetRoomNumber(cachedMode.RoomNumber);

        Dictionary<string, Entity> entityDic = cachedMode.EntitiesDic;
        foreach (var node in entityDic)
        {
            CachingPushOutEffect(node.Value);
        }

        WeakReference<Actor> actorRef = cachedPlayerController.GetControlActor();
        Actor actor = null;
        if (actorRef == null || !actorRef.TryGetTarget(out actor))
        {
            Debug.LogError("[PlayState]Player Actor doesn't exist! Camera Setting Fail!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        CameraHelper.Instance.Monitor(actor.transform);

        Joystick joystick = UIManager.Instance.Load("UI/Joystick") as Joystick;
        if (joystick != null)
        {
            joystick.rootTransform.gameObject.SetActive(true);
        }

        UpdateLeaderBoard();

        if (cachedMode.EntitiesDic.Count > 1 && PlayerPrefs.GetInt("GuideForEnterSuper", 0) == 0)
        {
            UIMessageBox.Instance.Show("첫 진입시에는 움직이기 전까지 무적입니다.(광고시청시 동일효과)");
            PlayerPrefs.SetInt("GuideForEnterSuper", 1);
        }

#if !QAMode
        if (entityDic.Count == 1)
        {
            StartAIMode();
        }
#endif
    }

    private void EnableBannerActive()
    {
        BannerActive(true);
    }

    private void LoginSuccess(SocketIOEvent e)
    {
        Server.Instance.Disconnect();
    }

    public override void Stay()
    {
        if (aiControllerList != null)
        {
            for(int i = 0; i < aiControllerList.Count; i++)
            {
                aiControllerList[i].Compute();
            }
        }

        Dictionary<string,Entity> entityDic = cachedMode.EntitiesDic;

        foreach (var node in entityDic)
        {
            Entity entity = node.Value;
            MoveEntity(entity);
            SetPushOutEffect(entity);
        }
        
        pushOutForcePool.Each((pushOutForce) =>
        {
            AddPushOutForce(pushOutForce);
            
            if(pushOutForce.force == 0)
            {
                reserveRemovePushOutForceList.Add(pushOutForce);
            }
        });

        foreach(var node in reserveRemovePushOutForceList)
        {
            pushOutForcePool.Return(node);
        }

        reserveRemovePushOutForceList.Clear();

#if QAMode
        AutoPlayerController qaController = cachedPlayerController as AutoPlayerController;
        if (qaController != null)
            qaController.Compute();
#endif
    }
    
    public override void Exit()
    {
#if QAMode
        if(cachedMode.AIMode)
            StopAIMode();
#endif
        cachedPlayerController.Manumit();
    }

    public override void Dispose()
    {
        Server.Instance.Off("PlayerEnterS2C", ReceivePlayerEnter);
        Server.Instance.Off("PlayerExitS2C", ReceivePlayerExit);
        Server.Instance.Off("PlayerEntityS2C", ReceivePlayerEntity);
        Server.Instance.Off("PushOutS2C", ReceivePushOut);
        Server.Instance.Off("PlayerDeadS2C", ReceivePlayerDead);
        Server.Instance.Off("RetryS2C", ReceiveRetry);
        Server.Instance.Off("ExitRoomS2C", ReceiveExitRoom);
        Server.Instance.Off("connectionS2C", LoginSuccess);

        cachedMode = null;
        cachedPlayerEntity = null;
        cachedPlayerController = null;

        InputHelper.Instance.DirectionDelegate -= InputDirection;

        dummyEntity = null;
        pushOutForcePool = null;
        reserveRemovePushOutForceList = null;
        attachedPushOutEffect = null;
        orderedBySpawnTimeEntityList = null;
        deadEntityList = null;
        aiControllerList = null;

        ResourceLoader.Instance.Unload("Devil/devil");
        ResourceLoader.Instance.Unload("Character/Prefabs/Boximon Chopper");
        ResourceLoader.Instance.Unload("Character/Prefabs/Boximon Demon");
        ResourceLoader.Instance.Unload("Character/Prefabs/Boximon Ghoul");
        ResourceLoader.Instance.Unload("Character/Prefabs/Boximon Hellhound");
        ResourceLoader.Instance.Unload("Character/Prefabs/Boximon Lava");
        UIManager.Instance.Unload("UI/ResultPopup");
        UIManager.Instance.Unload("UI/LeaderBoard");
        UIManager.Instance.Unload("UI/KillLog");
        UIManager.Instance.Unload("UI/UIRoomNumber");
        UIManager.Instance.Unload("UI/Joystick");
        UIManager.Instance.Unload("UI/AIModePanel");
    }

    private void CachingPushOutEffect(Entity entity)
    {
        if (entity == null)
            return;

        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(entity.id);
        ControllerBase controller = null;
        if (controllerRef == null || !controllerRef.TryGetTarget(out controller))
        {
            Debug.LogError("[PlayState]Controller doesn't exist! id : " + entity.id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        WeakReference<Actor> actorRef = controller.GetControlActor();
        Actor actor = null;
        if(actorRef == null || !actorRef.TryGetTarget(out actor))
        {
            Debug.LogError("[PlayState]Actor doesn't exist! id : " + entity.id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }
        
        GameObject pushOutEffect = cachedMode.PushOutEffectPool.Get();
        pushOutEffect.transform.SetParent(actor.transform);
        pushOutEffect.transform.localPosition = Vector3.zero;
        pushOutEffect.transform.localScale = Vector3.zero;

        attachedPushOutEffect.Add(entity.id, pushOutEffect);
    }

    private void MoveEntity(Entity entity)
    {
        if (entity == null || entity.state == (int)EEntityState.Idle)
            return;

        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(entity.id);
        ControllerBase controller = null;
        if (controllerRef == null || !controllerRef.TryGetTarget(out controller))
        {
            Debug.LogError("[PlayState]Controller doesn't exist! id : " + entity.id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        if(entity.state == (int)EEntityState.Dead)
        {
            WeakReference<Actor> actorRef = controller.GetControlActor();
            Actor actor = null;
            if (actorRef == null || !actorRef.TryGetTarget(out actor))
            {
                Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + entity.id);
                UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
                Server.Instance.Disconnect();
                return;
            }
            controller.ReserveHeight(actor.Height - Time.deltaTime * 5f);
        }

        entity.positionX += entity.directionX * Time.deltaTime;
        entity.positionY += entity.directionY * Time.deltaTime;

        controller.SetPosition(new Vector2(entity.positionX, entity.positionY));
    }

    private void SetPushOutEffect(Entity entity)
    {
        if (entity == null)
            return;

        GameObject pushOutEffect = null;
        if(!attachedPushOutEffect.TryGetValue(entity.id, out pushOutEffect))
        {
            return;
        }

        if (entity.state == (int)EEntityState.Idle || entity.state == (int)EEntityState.Dead)
        {
            entity.clientPushOutTick = 0;
            if (pushOutEffect.transform.localScale.magnitude > 1.0f)
            {
                WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(entity.id);
                ControllerBase controller = null;
                if (controllerRef == null || !controllerRef.TryGetTarget(out controller))
                {
                    return;
                }

                WeakReference<Actor> actorRef = controller.GetControlActor();
                Actor actor = null;
                if (actorRef == null || !actorRef.TryGetTarget(out actor))
                {
                    return;
                }

                actor.ModelAnimator.SetTrigger("Attack");
            }
            pushOutEffect.transform.localScale = new Vector3(0, 0, 0);
            return;
        }

        if(entity.state ==(int)EEntityState.Move)
        {
            entity.clientPushOutTick += Time.deltaTime;
            float gauge = entity.clientPushOutTick;

            if (gauge > PushOutForce.MAX_PUSHOUT_FORCE)
                gauge = PushOutForce.MAX_PUSHOUT_FORCE;

            pushOutEffect.transform.localScale = new Vector3(gauge * Actor.ScaleFactor * 2, 0.01f, gauge * Actor.ScaleFactor * 2);
        }
    }

    private void AddPushOutForce(PushOutForce pushOutForce)
    {
        if (pushOutForce == null)
        {
            Debug.LogWarning("[PlayState]PushOutForce is Null!");
            return;
        }

        Dictionary<string, Entity> entityDic = cachedMode.EntitiesDic;
        Entity entity = null;
        if(!entityDic.TryGetValue(pushOutForce.id, out entity))
        {
            return;
        }

        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(entity.id);
        ControllerBase controller = null;
        if (controllerRef == null || !controllerRef.TryGetTarget(out controller))
        {
            return;
        }

        float applyForce = pushOutForce.force - Convert.ToSingle((DateTime.Now - pushOutForce.createTime).TotalMilliseconds * 0.003);

        entity.positionX += pushOutForce.directionX * applyForce * Time.deltaTime;
        entity.positionY += pushOutForce.directionY * applyForce * Time.deltaTime;
        
        controller.SetPosition(new Vector2(entity.positionX, entity.positionY));
        
        if (applyForce < 0)
        {
            pushOutForce.force = 0;
        }
    }
    
    private void ReceivePlayerEnter(SocketIOEvent e)
    {
        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]PlayerEnter Data is Null!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

#if !QAMode
        if (cachedMode.AIMode)
        {
            StopAIMode();
        }
#endif

        PlayerEnterPacket packet = JsonUtility.FromJson<PlayerEnterPacket>(e.data);
        
        Entity entity = cachedMode.CreatePlayer(packet.player, packet.nickname);
        CachingPushOutEffect(entity);

        UpdateLeaderBoard();
    }

    private void ReceivePlayerExit(SocketIOEvent e)
    {
        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]PlayerExit Data is Null!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        PlayerExitPacket packet = JsonUtility.FromJson<PlayerExitPacket>(e.data);
        cachedMode.RemovePlayer(packet.id);
        GameObject pushOutEffect = null;
        if(attachedPushOutEffect.TryGetValue(packet.id, out pushOutEffect))
        {
            cachedMode.PushOutEffectPool.Return(pushOutEffect);
            attachedPushOutEffect.Remove(packet.id);
        }

        UpdateLeaderBoard();

#if !QAMode
        if(cachedMode.EntitiesDic.Count == 1)
        {
            StartAIMode();
        }
#endif
    }


    private void ReceivePlayerEntity(SocketIOEvent e)
    {
        if (cachedMode.AIMode)
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

        UpdateLeaderBoard();
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
            var pushOutForce = pushOutForcePool.Get();
            pushOutForce.Copy(node);
        }
        
    }

    private void ReceivePlayerDead(SocketIOEvent e)
    {
        if (cachedMode.AIMode)
            return;

        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]Dead Data is Null!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        PlayerDeadS2CPacket packet = JsonUtility.FromJson<PlayerDeadS2CPacket>(e.data);
        
        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(packet.id);
        ControllerBase controller = null;
        if (controllerRef == null || !controllerRef.TryGetTarget(out controller))
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + packet.id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        WeakReference<Actor> actorRef = controller.GetControlActor();
        Actor actor = null;
        if (actorRef == null || !actorRef.TryGetTarget(out actor))
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + packet.id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        Entity entity = null;
        if (!cachedMode.EntitiesDic.TryGetValue(packet.id, out entity))
        {
            Debug.LogError("[PacketReceive]Entity doesn't exist! id : " + packet.id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        GameObject pushOutEffect = null;
        if (!attachedPushOutEffect.TryGetValue(packet.id, out pushOutEffect))
        {
            Debug.LogError("[PacketReceive]PushOut Effect doesn't exist! id : " + packet.id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }
        
        WeakReference<ControllerBase> killcontrollerRef = GameClient.Instance.ControllerManager.Get(packet.killEntityID);
        ControllerBase killcontroller = null;
        if (killcontrollerRef != null)
            killcontrollerRef.TryGetTarget(out killcontroller);

        WeakReference<Actor> killactorRef = controller.GetControlActor();
        Actor killactor = null;
        if (killactorRef != null)
            killactorRef.TryGetTarget(out killactor);

        Entity killentity = null;
        cachedMode.EntitiesDic.TryGetValue(packet.killEntityID, out killentity);

        if(killentity != null)
        {
            killentity.killCount++;
        }
        
        KillLog((killentity == null) ? entity.nickName : killentity.nickName, entity.nickName);

        UpdateLeaderBoard();

        if (GameClient.Instance.ControllerManager.CurrentControllerID == packet.id)
        {
            Joystick joystick = UIManager.Instance.Load("UI/Joystick") as Joystick;
            joystick.rootTransform.gameObject.SetActive(false);
            UIResultPopup resultPopup = null;
            if (resultPopupRef == null || !resultPopupRef.TryGetTarget(out resultPopup))
            {
                resultPopupRef = new WeakReference<UIResultPopup>(UIManager.Instance.Load("UI/ResultPopup") as UIResultPopup);
                resultPopupRef.TryGetTarget(out resultPopup);
            }

            resultPopup.SetLeaderBoard(orderedBySpawnTimeEntityList);
            resultPopup.SetADButton(!cachedPlayerEntity.useAD);

            UILeaderBoard leaderBoard = null;
            if (leaderBoardRef.TryGetTarget(out leaderBoard))
            {
                leaderBoard.gameObject.SetActive(false);
            }

            resultPopup.Show();

            CameraHelper.Instance.Freeze();
            BannerActive(false);

            if (PlayerPrefs.GetInt("GuideForADSuper", 0) == 0)
            {
                UIMessageBox.Instance.Show("광고를 보면 킬 카운트가 유지되고 움직이기전까지 무적입니다");
                PlayerPrefs.SetInt("GuideForADSuper", 1);
            }
        }

        entity.state = (int)EEntityState.Dead;
        entity.directionX = 0.0f;
        entity.directionY = 0.0f;
        entity.killCount = 0;

        actor.ModelAnimator.SetBool("Dead", true);
        
        pushOutEffect.transform.localScale = Vector3.zero;
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
        
        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(entity.id);
        ControllerBase controller = null;
        if (controllerRef == null || !controllerRef.TryGetTarget(out controller))
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + entity.id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        WeakReference<Actor> actorRef = cachedPlayerController.GetControlActor();
        Actor actor = null;
        if (actorRef == null || !actorRef.TryGetTarget(out actor))
        {
            Debug.LogError("[PlayState]Player Actor doesn't exist! Camera Setting Fail!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        controller.ReserveHeight(0);

        UIResultPopup resultPopup = null;
        if (resultPopupRef != null && resultPopupRef.TryGetTarget(out resultPopup))
        {
            resultPopup.Hide();
        }

        Joystick joystick = UIManager.Instance.Load("UI/Joystick") as Joystick;
        joystick.rootTransform.gameObject.SetActive(true);

        WeakReference<Actor> myActorRef = cachedPlayerController.GetControlActor();
        Actor myActor = null;
        if (myActorRef == null || !myActorRef.TryGetTarget(out myActor))
        {
            Debug.LogError("[PlayState]Player Actor doesn't exist! Camera Setting Fail!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        CameraHelper.Instance.Monitor(myActor.transform);
        BannerActive(true);

        cachedMode.SetVisible(entity.id, !packet.useAD);

        UILeaderBoard leaderBoard = null;
        if (leaderBoardRef.TryGetTarget(out leaderBoard))
        {
            leaderBoard.gameObject.SetActive(true);
            UpdateLeaderBoard();
        }

        UpdateLeaderBoard();
    }
    
    private void ReceiveExitRoom(SocketIOEvent e)
    {
        Server.Instance.Disconnect();
    }

    public void InputDirection(Vector2 direction)
    {
        if (cachedPlayerEntity.state == (int)EEntityState.Dead)
        {
            return;
        }

        if(Math.Abs(direction.x - prevDirection.x) > float.Epsilon || Math.Abs(direction.y - prevDirection.y) > float.Epsilon)
        {
            SendDirectionToServer(direction);
            prevDirection = direction;
        }
    }

    private void SendDirectionToServer(Vector2 direction)
    {
        float total = Math.Abs(direction.x) + Math.Abs(direction.y);
        if (total == 0)
        {
            float gauge = cachedPlayerEntity.clientPushOutTick;
            if (gauge > PushOutForce.MAX_PUSHOUT_FORCE)
                gauge = PushOutForce.MAX_PUSHOUT_FORCE;
            CameraHelper.Instance.Shake(gauge * 0.3f);
            total = 1;
        }

        PlayerChangeMovementC2SPakcet packet = new PlayerChangeMovementC2SPakcet();
        packet.directionX = direction.x / total * 1000;
        packet.directionY = direction.y / total * 1000;

        if (cachedMode.AIMode)
        {
            cachedMode.server.Move(cachedPlayerEntity.id, packet.directionX, packet.directionY);
        }
        else
        {
            Server.Instance.Emit("PlayerChangeMovementC2S", JsonUtility.ToJson(packet));
        }   
    }

    public Entity SyncEntity(Entity dummyEntity)
    {
        dummyEntity.Adjust();
        Entity entity = null;
        string id = dummyEntity.id;
        if (!cachedMode.EntitiesDic.TryGetValue(id, out entity))
        {
            Debug.LogError("[PacketReceive]Entity is Null! id : " + id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return null;
        }

        EEntityState beforeState = (EEntityState)entity.state;
        EEntityState afterState = (EEntityState)dummyEntity.state;

        entity.Sync(dummyEntity);
        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(id);
        ControllerBase controller = null;
        if (controllerRef == null || !controllerRef.TryGetTarget(out controller))
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + id);
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return null;
        }
        
        controller.SetPosition(new Vector2(entity.positionX, entity.positionY));
        
        if (Math.Abs(entity.directionX) > float.Epsilon || Math.Abs(entity.directionY) > float.Epsilon)
        {
            controller.SetRotate(new Vector2(entity.directionX, entity.directionY));
        }
        
        WeakReference<Actor> actorRef = controller.GetControlActor();
        Actor actor = null;
        if (actorRef == null || !actorRef.TryGetTarget(out actor))
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + id);
            Server.Instance.Disconnect();
            return null;
        }

        if(beforeState == EEntityState.Idle && afterState == EEntityState.Move)
        {
            actor.ModelAnimator.SetBool("Move", true);
            cachedMode.SetVisible(id, true);
            actor.MeshRenderer.enabled = true;
        }
        else if(beforeState == EEntityState.Move && afterState == EEntityState.Idle)
        {
            actor.ModelAnimator.SetBool("Move", false);
        }
        else if(beforeState == EEntityState.Dead && afterState != EEntityState.Dead)
        {
            actor.ModelAnimator.SetBool("Dead", false);
        }

        return entity;
    }

    private void UpdateLeaderBoard()
    {
        SortEntityKillCount(cachedMode.EntitiesDic, ref orderedBySpawnTimeEntityList, ref deadEntityList);

        UILeaderBoard leaderBoard = null;
        if(leaderBoardRef.TryGetTarget(out leaderBoard))
        {
            leaderBoard.Set(orderedBySpawnTimeEntityList);
        }

        for (int index = 0; index < orderedBySpawnTimeEntityList.Count; index++)
        {
            Entity entity = orderedBySpawnTimeEntityList[index];

            NicknameHUD hud = null;
            if(cachedMode.CachedNicknameDic.TryGetValue(entity.id, out hud))
            {
                hud.SetRankTextActive(true);
                hud.SetRank(index + 1);
            }
        }

        foreach(var entity in deadEntityList)
        {
            NicknameHUD hud = null;
            if (cachedMode.CachedNicknameDic.TryGetValue(entity.id, out hud))
            {
                hud.SetRankTextActive(false);
            }
        }
    }

    private void SortEntityKillCount(Dictionary<string, Entity> entityDic, ref List<Entity> inOrderedEntityList, ref List<Entity> inDeadEntityList)
    {
        inOrderedEntityList.Clear();
        inDeadEntityList.Clear();

        foreach (var node in entityDic)
        {
            Entity entity = node.Value;

            if(entity.state == (int)EEntityState.Dead)
            {
                inDeadEntityList.Add(entity);
            }
            else
            {
                int index = 0;
                for (; index < inOrderedEntityList.Count ; index++)
                {
                    Entity orderEntity = inOrderedEntityList[index];
                    if(orderEntity.killCount <= entity.killCount)
                    {
                        break;
                    }
                }

                inOrderedEntityList.Insert(index, entity);
            }
        }
    }

    private void KillLog(string killer, string killed)
    {
        UIKillLog killLog = null;
        if (killLogRef.TryGetTarget(out killLog))
        {
            killLog.Add(killer, killed);
        }
    }

    private void BannerActive(bool isActive)
    {
        if(isActive)
        {
            ADManager.Instance.ShowBanner();
        }
        else
        {
            ADManager.Instance.HideBanner();
        }
    }


    // AI Mode

    private void StartAIMode()
    {
        cachedMode.StartAIMode();

        cachedMode.server.OnSyncEntity += OnSync;
        cachedMode.server.OnPushOut += OnPushOut;
        cachedMode.server.OnDead += OnDead;
        cachedMode.server.OnRetry += OnRetry;

        aiContextList = new List<AIContext>();        
        aiContextList.Add(new AIContext("AI1", 0, 1.3f));
        aiContextList.Add(new AIContext("AI2", 0, -1.3f));
        aiContextList.Add(new AIContext("AI3", 1.3f , 0));
        aiContextList.Add(new AIContext("AI4", -1.3f , 0));

        foreach (AIContext context in aiContextList)
        {
            cachedMode.server.Enter(context.id, context.posX, context.posY);
            AddAIController(context.id);
            CachingAIPushOutEffect(context.id);
        }

        cachedPlayerController.ReserveHeight(0);
        cachedMode.server.Enter(cachedPlayerEntity.id, 0.4f, 1.1f, false);

        WeakReference<Actor> actorRef = cachedPlayerController.GetControlActor();
        Actor actor = null;
        if (actorRef == null || !actorRef.TryGetTarget(out actor))
        {
            Debug.LogError("[PlayState]Player Actor doesn't exist! Camera Setting Fail!");            
            return;
        }

        CameraHelper.Instance.Monitor(actor.transform);

        cachedMode.SetVisible(cachedPlayerController.UserID, true);

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

        pushOutForcePool.Refresh();
    }

    private void AddAIController(string id)
    {
        ControllerBase controller = null;
        AIController aiController = null;
        GameClient.Instance.ControllerManager.Get(id).TryGetTarget(out controller);
        aiController = controller as AIController;
        aiControllerList.Add(aiController);
        AIContext aiContext = FindAIContext(id);
        aiController.CachePlayModeResource(cachedMode.server, aiContext.posX, aiContext.posY);
    }

    private void CachingAIPushOutEffect(string id)
    {
        Entity entity = null;
        cachedMode.EntitiesDic.TryGetValue(id, out entity);
        NicknameHUD hud = null;
        if (cachedMode.CachedNicknameDic.TryGetValue(entity.id, out hud))
        {
            hud.rank.gameObject.SetActive(false);
            hud.SetNickname(id);
        }
        CachingPushOutEffect(entity);
    }

    private AIContext FindAIContext(string id)
    {
        return aiContextList.Find(x => x.id == id);
    }

    private void StopAIMode()
    {
        GameObject pushOutEffect = null;
        foreach (AIContext context in aiContextList)
        {
            cachedMode.server.Exit(context.id);
            if (attachedPushOutEffect.TryGetValue(context.id, out pushOutEffect))
            {
                cachedMode.PushOutEffectPool.Return(pushOutEffect);
                attachedPushOutEffect.Remove(context.id);
            }
        }
        cachedMode.server.OnSyncEntity -= OnSync;
        cachedMode.server.OnPushOut -= OnPushOut;
        cachedMode.server.OnDead -= OnDead;
        cachedMode.server.OnRetry -= OnRetry;
#if !QAMode
        cachedMode.StopAIMode();
#endif
        aiControllerList.Clear();
        aiContextList.Clear();
        aiContextList = null;

        cachedMode.SetVisible(cachedPlayerController.UserID, false);

        UIAIModePanel aiModePanel = UIManager.Instance.Load("UI/AIModePanel") as UIAIModePanel;
        aiModePanel.Hide();
        UIManager.Instance.Unload("UI/AIModePanel");

        pushOutForcePool.Refresh();

        UILeaderBoard leaderBoard = UIManager.Instance.Load("UI/LeaderBoard") as UILeaderBoard;
        leaderBoard.Show();
        UIKillLog killLog = UIManager.Instance.Load("UI/KillLog") as UIKillLog;
        killLog.Show();
    }

    private void OnPushOut(List<PushOutForce> pushOutForceList)
    {
        foreach (var node in pushOutForceList)
        {
            var pushOutForce = pushOutForcePool.Get();
            pushOutForce.Copy(node);
            pushOutForce.force *= 1000f;
        }
    }

    private void OnDead(string deadID)
    {
        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(deadID);
        ControllerBase controller = null;
        if (controllerRef == null || !controllerRef.TryGetTarget(out controller))
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + deadID);
            return;
        }

        WeakReference<Actor> actorRef = controller.GetControlActor();
        Actor actor = null;
        if (actorRef == null || !actorRef.TryGetTarget(out actor))
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + deadID);
            return;
        }

        Entity entity = null;
        if (!cachedMode.EntitiesDic.TryGetValue(deadID, out entity))
        {
            Debug.LogError("[PacketReceive]Entity doesn't exist! id : " + deadID);
            return;
        }

        GameObject pushOutEffect = null;
        if (attachedPushOutEffect.TryGetValue(deadID, out pushOutEffect))
        {
            pushOutEffect.transform.localScale = Vector3.zero;
        }

        entity.state = (int)EEntityState.Dead;
        entity.directionX = 0.0f;
        entity.directionY = 0.0f;
        entity.killCount = 0;

        actor.ModelAnimator.SetBool("Dead", true);

        if (GameClient.Instance.ControllerManager.CurrentControllerID == deadID)
        {
            CameraHelper.Instance.Freeze();
            cachedMode.server.Retry(deadID, 0.4f, 1.1f);
        }
        else
        {
        }
    }

    private void OnRetry(Entity inentity)
    {
        dummyEntity.id = inentity.id;
        dummyEntity.Sync(inentity);
        Entity entity = SyncEntity(dummyEntity);

        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(entity.id);
        ControllerBase controller = null;
        if (controllerRef == null || !controllerRef.TryGetTarget(out controller))
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + entity.id);
            return;
        }

        WeakReference<Actor> actorRef = cachedPlayerController.GetControlActor();
        Actor actor = null;
        if (actorRef == null || !actorRef.TryGetTarget(out actor))
        {
            Debug.LogError("[TUtorialState]Player Actor doesn't exist! Camera Setting Fail!");
            return;
        }

        controller.ReserveHeight(0);

        CameraHelper.Instance.Monitor(actor.transform);
    }

    private void OnSync(Entity entity)
    {
        if (entity == null)
            return;

        dummyEntity.id = entity.id;
        dummyEntity.Sync(entity);
        SyncEntity(dummyEntity);
    }

}
