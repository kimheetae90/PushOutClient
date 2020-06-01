using System.Collections.Generic;
using UnityEngine;

public class ServerMonitorRoomList : UIObject
{
    public ServerMonitorRoomButton prefab;
    public RectTransform itemRoot;

    public List<ServerMonitorRoomButton> itemList;

    public int CreateItemCount;

    private void Awake()
    {
        GameObject newObj; // Create GameObject instance
        itemList = new List<ServerMonitorRoomButton>();
        for (int i = 0; i < CreateItemCount; i++)
        {
            // Create new instances of our prefab until we've created as many as we specified
            newObj = Instantiate(prefab.gameObject, itemRoot) as GameObject;

            if (newObj == null)
            {
                continue;
            }

            ServerMonitorRoomButton newLeaderBoardItem = newObj.GetComponent<ServerMonitorRoomButton>();
            if (newLeaderBoardItem == null)
            {
                continue;
            }

            itemList.Add(newLeaderBoardItem);
        }
    }

    public void Set(List<GameRoomInfo> roomInfo)
    {
        int totalEntityCount = roomInfo.Count;
        for (int i = 0; i < itemList.Count; i++)
        {
            ServerMonitorRoomButton item = itemList[i];
            if (totalEntityCount <= i)
            {
                item.gameObject.SetActive(false);
            }
            else
            {
                item.gameObject.SetActive(true);
                GameRoomInfo room = roomInfo[i];
                item.Set(room.roomNum, room.memberCount, room.isPrivate);
            }
        }
    }
}
