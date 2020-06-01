using System;
using System.Collections.Generic;
using UnityEngine;
using UnitySocketIO.Events;


[Serializable]
public class LoginS2CPacket
{
    public string id;
}

[Serializable]
public class ChangeNicknameC2SPacket
{
    public string Nickname;
}

[Serializable]
public class CheckVersionC2SPacket
{
    public string version;
}

[Serializable]
public class CheckVersionS2CPacket
{
    public bool res;
}

[Serializable]
public class CreatePrivateRoomC2SPacket
{
    public string password;
}


[Serializable]
public class EnterPrivateRoomC2SPacket
{
    public int roomNum;
    public string password;
}


public class LobbyState : FSMState
{
    public override void Enter()
    {
        Server.Instance.On("connectionS2C", LoginSuccess);
        Server.Instance.On("CheckVersionS2C", ReceiveCheckVersion);        
    }

    public override void Dispose()
    {
        Server.Instance.Off("connectionS2C", LoginSuccess);
        Server.Instance.Off("CheckVersionS2C", ReceiveCheckVersion);
        UIManager.Instance.Unload("UI/Lobby");
    }

    private void LoginSuccess(SocketIOEvent e)
    {
        Debug.Log("[PacketReceive]LobbyLoadingState Connection Success received: " + e.name + " " + e.data);
        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]connectionSuccess Data is Null!");
            return;
        }

        LoginS2CPacket packet = JsonUtility.FromJson<LoginS2CPacket>(e.data);

#if QAMode
        GameClient.Instance.ControllerManager.Add(packet.id, new AutoPlayerController());
#else
        GameClient.Instance.ControllerManager.Add(packet.id, new PlayerController());
#endif
        GameClient.Instance.ControllerManager.CurrentControllerID = packet.id;

        CheckVersionC2SPacket checkPacket = new CheckVersionC2SPacket();
        checkPacket.version = Application.version;
        Server.Instance.Emit("CheckVersionC2S", JsonUtility.ToJson(checkPacket));
    }

    private void ChangePlayMode()
    {
        ChangeNickName();
        GameClient.Instance.StartGame(new PlayMode());
    }

    private void ReceiveCheckVersion(SocketIOEvent e)
    {
        Debug.Log("[PacketReceive]LobbyLoadingState Connection Success received: " + e.name + " " + e.data);
        if (e.data == null)
        {
            Debug.LogError("[PacketReceive]connectionSuccess Data is Null!");
            return;
        }

        CheckVersionS2CPacket checkPacket = JsonUtility.FromJson<CheckVersionS2CPacket>(e.data);
        LobbyMode lobbyMode = Base as LobbyMode;
        UILobby lobbyUI = null;
        if (!lobbyMode.LobbyUI.TryGetTarget(out lobbyUI))
        {
            Debug.LogError("[LobbyState]Doesn't Exist UILobby");
            return;
        }
        
        if(!checkPacket.res)
        {
            ResourceLoader.Instance.Load("UI/Lobby");
            UIMessageBox.Instance.Show("앱을 업데이트 해주세요", ()=>
            {
                Application.Quit();
            });
            lobbyUI.Hide();
            return;
        }

        ChangeNickName();

        switch (lobbyMode.ClickButtonFlag)
        {
            case EGameButtonFlag.PublicJoin:
                Server.Instance.Emit("EnterRoomC2S");
                break;
            case EGameButtonFlag.PrivateCreate:
                CreatePrivateRoomC2SPacket createPacket = new CreatePrivateRoomC2SPacket();
                createPacket.password = lobbyUI.Password;
                Server.Instance.Emit("CreatePrivateRoomC2S", JsonUtility.ToJson(createPacket));
                break;
            case EGameButtonFlag.PrivateJoin:
                EnterPrivateRoomC2SPacket enterPacket = new EnterPrivateRoomC2SPacket();
                enterPacket.roomNum = lobbyUI.RoomNum;
                enterPacket.password = lobbyUI.Password;
                Server.Instance.Emit("EnterPrivateRoomC2S", JsonUtility.ToJson(enterPacket));
                break;
        }

        PlayMode playMode = new PlayMode();
        playMode.GameFlag = lobbyMode.ClickButtonFlag;
        GameClient.Instance.StartGame(playMode);
    }

    private void ChangeNickName()
    {
        LobbyMode lobbyMode = Base as LobbyMode;
        UILobby uiLobby = null;
        if (!lobbyMode.LobbyUI.TryGetTarget(out uiLobby))
        {
            Debug.LogError("[LobbyState]Lobby UI Doesn't Exist!");
            return;
        }

        if (!uiLobby.Nickname.Equals(string.Empty))
        {
            ChangeNicknameC2SPacket packet = new ChangeNicknameC2SPacket();
            packet.Nickname = uiLobby.Nickname;
            Server.Instance.Emit("ChangeNicknameC2S", JsonUtility.ToJson(packet));
        }
    }
}

