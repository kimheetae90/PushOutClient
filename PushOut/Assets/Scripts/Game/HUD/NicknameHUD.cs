using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NicknameHUD : MonoBehaviour
{
    public TextMesh nickname;
    public TextMesh rank;

    public void SetNickname(string name)
    {
        nickname.text = name;
    }

    public void SetRankTextActive(bool isActive)
    {
        rank.gameObject.SetActive(isActive);
    }

    public void SetRank(int inRank)
    {
        rank.text = inRank.ToString();
    }
}
