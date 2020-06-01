using System;
using System.Collections.Generic;
using UnityEngine;

public enum EGameButtonFlag
{
    PublicJoin,
    PrivateCreate,
    PrivateJoin,
}


public class LobbyMode : GameMode
{
    public EGameButtonFlag ClickButtonFlag { get; set; }
    public bool versionCheck = false;

    public WeakReference<UILobby> LobbyUI;

    public override void Initiallize()
    {
        ModeName = "LobbyMode";

        AddState("Loading", new LobbyLoadingState());
        AddState("Lobby", new LobbyState());
    }

    public override void Dispose()
    {
    }
}
