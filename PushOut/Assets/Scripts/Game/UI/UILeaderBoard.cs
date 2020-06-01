using System;
using System.Collections.Generic;
using UnityEngine;

public class UILeaderBoard : UIObject
{
    public UILeaderBoardItem prefab;
    public RectTransform itemRoot;

    public List<UILeaderBoardItem> itemList;

    public int CreateItemCount;

    private void Awake()
    {
        GameObject newObj; // Create GameObject instance
        itemList = new List<UILeaderBoardItem>();
        for (int i = 0; i < CreateItemCount; i++)
        {
            // Create new instances of our prefab until we've created as many as we specified
            newObj = Instantiate(prefab.gameObject, itemRoot) as GameObject;

            if(newObj == null)
            {
                Debug.LogError("[UILearderBoard]Init Child Fail!");
                continue;
            }

            UILeaderBoardItem newLeaderBoardItem = newObj.GetComponent<UILeaderBoardItem>();
            if (newLeaderBoardItem == null)
            {
                Debug.LogError("[UILearderBoard]Child doesn't contain UILeaderBoardItem Component");
                continue;
            }

            itemList.Add(newLeaderBoardItem);
        }
    }

    public void Set(List<Entity> orderedBySpawnTimeEntityList)
    {
        var now = DateTime.Now.ToLocalTime();
        var span = (now - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime());
        long currentTime = (long)span.TotalMilliseconds;

        int totalEntityCount = orderedBySpawnTimeEntityList.Count;
        for (int i = 0; i < itemList.Count; i++)
        {
            UILeaderBoardItem item = itemList[i];
            if (totalEntityCount <= i)
            {
                item.gameObject.SetActive(false);
            }
            else
            {
                item.gameObject.SetActive(true);
                Entity entity = orderedBySpawnTimeEntityList[i];
                item.Set(i + 1, entity.nickName, entity.killCount);
            }
        }
    }
}
