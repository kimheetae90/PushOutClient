using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class GameRoomInfo
{
    public int roomNum;
    public int memberCount;
    public bool isPrivate;
}

[Serializable]
public class RoomDetailInfoC2SPacket
{
    public int roomNum;
}

public class ServerMonitorRoomButton : UIObject
{
    public Text roomNum;
    public Text memberCount;
    public GameObject privateImage;

    private int roomNumber;

    public void Set(int inroomNum, int inmemberCount, bool isPrivate)
    {
        roomNumber = inroomNum;
        roomNum.text = inroomNum.ToString();
        privateImage.SetActive(isPrivate);
        memberCount.text = inmemberCount.ToString();
    }

    public void OnClick()
    {
        RoomDetailInfoC2SPacket packet = new RoomDetailInfoC2SPacket();
        packet.roomNum = roomNumber;
        Server.Instance.Emit("ServerMonitorRoomDetailInfoC2S", JsonUtility.ToJson(packet));
    }
}
