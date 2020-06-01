using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class UILobby : UIObject
{
    public int VisitStackToShowScreenAD = 4;

    public string Nickname { get; private set; }

    public string Password { get; private set; }
    public int RoomNum  { get; private set; }

    public GameObject CreatePanel;
    public GameObject JoinPanel;

    private void Awake()
    {
        Nickname = string.Empty;
        OnShow += CountScreenAD;
        RoomNum = -1;
    }

    public void CountScreenAD()
    {
        int count = PlayerPrefs.GetInt("ScreenAD", 0);
        count++;

        PlayerPrefs.SetInt("ScreenAD", count);

        if (count >= VisitStackToShowScreenAD)
        {
            ADManager.Instance.ShowScreen();
            PlayerPrefs.SetInt("ScreenAD", 0);
        }
    }

    public void SetNickname(Text nickName)
    {
        Nickname = nickName.text;
    }

    private void StartGame()
    {
        Server.Instance.Connect();
    }

    public void OnClickCreatePrivateRoom()
    {
        CreatePanel.SetActive(true);
    }

    public void OnClickCreatePrivateRoomOk()
    {
        OnClickCreatePrivateRoomCancel();

        if (Password.Length == 0)
            return;

        LobbyMode mode = POGameClient.Instance.Game as LobbyMode;
        mode.ClickButtonFlag = EGameButtonFlag.PrivateCreate;
        StartGame();
    }

    public void OnClickCreatePrivateRoomCancel()
    {
        CreatePanel.SetActive(false);
    }

    public void OnClickJoinPrivateRoom()
    {
        JoinPanel.SetActive(true);
    }

    public void OnClickJoinPrivateRoomOk()
    {
        OnClickJoinPrivateRoomCancel();

        if (Password.Length == 0 || RoomNum < 0)
            return;

        LobbyMode mode = POGameClient.Instance.Game as LobbyMode;
        mode.ClickButtonFlag = EGameButtonFlag.PrivateJoin;
        StartGame();
    }

    public void OnClickJoinPrivateRoomCancel()
    {
        JoinPanel.SetActive(false);
    }

    public void SetRoomNum(Text roomNum)
    {
        int num = -1;
        if(int.TryParse(roomNum.text, out num))
        {
            RoomNum = num;
        }
    }

    public void SetPassword(Text passwordText)
    {
        Password = passwordText.text;
    }

    public void OnClickJoinPublicRoom()
    {
        LobbyMode mode = POGameClient.Instance.Game as LobbyMode;
        mode.ClickButtonFlag = EGameButtonFlag.PublicJoin;
        StartGame();
    }
}
