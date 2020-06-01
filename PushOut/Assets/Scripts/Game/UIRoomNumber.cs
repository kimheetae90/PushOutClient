using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class UIRoomNumber : UIObject
{
    public Text roomNum;

    public void SetRoomNumber(int roomNumber)
    {
        roomNum.text = roomNumber.ToString() + "번 방";
    }
}
