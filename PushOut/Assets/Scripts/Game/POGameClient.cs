using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

public class POGameClient : GameClient
{
    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        PlayerPrefs.SetInt("ScreenAD", 0);

#if ServerMonitor
        StartGame(new ServerMonitorServerConnectMode());
#else
#if !DUMMY_CLIENT
        int tutorial = PlayerPrefs.GetInt("Tutorial", 0);
        if(tutorial == 0)
        {
            StartGame(new TutorialMode());
        }
        else
#endif
        {
            StartGame(new LobbyMode());
        }
#endif
      
        ResourceLoader.Instance.Load("UI/MessageBox");
    }
}
