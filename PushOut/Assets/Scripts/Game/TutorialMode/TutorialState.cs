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
    private Vector2 prevDirection;

    private Entity dummyEntity;
    private List<PushOutForce> reserveRemovePushOutForceList;

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
        reserveRemovePushOutForceList = new List<PushOutForce>();
        
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
        reserveRemovePushOutForceList.Clear();
        reserveRemovePushOutForceList = null;
        GameClient.Instance.StopCoroutine(tutorialSequence); 
        cachedMode = null;
    }

    public override void Stay()
    {
    }

    private void OnPushOut(List<PushOutForce> pushOutForceList)
    {
        foreach (var node in pushOutForceList)
        {
            PushOutForce newPushOut = new PushOutForce();
            newPushOut.Copy(node);
            newPushOut.force *= 1000f;
            cachedMode.BattleSystemComponent.AddPushOutForce(newPushOut);
        }
    }

    private void OnDead(string deadID)
    {
        Actor actor = cachedMode.ActorPool.Find(deadID);
        if (actor == null)
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

        GameObject pushOutEffect = cachedMode.PushOutEffectPool.Find(deadID);
        if (pushOutEffect != null)
        {
            pushOutEffect.transform.localScale = Vector3.zero;
        }

        entity.state = (int)EEntityState.Dead;
        entity.directionX = 0.0f;
        entity.directionY = 0.0f;
        entity.killCount = 0;

        actor.ModelAnimator.SetBool("Dead", true);

        if (GameClient.Instance.UserInfo.UserID.Equals(deadID))
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

        Actor actor = cachedMode.ActorPool.Find(dummyEntity.id);
        if (actor == null)
        {
            Debug.LogError("[TUtorialState]Player Actor doesn't exist! Camera Setting Fail!");
            return;
        }

        actor.Height = 0;
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

            Actor actor = cachedMode.ActorPool.Find("player");
            if (actor == null)
            {
                Debug.LogError("[PlayState]Player Actor doesn't exist! Camera Setting Fail!");
                return;
            }

            CameraHelper.Instance.Monitor(actor.transform);
        });

        Entity cachedPlayerEntity = null;
        if (!cachedMode.EntitiesDic.TryGetValue("player", out cachedPlayerEntity))
        {
            Debug.LogError("[PlayState]Player Entity doesn't exist!");
            yield return null;
        }
        GameObject attachedPushOutEffect = cachedMode.PushOutEffectPool.Find("player");
        if (attachedPushOutEffect == null)
        {
            Debug.LogError("[PlayState]Player PushOutForceEffect doesn't exist!");
            yield return null;
        }

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
            attachedPushOutEffect.SetActive(false);
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
            attachedPushOutEffect.SetActive(true);
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
        string aiID = "ai";
        cachedMode.server.Enter(aiID, 0, 1.3f);
        if (cachedMode.EntitiesDic.TryGetValue(aiID, out aiEntity))
        {
            aiPosX = aiEntity.positionX;
            aiPosY = aiEntity.positionY;
        }
        NicknameHUD hud = cachedMode.NicknamePool.Find(aiID);
        if (hud != null)
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
        cachedMode.server.Exit(aiID);

        cachedMode.server.Enter(aiID, 0, 1.3f);
        cachedMode.EntitiesDic.TryGetValue(aiID, out aiEntity);
        hud = cachedMode.NicknamePool.Find(aiID);
        if (hud == null)
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
        cachedMode.server.Exit(aiID);
        
        UIMessageBox.Instance.Show("이제 연습용 캐릭터가 움직입니다! 떨어뜨려서 튜토리얼을 완료하세요", () =>
        {
            step = ETutorialSequenceStep.Exercise;
            SetJoystick(true);
        });

        cachedMode.server.Enter(aiID, 1.2f, 0);
        cachedMode.EntitiesDic.TryGetValue(aiID, out aiEntity);
        hud = cachedMode.NicknamePool.Find(aiID);
        if (hud != null)
        {
            hud.rank.gameObject.SetActive(false);
            hud.SetNickname("AI");
        }

        AIController aiController = new AIController();
        aiController.CacheTutorialModeResource(aiID, cachedMode.server);
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
        Entity cachedPlayerEntity = null;
        if (!cachedMode.EntitiesDic.TryGetValue("player", out cachedPlayerEntity))
        {
            return;
        }

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
        Entity cachedPlayerEntity = null;
        if (!cachedMode.EntitiesDic.TryGetValue("player", out cachedPlayerEntity))
        {
            return;
        }

        float total = Math.Abs(direction.x) + Math.Abs(direction.y);
        if (total == 0) total = 1;

        float dirX = direction.x / total * 1000;
        float dirY = direction.y / total * 1000;

        cachedMode.server.Move(cachedPlayerEntity.id, dirX, dirY);
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

        Actor actor = cachedMode.ActorPool.Find(id);
        actor.SetPosition(new Vector3(entity.positionX, actor.Height, entity.positionY));
        if (Math.Abs(entity.directionX) > float.Epsilon || Math.Abs(entity.directionY) > float.Epsilon)
        {
            actor.SetRotate(new Vector2(entity.directionX, entity.directionY));
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
