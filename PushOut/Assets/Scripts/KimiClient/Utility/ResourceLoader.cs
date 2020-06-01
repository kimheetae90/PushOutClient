using System.Collections.Generic;
using UnityEngine;

public class ResourceLoader : MonoBehaviour
{
    public static ResourceLoader Instance;

    public Dictionary<string, Object> cachedResourceDic;

    private void Awake()
    {
        Install();
    }

    public void Install()
    {
        Instance = this;
        cachedResourceDic = new Dictionary<string, Object>();
    }

    public ResourceType GetResource<ResourceType>(string key) where ResourceType : class
    {
        Object resource = Get(key);

        if(resource == null)
        {
            if(Load(key))
            {
                resource = Get(key);
            }
            else
            {
                return default(ResourceType);
            }
        }

        return resource as ResourceType;
    }

    public bool Load(string key)
    {
        if(cachedResourceDic.ContainsKey(key))
        {
            Debug.LogWarning("Already contain Resource! key : " + key);
            return false;
        }

        Object resource = Resources.Load(key);
        cachedResourceDic.Add(key, resource);
        return true;
    }

    public bool Unload(string key)
    {
        Object resource = Get(key);
        if (resource == null)
            return false;

        cachedResourceDic.Remove(key);
        Resources.UnloadUnusedAssets();
        return true;
    }

    public Object Get(string key, bool useDebug = true)
    {
        Object resource = null;
        if(!cachedResourceDic.TryGetValue(key, out resource) && useDebug)
        {
            Debug.LogWarning("Doesn't contain Resource key : " + key);
        }

        return resource;
    }

}
