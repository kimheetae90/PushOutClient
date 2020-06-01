using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;


[Serializable]
public class UIKillLogItem
{
    public GameObject go;
    public Text killer;
    public Text killed;
}

public class UIKillLog : UIObject
{
    public List<UIKillLogItem> killLogItemList;

    private int currentIndex = 0;
    private List<Coroutine> removeTimerList = new List<Coroutine>();
    
    public void Add(string killerName, string killedName)
    {
        if (currentIndex >= killLogItemList.Count)
        {
            Remove();
        }
        killLogItemList[currentIndex].go.SetActive(true);
        killLogItemList[currentIndex].killer.text = killerName;
        killLogItemList[currentIndex].killed.text = killedName;

        removeTimerList.Add(StartCoroutine(CoRemove()));

        currentIndex++;
    }

    IEnumerator CoRemove()
    {
        yield return new WaitForSeconds(3f);
        Remove();
    }
    
    public void Remove()
    {
        if (currentIndex <= 0)
            return;

        if (currentIndex >= killLogItemList.Count)
        {
            Coroutine co = removeTimerList[0];
            removeTimerList.RemoveAt(0);
            StopCoroutine(co);
        }

        for (int i = 0;i<currentIndex - 1;i++)
        {
            killLogItemList[i].killer.text = killLogItemList[i+1].killer.text;
            killLogItemList[i].killed.text = killLogItemList[i+1].killed.text;
        }

        killLogItemList[--currentIndex].go.SetActive(false);
    }
}
