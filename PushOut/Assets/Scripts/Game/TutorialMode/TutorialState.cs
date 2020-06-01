using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public enum ETutorialSequenceStep
{
    None,
    ShowStartDialog,
    ShowMapAndPlayerDesc,
    ShowMoveDesc,
    Moving,
    ShowGaugeDesc,
    MoveToShowGauge,
    ShowPushoOutDesc,
    GenAI,
    ShowKill,
    KillAI,
    ShowExercise,
    Exercise,
    End
}

public class TutorialState : FSMState
{
    private TutorialMode cachedMode;
    private Entity cachedPlayerEntity;
    private ControllerBase cachedPlayerController;
    private Vector2 prevDirection;

    private Entity dummyEntity;
    private ObjectPooler<PushOutForce> pushOutForcePool;
    private List<PushOutForce> reserveRemovePushOutForceList;
    private Dictionary<string, GameObject> attachedPushOutEffect;

    private DummyServer server;

    private ETutorialSequenceStep step;
    private Coroutine tutorialSequence;


    public override void Enter()
    {
        cachedMode = Base as TutorialMode;

        cachedMode.server.OnDead += OnDead;
        cachedMode.server.OnPushOut += OnPushOut;
        cachedMode.server.OnSyncEntity += OnSync;
        cachedMode.server.OnRetry += OnRetry;

        dummyEntity = new Entity();
        pushOutForcePool = new ObjectPooler<PushOutForce>();
        pushOutForcePool.Initiallize(100);
        reserveRemovePushOutForceList = new List<PushOutForce>();
        attachedPushOutEffect = new Dictionary<string, GameObject>();
        
        if (!cachedMode.EntitiesDic.TryGetValue(GameClient.Instance.ControllerManager.CurrentControllerID, out cachedPlayerEntity))
        {
            Debug.LogError("[PlayState]Player Entity doesn't exist!");
            return;
        }

        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(GameClient.Instance.ControllerManager.CurrentControllerID);
        if (controllerRef == null || !controllerRef.TryGetTarget(out cachedPlayerController))
        {
            Debug.LogError("[PlayState]Player Controller doesn't exist!");
            return;
        }

        NicknameHUD hud = null;
        if(cachedMode.CachedNicknameDic.TryGetValue(cachedPlayerEntity.id, out hud))
        {
            hud.rank.gameObject.SetActive(false);
            hud.SetNickname("PushOut.io");
        }

        CachingPushOutEffect(cachedPlayerEntity);

        step = ETutorialSequenceStep.ShowStartDialog;
        tutorialSequence = GameClient.Instance.StartCoroutine(TutorialSequence());
    }

    public override void Dispose()
    {
        if (cachedMode != null && cachedMode.server != null)
        {
            cachedMode.server.OnDead -= OnDead;
            cachedMode.server.OnPushOut -= OnPushOut;
            cachedMode.server.OnSyncEntity -= OnSync;
            cachedMode.server.OnRetry -= OnRetry;
        }
        SetJoystick(false);
        UIManager.Instance.Unload("UI/Joystick");
        dummyEntity = null;
        pushOutForcePool.Clear();
        pushOutForcePool = null;
        reserveRemovePushOutForceList.Clear();
        reserveRemovePushOutForceList = null;
        attachedPushOutEffect.Clear();
        attachedPushOutEffect = null;
        cachedPlayerEntity = null;
        cachedPlayerController = null;
        GameClient.Instance.StopCoroutine(tutorialSequence); 
        cachedMode = null;

    }

    public override void Stay()
    {
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
            cachedMode.server.Retry("player", 0, 1.3f);
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

        SetJoystick(true);

        CameraHelper.Instance.Monitor(actor.transform);
    }

    private void OnSync(Entity entity)
    {
        if (entity == null)
            return;

        SyncEntity(entity);
    }

