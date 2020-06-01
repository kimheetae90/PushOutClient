using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerMonitorServerConnectMode : GameMode
{
    public override void Initiallize()
    {
        ModeName = "ServerMonitorServerConnectMode";

        AddState("ServerMonitorServerConnect", new ServerMonitorServerConnectState());
    }

    public override void Dispose()
    {
    }
}
