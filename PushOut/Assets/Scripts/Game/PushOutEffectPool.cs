using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PushOutEffectPool : ObjectPooler<GameObject>
{
    private GameObject prefab;

    protected override void PreProcessInitiallize()
    {
        ResourceLoader.Instance.Load("Effect/PushOutForce/PushOutForceEffect");
        prefab = ResourceLoader.Instance.Get("Effect/PushOutForce/PushOutForceEffect") as GameObject;
    }

    protected override void CreateNewObject()
    {
        if (prefab == null)
        {
            Debug.LogError("[PushOutEffectPooler]Prefab doesn't Loaded!");
            return;
        }

        GameObject actorGameObject = Object.Instantiate(prefab) as GameObject;
        if (actorGameObject == null)
        {
            Debug.LogError("[PushOutEffectPooler]Prefab Instantiate Fail!");
            return;
        }
        
        actorGameObject.SetActive(false);

        unUsePool.Enqueue(actorGameObject);
    }

    protected override void PostProcessGet(GameObject getObject)
    {
        if (getObject != null)
        {
            getObject.gameObject.SetActive(true);
        }
    }

    protected override void PreProcessReturn(GameObject returnObject)
    {
        if (returnObject != null)
        {
            returnObject.transform.SetParent(null);
            returnObject.gameObject.SetActive(false);
        }
    }

    public override void Clear()
    {
        EachForAll((effectObject) => { Object.DestroyImmediate(effectObject); });
        base.Clear();

        ResourceLoader.Instance.Unload("Effect/PushOutForce/PushOutForceEffect");
    }
}