    private IEnumerator TutorialSequence()
    {
        yield return new WaitForSeconds(0.5f);

        UIMessageBox.Instance.Show("튜토리얼을 진행합니다.", () => 
        { 
            step = ETutorialSequenceStep.ShowMapAndPlayerDesc;

            WeakReference<Actor> actorRef = cachedPlayerController.GetControlActor();
            Actor actor = null;
            if (actorRef == null || !actorRef.TryGetTarget(out actor))
            {
                Debug.LogError("[PlayState]Player Actor doesn't exist! Camera Setting Fail!");
                return;
            }

            CameraHelper.Instance.Monitor(actor.transform);
        });

        while (step == ETutorialSequenceStep.ShowStartDialog) yield return null;
        yield return new WaitForSeconds(0.5f);

        UIMessageBox.Instance.Show("맵과 플레이어를 생성했습니다.", () => 
        {
            step = ETutorialSequenceStep.ShowMoveDesc; 
        });

        while (step == ETutorialSequenceStep.ShowMapAndPlayerDesc) yield return null;
        yield return new WaitForSeconds(0.5f);

        float posX = cachedPlayerEntity.positionX;
        float posY = cachedPlayerEntity.positionY;

        UIMessageBox.Instance.Show("화면을 드래그하면 캐릭터가 움직입니다.", () =>
        {
            step = ETutorialSequenceStep.Moving;
            SetJoystick(true);
            GameObject pushOutForceGO = null;
            if(attachedPushOutEffect.TryGetValue(cachedPlayerEntity.id, out pushOutForceGO))
            {
                pushOutForceGO.SetActive(false);
            }
        });

        while (step == ETutorialSequenceStep.ShowMoveDesc || step == ETutorialSequenceStep.Moving)
        {
            if (0.3f < Vector2.Distance(new Vector2(posX, posY), new Vector2(cachedPlayerEntity.positionX, cachedPlayerEntity.positionY)))
            {
                step = ETutorialSequenceStep.ShowGaugeDesc;
            }
            yield return null;
        }

        SetJoystick(false);
        InputDirection(Vector2.zero);
        InputHelper.Instance.DirectionDelegate -= InputDirection;
        UIMessageBox.Instance.Show("움직이면 캐릭터를 둘러싸는 게이지가 생기고 점점 커집니다.", () =>
        {
            step = ETutorialSequenceStep.MoveToShowGauge;
            GameObject pushOutForceGO = null;
            if (attachedPushOutEffect.TryGetValue(cachedPlayerEntity.id, out pushOutForceGO))
            {
                pushOutForceGO.SetActive(true);
            }
            SetJoystick(true);
        });

        while (step == ETutorialSequenceStep.ShowGaugeDesc || step == ETutorialSequenceStep.MoveToShowGauge)
        {
            float gauge = cachedPlayerEntity.clientPushOutTick;
            if (gauge > PushOutForce.MAX_PUSHOUT_FORCE)
            {
                step = ETutorialSequenceStep.ShowPushoOutDesc;
            }
            yield return null;
        }
        
        SetJoystick(false);
        Entity aiEntity = null;
        float aiPosX = 0, aiPosY = 0;
        cachedMode.server.Enter("ai", 0, 1.3f);
        if (cachedMode.EntitiesDic.TryGetValue("ai", out aiEntity))
        {
            aiPosX = aiEntity.positionX;
            aiPosY = aiEntity.positionY;
        }
        NicknameHUD hud = null;
        if (cachedMode.CachedNicknameDic.TryGetValue(aiEntity.id, out hud))
        {
            hud.rank.gameObject.SetActive(false);
            hud.SetNickname("AI");
        }
        UIMessageBox.Instance.Show("게이지 안에 캐릭터가 들어오면 밀려납니다. 연습용 캐릭터를 밀어내세요", () =>
        {
            step = ETutorialSequenceStep.GenAI;
            SetJoystick(true);
        });

        while (step == ETutorialSequenceStep.ShowPushoOutDesc || step == ETutorialSequenceStep.GenAI)
        {
            if (0.1f < Vector2.Distance(new Vector2(aiPosX, aiPosY), new Vector2(aiEntity.positionX, aiEntity.positionY)))
            {
                step = ETutorialSequenceStep.ShowKill;
            }
            yield return null;
        }

        SetJoystick(false);
        yield return new WaitForSeconds(1.0f);
        cachedMode.server.Exit("ai");

        cachedMode.server.Enter("ai", 0, 1.3f);
        cachedMode.EntitiesDic.TryGetValue("ai", out aiEntity);
        hud = null;
        if (cachedMode.CachedNicknameDic.TryGetValue(aiEntity.id, out hud))
        {
            hud.rank.gameObject.SetActive(false);
            hud.SetNickname("AI");
        }
        UIMessageBox.Instance.Show("연습용 캐릭터를 밀어서 맵 아래로 떨어뜨리세요", () =>
        {
            step = ETutorialSequenceStep.KillAI;
            SetJoystick(true);
        });

        while (step == ETutorialSequenceStep.KillAI || step == ETutorialSequenceStep.ShowKill)
        {
            if (aiEntity.state == 2)
            {
                step = ETutorialSequenceStep.ShowExercise;
            }
            yield return null;
        }

        SetJoystick(false);
        yield return new WaitForSeconds(1.0f);
        cachedMode.server.Exit("ai");
        
        UIMessageBox.Instance.Show("이제 연습용 캐릭터가 움직입니다! 떨어뜨려서 튜토리얼을 완료하세요", () =>
        {
            step = ETutorialSequenceStep.Exercise;
            SetJoystick(true);
        });

        cachedMode.server.Enter("ai", 1.2f, 0);
        cachedMode.EntitiesDic.TryGetValue("ai", out aiEntity);
        hud = null;
        if (cachedMode.CachedNicknameDic.TryGetValue(aiEntity.id, out hud))
        {
            hud.rank.gameObject.SetActive(false);
            hud.SetNickname("AI");
        }

        ControllerBase controller = null;
        GameClient.Instance.ControllerManager.Get("ai").TryGetTarget(out controller);
        AIController aiController = controller as AIController;
        aiController.CacheTutorialModeResource(cachedMode.server);
        CachingPushOutEffect(aiEntity);
        while (step == ETutorialSequenceStep.ShowExercise || step == ETutorialSequenceStep.Exercise)
        {
            if(step == ETutorialSequenceStep.Exercise)
                aiController.Compute();

            if (aiEntity.state == 2)
            {
                step = ETutorialSequenceStep.End;
                PlayerPrefs.SetInt("Tutorial", 1);
            }
            yield return null;
        }

        SetJoystick(false);
        yield return new WaitForSeconds(0.5f);
        cachedMode.server.Exit("ai");

        UIMessageBox.Instance.Show("튜토리얼을 완료했습니다! 로비로 이동합니다", () =>
        {
            GameClient.Instance.StartGame(new LobbyMode());
        });
    }

