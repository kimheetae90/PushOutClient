using System;
using System.Collections.Generic;
using UnityEngine;
using UnitySocketIO.Events;

[Serializable]
public class RoomInfoPacket
{
    public int roomNum;
    public int password;
    public List<Entity> memberInfo;
    public List<string> nickname;
}

public class PlayLoadingState : FSMState
{
    private PlayMode cachedMode;

    public override void Enter()
    {
        Server.Instance.On("RoomInfoS2C", ReceiveRoomInfo);

        cachedMode = Base as PlayMode;
        cachedMode.ActorPool.Initiallize(10);
        cachedMode.PushOutEffectPool.Initiallize(10);
        cachedMode.NicknamePool.Initiallize(10);
    }

    private void ReceiveRoomInfo(SocketIOEvent e)
    {
        Debug.Log("[PacketReceive]PlayLoading Room Info received: " + e.name + " " + e.data);
        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]RoomInfo Data is Null!");
            Server.Instance.Disconnect();
            return;
        }

        RoomInfoPacket roomInfo = JsonUtility.FromJson<RoomInfoPacket>(e.data);

        cachedMode.RoomNumber = roomInfo.roomNum;
        cachedMode.Password = roomInfo.password;
        var memberInfo = roomInfo.memberInfo;
        var nickname = roomInfo.nickname;

        UIRoomNumber uiRoomNumber = UIManager.Instance.Load("UI/UIRoomNumber") as UIRoomNumber;
        uiRoomNumber.SetRoomNumber(cachedMode.RoomNumber, cachedMode.Password);

        int index = 0;
        foreach (var member in memberInfo)
        {
            cachedMode.CreatePlayer(member, nickname[index++]);
        }

        Change("Play");
    }
    
    public override void Dispose()
    {
        Server.Instance.Off("RoomInfoS2C", ReceiveRoomInfo);

        cachedMode = null;
    }
}
