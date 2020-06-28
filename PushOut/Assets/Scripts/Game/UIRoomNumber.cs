using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class UIRoomNumber : UIObject
{
    public Text roomNum;
    public Text password;

    public void SetRoomNumber(int roomNumber, int inpassword)
    {
        roomNum.text = "No. " + roomNumber.ToString();
        if(inpassword != -1)
        {
            password.text = "pw : " + inpassword.ToString();
            password.gameObject.SetActive(true);
        }
        else
        {
            password.gameObject.SetActive(false);
        }
    }
}
