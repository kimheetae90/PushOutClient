using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NicknameObjectPool : ObjectPooler<string, NicknameHUD>
{
    private GameObject prefab;
    
    protected override void PreProcessInitiallize()
    {
        ResourceLoader.Instance.Load("Nickname");
        prefab = ResourceLoader.Instance.Get("Nickname") as GameObject;
    }

    protected override void CreateNewObject()
    {
        if (prefab == null)
        {
            Debug.LogError("[NicknameObjectPool]Prefab doesn't Loaded!");
            return;
        }

        GameObject nicknameGameObject = Object.Instantiate(prefab) as GameObject;
        if (nicknameGameObject == null)
        {
            Debug.LogError("[NicknameObjectPool]Prefab Instantiate Fail!");
            return;
        }

        nicknameGameObject.SetActive(false);
        NicknameHUD nicknameHUD = nicknameGameObject.GetComponent<NicknameHUD>();

        if (nicknameHUD == null)
        {
            Debug.LogError("[NicknameObjectPool]Actor doesn't contain Actor Component!");
            return;
        }

        unUsePool.Enqueue(nicknameHUD);
    }

    protected override void PostProcessGet(NicknameHUD getObject)
    {
        if (getObject != null)
        {
            getObject.gameObject.SetActive(true);
        }
    }

    protected override void PreProcessReturn(NicknameHUD returnObject)
    {
        if (returnObject != null)
        {
            returnObject.transform.SetParent(null);
            returnObject.transform.position = Vector3.zero;
            returnObject.transform.rotation = Quaternion.identity;
            returnObject.transform.localScale = new Vector3(1,1,1);
            returnObject.gameObject.SetActive(false);
        }
    }

    public override void Clear()
    {
        EachForAll((nickname) => { Object.DestroyImmediate(nickname.gameObject); });
        base.Clear();

        ResourceLoader.Instance.Unload("Nickname");
    }
}
