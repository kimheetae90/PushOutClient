using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnitySocketIO.Events;


[Serializable]
public class ServerMonitorRoomInfoPacket
{
    public List<GameRoomInfo> roomInfo;
}

public class UIServerMonitor : UIObject
{
    public ServerMonitorRoomList roomList;
    public UILeaderBoard leaderBoard;
    public Text roomNumber;

    public void Awake()
    {
        Server.Instance.On("ServerMonitorRoomListS2C", ReceiveRoomInfo);
        Server.Instance.On("ServerMonitorRoomDetailInfoS2C", ReceiveRoomDetailInfo);
    }

    public void Start()
    {
        roomList.Set(new List<GameRoomInfo>());
        leaderBoard.Set(new List<Entity>());
    }

    public void OnClickRefresh()
    {
        Server.Instance.Emit("ServerMonitorRoomListC2S");
    }

    public void OnClickJoin()
    {

    }
    private void ReceiveRoomInfo(SocketIOEvent e)
    {
        ServerMonitorRoomInfoPacket roomInfo = JsonUtility.FromJson<ServerMonitorRoomInfoPacket>(e.data);
        roomList.Set(roomInfo.roomInfo);
    }
    private void ReceiveRoomDetailInfo(SocketIOEvent e)
    {
        RoomInfoPacket roomInfo = JsonUtility.FromJson<RoomInfoPacket>(e.data);
        roomNumber.text = roomInfo.roomNum.ToString();
        var memberInfo = roomInfo.memberInfo;
        var nickname = roomInfo.nickname;

        for(int i =0;i<memberInfo.Count;i++)
        {
            Entity entity = memberInfo[i];
            entity.nickName = nickname[i];
        }
        leaderBoard.Set(memberInfo);
    }

    public void OnDestroy()
    {
        Server.Instance.Off("ServerMonitorRoomListS2C", ReceiveRoomInfo);
        Server.Instance.Off("ServerMonitorRoomDetailInfoS2C", ReceiveRoomDetailInfo);
    }
}
