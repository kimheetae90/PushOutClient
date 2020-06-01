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
        Server.Instance.On("PlayerEnterS2C", ReceivePlayerEnter);
        Server.Instance.On("PlayerExitS2C", ReceivePlayerExit);

        cachedMode = Base as PlayMode;
        cachedMode.ActorPool.Initiallize(5);
        cachedMode.PushOutEffectPool.Initiallize(5);
        cachedMode.NicknamePool.Initiallize(5);

        ResourceLoader.Instance.Load("Devil/devil");
        ResourceLoader.Instance.Load("Character/Prefabs/Boximon Chopper");
        ResourceLoader.Instance.Load("Character/Prefabs/Boximon Demon");
        ResourceLoader.Instance.Load("Character/Prefabs/Boximon Ghoul");
        ResourceLoader.Instance.Load("Character/Prefabs/Boximon Hellhound");
        ResourceLoader.Instance.Load("Character/Prefabs/Boximon Lava");
        ResourceLoader.Instance.Load("UI/ResultPopup");
    }

    private void LoginSuccess(SocketIOEvent e)
    {
        Server.Instance.Disconnect();
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

        Server.Instance.Off("PlayerEnterS2C", ReceivePlayerEnter);
        Server.Instance.Off("PlayerExitS2C", ReceivePlayerExit);
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

        cachedMode.CreatePlayer(player, nickname);
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

        cachedMode.RemovePlayer(player);
    }

    public override void Dispose()
    {
        Server.Instance.Off("RoomInfoS2C", ReceiveRoomInfo);

        cachedMode = null;
    }
}
