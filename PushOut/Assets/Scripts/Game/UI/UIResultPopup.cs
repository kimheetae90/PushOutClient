using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIResultPopup : UIObject
{
    public UILeaderBoard leaderBoard;
    public GameObject adButton;    

    private void Awake()
    {
        androidBackButtonListner.SetAction(OnBackButtonExit);
    }

    public void SetLeaderBoard(List<Entity> orderedBySpawnTimeEntityList)
    {
        List<Entity> copyOrder = new List<Entity>();
        foreach(var entity in orderedBySpawnTimeEntityList)
        {
            copyOrder.Add(entity);
        }
        leaderBoard.Set(copyOrder);
        copyOrder.Clear();
    }
    
    public void SetADButton(bool flag)
    {
        if(adButton != null)
        {
            adButton.SetActive(flag);
        }
    }

    public void OnRetryButton()
    {
        Server.Instance.Emit("RetryC2S");
    }

    public bool OnBackButtonExit()
    {
        if (!this.gameObject.activeSelf)
            return false;

        OnExitButton();
        return true;
    }

    public void OnExitButton()
    {
        Server.Instance.Disconnect();
        Server.Instance.Emit("disconnect");
    }

    public void OnADButton()
    {
        ADManager.Instance.ShowReward(
            SendRetryKeepKillCount, SendRetryKeepKillCount, SendRetry
            );
    }

    private void SendRetryKeepKillCount()
    {
        Server.Instance.Emit("RetryKeepKillCountC2S");
    }

    private void SendRetry()
    {
        Server.Instance.Emit("RetryC2S");
    }
}
