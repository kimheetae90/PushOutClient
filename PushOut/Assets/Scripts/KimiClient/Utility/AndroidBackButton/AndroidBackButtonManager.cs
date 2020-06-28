using System.Collections.Generic;
using UnityEngine;

public class AndroidBackButtonManager : MonoBehaviour
{
    public static AndroidBackButtonManager Instance;
    private List<AndroidBackButtonListener> listnerList;
    private List<AndroidBackButtonListener> panddingList;

    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;
        listnerList = new List<AndroidBackButtonListener>();
        panddingList = new List<AndroidBackButtonListener>();
    }

    // Update is called once per frame
    void Update()
    {
        if(ADManager.Instance.IsRunningRewardAD)
        {
            return;
        }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(listnerList.Count > 0)
            {
                AndroidBackButtonListener listner = null;
                foreach (var node in listnerList)
                {
                    if(node == null)
                    {
                        panddingList.Add(node);
                    }
                    else
                    {
                        if (node != null && node.OnAndroidBackButton())
                        {
                            panddingList.Add(listner);
                            break;
                        }
                    }
                }

                foreach (var node in panddingList)
                {
                    if (listnerList.Contains(node))
                        listnerList.Remove(node);
                }

                panddingList.Clear();
            }
            else
            {
                UIMessageBox.Instance.Show("그만할라구요?", () =>
                {
                    Server.Instance.Disconnect();
                    Application.Quit();
                },() =>{});
            }
        }
    }

    public void Regist(AndroidBackButtonListener listner)
    {
        if(listnerList.Contains(listner))
        {
            listnerList.Remove(listner);
        }

        listnerList.Insert(0, listner);
    }

    public void Remove(AndroidBackButtonListener listner)
    {
        if (listnerList.Contains(listner))
            listnerList.Remove(listner);
    }
}
