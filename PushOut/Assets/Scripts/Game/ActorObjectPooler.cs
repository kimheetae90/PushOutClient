using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorObjectPooler : ObjectPooler<Actor>
{
    private GameObject prefab;

    protected override void PreProcessInitiallize()
    {
        ResourceLoader.Instance.Load("Actor");
        prefab = ResourceLoader.Instance.Get("Actor") as GameObject;
    }

    protected override void CreateNewObject()
    {
        if(prefab == null)
        {
            Debug.LogError("[ActorObjectPooler]Prefab doesn't Loaded!");
            return;
        }

        GameObject actorGameObject = Object.Instantiate(prefab) as GameObject;
        if (actorGameObject == null)
        {
            Debug.LogError("[ActorObjectPooler]Prefab Instantiate Fail!");
            return;
        }

        actorGameObject.SetActive(false);
        Actor newObject = actorGameObject.GetComponent<Actor>();

        if (newObject == null)
        {
            Debug.LogError("[ActorObjectPooler]Actor doesn't contain Actor Component!");
            return;
        }

        unUsePool.Enqueue(newObject);
    }

    protected override void PostProcessGet(Actor getObject)
    {
        if(getObject != null)
        {
            getObject.gameObject.SetActive(true);
        }
    }

    protected override void PreProcessReturn(Actor returnObject)
    {
        if(returnObject != null)
        {
            returnObject.transform.position = Vector3.zero;
            returnObject.transform.rotation = Quaternion.identity;
            returnObject.transform.localScale = new Vector3(1, 1, 1);
            returnObject.gameObject.SetActive(false);
        }
    }

    public override void Clear()
    {
        EachForAll((actor) => { Object.DestroyImmediate(actor.gameObject); });
        base.Clear();

        ResourceLoader.Instance.Unload("Actor");
    }
}
