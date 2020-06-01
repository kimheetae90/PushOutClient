using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class UIAIModePanel : UIObject
{
    public void OnClickExit()
    {
        Server.Instance.Disconnect();
        Server.Instance.Emit("disconnect");
    }
}
