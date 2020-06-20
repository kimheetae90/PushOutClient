using UnityEngine;
using System;
using System.Collections.Generic;
using UnitySocketIO.Events;

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

    private Entity dummyEntity;
    private Vector2 prevDirection;
    
    private List<Entity> orderedBySpawnTimeEntityList;
    private List<Entity> deadEntityList;

    public override void Enter()
    {
        cachedMode = Base as PlayMode;

        InputHelper.Instance.DirectionDelegate += InputDirection;

        dummyEntity = new Entity();
        orderedBySpawnTimeEntityList = new List<Entity>();
        deadEntityList = new List<Entity>();

        Actor actor = cachedMode.ActorPool.Find(GameClient.Instance.UserInfo.UserID);
        CameraHelper.Instance.Monitor(actor.transform);

        Joystick joystick = UIManager.Instance.Load("UI/Joystick") as Joystick;
        if (joystick != null)
        {
            joystick.rootTransform.gameObject.SetActive(true);
        }

        Server.Instance.On("PlayerDeadS2C", ReceivePlayerDead);
        Server.Instance.On("RetryS2C", ReceiveRetry);
        Server.Instance.On("RetryKeepKillCountS2C", ReceiveRetry);

        UpdateLeaderBoard();

        if (cachedMode.EntitiesDic.Count > 1 && PlayerPrefs.GetInt("GuideForEnterSuper", 0) == 0)
        {
            UIMessageBox.Instance.Show("첫 진입시에는 움직이기 전까지 무적입니다.(광고시청시 동일효과)");
            PlayerPrefs.SetInt("GuideForEnterSuper", 1);
        }

        if (cachedMode.EntitiesDic.Count == 1)
        {
            cachedMode.StartAIMode();
        }
    }

    public override void Stay()
    {
        UpdateLeaderBoard();
    }
    
    public override void Exit()
    {
    }

    public override void Dispose()
    {
        Server.Instance.Off("PlayerDeadS2C", ReceivePlayerDead);
        Server.Instance.Off("RetryS2C", ReceiveRetry);
        Server.Instance.Off("RetryKeepKillCountS2C", ReceiveRetry);

        InputHelper.Instance.DirectionDelegate -= InputDirection;

        dummyEntity = null;
        orderedBySpawnTimeEntityList = null;
        deadEntityList = null;
    }

    private void ReceiveRetry(SocketIOEvent e)
    {
        Debug.Log("[PacketReceive]PlayLoading Player Enter received: " + e.name + " " + e.data);
        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]PlayerEnter Data is Null!");
            Server.Instance.Disconnect();
            return;
        }

        RetryS2CPacket packet = JsonUtility.FromJson<RetryS2CPacket>(e.data);
        
        UpdateLeaderBoard();

        if (GameClient.Instance.UserInfo.UserID.Equals(packet.player.id))
        {
            Joystick joystick = UIManager.Instance.Load("UI/Joystick") as Joystick;
            joystick.SetEnable(true);

            UIResultPopup resultPopup = UIManager.Instance.Load("UI/ResultPopup") as UIResultPopup;
            resultPopup.Hide();

            UILeaderBoard leaderBoard = UIManager.Instance.Load("UI/LeaderBoard") as UILeaderBoard;
            leaderBoard.Show();

            Actor actor = cachedMode.ActorPool.Find(packet.player.id);
            CameraHelper.Instance.Monitor(actor.transform);

            ADManager.Instance.ShowBanner();
        }
    }

    private void ReceivePlayerDead(SocketIOEvent e)
    {
        Debug.Log("[PacketReceive]PlayLoading Player Enter received: " + e.name + " " + e.data);
        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]PlayerEnter Data is Null!");
            Server.Instance.Disconnect();
            return;
        }

        PlayerDeadS2CPacket packet = JsonUtility.FromJson<PlayerDeadS2CPacket>(e.data);

        UpdateLeaderBoard();

        Entity killentity = null, entity = null;
        cachedMode.EntitiesDic.TryGetValue(packet.killEntityID, out killentity);
        cachedMode.EntitiesDic.TryGetValue(packet.id, out entity);
        KillLog((killentity == null) ? entity.nickName : killentity.nickName, entity.nickName);

        if(GameClient.Instance.UserInfo.UserID.Equals(packet.id))
        {
            Joystick joystick = UIManager.Instance.Load("UI/Joystick") as Joystick;
            joystick.SetEnable(false);

            UIResultPopup resultPopup = UIManager.Instance.Load("UI/ResultPopup") as UIResultPopup;
            resultPopup.SetLeaderBoard(orderedBySpawnTimeEntityList);
            resultPopup.SetADButton(!entity.useAD);
            resultPopup.Show();

            UILeaderBoard leaderBoard = UIManager.Instance.Load("UI/LeaderBoard") as UILeaderBoard;
            leaderBoard.Hide();

            CameraHelper.Instance.Freeze();

            ADManager.Instance.HideBanner();

            if (PlayerPrefs.GetInt("GuideForADSuper", 0) == 0)
            {
                UIMessageBox.Instance.Show("광고를 보면 킬 카운트가 유지되고 움직이기전까지 무적입니다");
                PlayerPrefs.SetInt("GuideForADSuper", 1);
            }
        }
    }

    public void InputDirection(Vector2 direction)
    {
        if(Math.Abs(direction.x - prevDirection.x) > float.Epsilon || Math.Abs(direction.y - prevDirection.y) > float.Epsilon)
        {
            SendDirectionToServer(direction);
            prevDirection = direction;
        }
    }

    private void SendDirectionToServer(Vector2 direction)
    {
        Entity entity = null;
        if (!cachedMode.EntitiesDic.TryGetValue(GameClient.Instance.UserInfo.UserID, out entity))
        {
            Debug.LogError("[SendDirectionToServer]My Entity Data is Null!");
            UIMessageBox.Instance.Show("에러가 발생해 로비로 이동합니다");
            Server.Instance.Disconnect();
            return;
        }

        float total = Math.Abs(direction.x) + Math.Abs(direction.y);
        if (total == 0)
        {
            float gauge = entity.clientPushOutTick;
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
            cachedMode.server.Move(entity.id, packet.directionX, packet.directionY);
        }
        else
        {
            Server.Instance.Emit("PlayerChangeMovementC2S", JsonUtility.ToJson(packet));
        }   
    }

    private void UpdateLeaderBoard()
    {
        if (cachedMode.AIMode)
            return;

        SortEntityKillCount(cachedMode.EntitiesDic, ref orderedBySpawnTimeEntityList, ref deadEntityList);

        UILeaderBoard leaderBoard = UIManager.Instance.Load("UI/LeaderBoard") as UILeaderBoard;
        if(leaderBoard == null)
            return;
         
        leaderBoard.Set(orderedBySpawnTimeEntityList);

        for (int index = 0; index < orderedBySpawnTimeEntityList.Count; index++)
        {
            Entity entity = orderedBySpawnTimeEntityList[index];
            NicknameHUD hud = cachedMode.NicknamePool.Find(entity.id);
            if(hud != null)
            {
                hud.SetRankTextActive(true);
                hud.SetRank(index + 1);
            }
        }

        foreach(var entity in deadEntityList)
        {
            NicknameHUD hud = cachedMode.NicknamePool.Find(entity.id); ;
            if (hud != null)
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
        UIKillLog killLog = UIManager.Instance.Load("UI/KillLog") as UIKillLog;
        if (killLog != null)
        {
            killLog.Add(killer, killed);
        }
    }
}
