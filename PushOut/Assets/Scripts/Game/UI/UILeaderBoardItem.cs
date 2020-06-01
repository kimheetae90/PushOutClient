using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class UILeaderBoardItem : UIObject
{
    public Text rank;
    public Text nickname;
    public Text time;

    public void Set(int inRank, string inNickname, int killCount)
    {
        rank.text = inRank.ToString();
        nickname.text = inNickname;
        time.text = killCount.ToString() + " Kill";
    }
}