    public void InputDirection(Vector2 direction)
    {
        if (cachedPlayerEntity.state == (int)EEntityState.Dead)
        {
            return;
        }

        if (Math.Abs(direction.x - prevDirection.x) > float.Epsilon || Math.Abs(direction.y - prevDirection.y) > float.Epsilon)
        {
            SendDirectionToServer(direction);
            prevDirection = direction;
        }
    }

    private void SendDirectionToServer(Vector2 direction)
    {
        float total = Math.Abs(direction.x) + Math.Abs(direction.y);
        if (total == 0) total = 1;

        float dirX = direction.x / total * 1000;
        float dirY = direction.y / total * 1000;

        cachedMode.server.Move(cachedPlayerEntity.id, dirX, dirY);
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
            return;
        }

        if (entity.state == (int)EEntityState.Dead)
        {
            WeakReference<Actor> actorRef = controller.GetControlActor();
            Actor actor = null;
            if (actorRef == null || !actorRef.TryGetTarget(out actor))
            {
                Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + entity.id);
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
        if (!attachedPushOutEffect.TryGetValue(entity.id, out pushOutEffect))
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
                    Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + entity.id);
                    return;
                }

                WeakReference<Actor> actorRef = controller.GetControlActor();
                Actor actor = null;
                if (actorRef == null || !actorRef.TryGetTarget(out actor))
                {
                    Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + entity.id);
                    return;
                }

                actor.ModelAnimator.SetTrigger("Attack");
            }
            pushOutEffect.transform.localScale = new Vector3(0, 0, 0);
            return;
        }

        if (entity.state == (int)EEntityState.Move)
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
        if (!entityDic.TryGetValue(pushOutForce.id, out entity))
        {
            return;
        }

        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(entity.id);
        ControllerBase controller = null;
        if (controllerRef == null || !controllerRef.TryGetTarget(out controller))
        {
            Debug.LogError("[PlayState]Controller doesn't exist! id : " + entity.id);
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

    public Entity SyncEntity(Entity dummyEntity)
    {
        this.dummyEntity.Sync(dummyEntity);
        this.dummyEntity.Adjust();
        Entity entity = null;
        string id = dummyEntity.id;
        if (!cachedMode.EntitiesDic.TryGetValue(id, out entity))
        {
            Debug.LogError("[PacketReceive]Entity is Null! id : " + id);
            return null;
        }

        EEntityState beforeState = (EEntityState)entity.state;
        EEntityState afterState = (EEntityState)dummyEntity.state;

        entity.Sync(this.dummyEntity);
        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(id);
        ControllerBase controller = null;
        if (controllerRef == null || !controllerRef.TryGetTarget(out controller))
        {
            Debug.LogError("[PacketReceive]Controller doesn't exist! id : " + id);
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
            return null;
        }

        if (beforeState == EEntityState.Idle && afterState == EEntityState.Move)
        {
            actor.ModelAnimator.SetBool("Move", true);
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

    private void CachingPushOutEffect(Entity entity)
    {
        if (entity == null)
            return;

        WeakReference<ControllerBase> controllerRef = GameClient.Instance.ControllerManager.Get(entity.id);
        ControllerBase controller = null;
        if (controllerRef == null || !controllerRef.TryGetTarget(out controller))
        {
            Debug.LogError("[PlayState]Controller doesn't exist! id : " + entity.id);
            return;
        }

        WeakReference<Actor> actorRef = controller.GetControlActor();
        Actor actor = null;
        if (actorRef == null || !actorRef.TryGetTarget(out actor))
        {
            Debug.LogError("[PlayState]Actor doesn't exist! id : " + entity.id);
            return;
        }

        GameObject pushOutEffect = cachedMode.PushOutEffectPool.Get();
        pushOutEffect.transform.SetParent(actor.transform);
        pushOutEffect.transform.localPosition = Vector3.zero;
        pushOutEffect.transform.localScale = Vector3.zero;

        attachedPushOutEffect.Add(entity.id, pushOutEffect);
    }

    private void SetJoystick(bool enable)
    {
        Joystick joystick = UIManager.Instance.Load("UI/Joystick") as Joystick;
        if (joystick != null)
        {
            joystick.gameObject.SetActive(enable);
            if(enable)
            {
                InputHelper.Instance.DirectionDelegate += InputDirection;
            }
            else
            {
                InputDirection(Vector2.zero);
                joystick.OnPointerUp(null);
                InputHelper.Instance.DirectionDelegate -= InputDirection;
            }
        }
    }
}
