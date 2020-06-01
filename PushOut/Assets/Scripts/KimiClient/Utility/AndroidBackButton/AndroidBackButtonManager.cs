using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AndroidBackButtonManager : MonoBehaviour
{
    public static AndroidBackButtonManager Instance;
    private List<AndroidBackButtonListener> listnerList;

    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;
        listnerList = new List<AndroidBackButtonListener>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(listnerList.Count > 0)
            {
                foreach(var listner in listnerList)
                {
                    listnerList.Remove(listner);
                    if (listner != null)
                    {
                        listner.OnAndroidBackButton();
                        break;
                    }
                }
            }
        }
    }

    public void Regist(AndroidBackButtonListener listner)
    {
        listnerList.Insert(0,listner);
    }
}
