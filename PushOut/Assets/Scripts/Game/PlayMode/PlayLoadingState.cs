﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnitySocketIO.Events;

[Serializable]
public class RoomInfoPacket
{
    public int roomNum;
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

        ResourceLoader.Instance.Load("UI/ResultPopup");
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
        
        string roomNum = roomInfo.roomNum.ToString();
        var memberInfo = roomInfo.memberInfo;
        var nickname = roomInfo.nickname;

        int roomNumber;
        int.TryParse(roomNum, out roomNumber);
        cachedMode.RoomNumber = roomNumber;

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
