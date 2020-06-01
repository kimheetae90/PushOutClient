using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UnityEngine;

public class POGameClient : GameClient
{
    private AndroidBackButtonListener androidBackButtonListner;

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
#if !QAMode
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
        androidBackButtonListner = gameObject.AddComponent<AndroidBackButtonListener>();
        androidBackButtonListner.SetAction(OnQuit);

        ResourceLoader.Instance.Load("UI/MessageBox");
    }

    private void OnQuit()
    {
        UIMessageBox.Instance.Show("그만할라구요?", () => {
            Server.Instance.Disconnect();
            Application.Quit();
        },
        () => {
            androidBackButtonListner.Regist();
        });
    }
}
