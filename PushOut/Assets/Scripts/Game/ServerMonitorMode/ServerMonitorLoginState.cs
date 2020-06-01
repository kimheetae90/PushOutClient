using System;
using System.Collections.Generic;
using UnityEngine;
using UnitySocketIO.Events;

public class ServerMonitorLoginState : FSMState
{
    public override void Enter()
    {
        ResourceLoader.Instance.Load("ServerMonitor/ServerMonitor");
        UIServerMonitor serverMonitor = UIManager.Instance.Load("ServerMonitor/ServerMonitor") as UIServerMonitor;
        serverMonitor.Show();
    }
}
