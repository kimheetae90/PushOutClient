using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerMonitorMode : GameMode
{
    public override void Initiallize()
    {
        ModeName = "ServerMonitorMode";

        AddState("ServerMonitorLogin", new ServerMonitorLoginState());
    }

    public override void Dispose()
    {
    }
}
