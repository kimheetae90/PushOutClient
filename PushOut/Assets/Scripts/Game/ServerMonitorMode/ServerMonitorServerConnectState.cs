using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySocketIO.Events;

public class ServerMonitorServerConnectState : FSMState
{
    public override void Enter()
    {
        Server.Instance.Initiallize();
        Server.Instance.On("connectionS2C", LoginSuccess);
        Server.Instance.Connect();
    }

    private void LoginSuccess(SocketIOEvent e)
    {
        if (e.data == null)
        {
            return;
        }

        GameClient.Instance.StartGame(new ServerMonitorMode());
    }


    public override void Dispose()
    {
        Server.Instance.Off("connectionS2C", LoginSuccess);
    }
}
