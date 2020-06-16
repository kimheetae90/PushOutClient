using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

public class LobbyLoadingState : FSMState
{
    public override void Enter()
    {
        LobbyMode lobbyMode = Base as LobbyMode;
        lobbyMode.versionCheck = false;

        Server.Instance.Initiallize();

        UILobby lobby = UIManager.Instance.Load("UI/Lobby") as UILobby;
        lobbyMode.LobbyUI = new WeakReference<UILobby>(lobby);        
        lobby.Show();

        CameraHelper.Instance.currentCamera.transform.localPosition = new UnityEngine.Vector3(1000, 1000, 1000);
        CameraHelper.Instance.currentCamera.transform.rotation = UnityEngine.Quaternion.identity;

        Change("Lobby");
    }
}
