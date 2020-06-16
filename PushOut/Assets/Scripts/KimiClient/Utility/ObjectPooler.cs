using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class ObjectPooler<KeyType, Template> where Template : new()
{
    protected Dictionary<KeyType, Template> usePool;
    protected Queue<Template> unUsePool;

    public int Count { get { return usePool.Count + unUsePool.Count; } }
    public int ActiveCount { get { return usePool.Count; } }
    public int DeactiveCount { get { return unUsePool.Count; } }

    public void Initiallize(int initSize = 0)
    {
        usePool = new Dictionary<KeyType, Template>();
        unUsePool = new Queue<Template>();

        PreProcessInitiallize();

        for (int i = 0; i < initSize; i++)
        {
            CreateNewObject();
        }
    }

    protected virtual void PreProcessInitiallize() { }

    public Template Get(KeyType key)
    {
        if(unUsePool.Count == 0)
        {
            CreateNewObject();
        }

        Template getObject = unUsePool.Dequeue();
        PostProcessGet(getObject);
        usePool.Add(key, getObject);

        return getObject;
    }

    protected virtual void PostProcessGet(Template getObject) { }

    protected virtual void CreateNewObject()
    {
        Template newObject = new Template();
        unUsePool.Enqueue(newObject);
    }

    public void Return(KeyType key)
    {
        if (!usePool.ContainsKey(key))
        {
            Debug.LogError("Pool doesn't contain New Object!");
            return;
        }

        PreProcessReturn(usePool[key]);

        unUsePool.Enqueue(usePool[key]);
        usePool.Remove(key);
    }

    public void Return(Template value)
    {
        if (!usePool.ContainsValue(value))
        {
            Debug.LogError("Pool doesn't contain New Object!");
            return;
        }

        foreach(var node in usePool)
        {
            if(node.Value.Equals(value))
            {
                PreProcessReturn(value);

                unUsePool.Enqueue(value);
                usePool.Remove(node.Key);
                break;
            }
        }
    }

    protected virtual void PreProcessReturn(Template returnObject) { }

    public Template Find(KeyType key)
    {
        if(usePool.ContainsKey(key))
        {
            return usePool[key];
        }

        return default(Template);
    }

    public void Each(System.Action<Template> func)
    {
        foreach(var node in usePool)
        {
            func(node.Value);
        }
    }

    public void EachForUnUse(System.Action<Template> func)
    {
        foreach (var node in unUsePool)
        {
            func(node);
        }
    }

    public void EachForAll(System.Action<Template> func)
    {
        Each(func);
        EachForUnUse(func);
    }
    
    public void Refresh(System.Action<Template> func = null)
    {
        int poolSize = usePool.Count;
        foreach (var node in usePool)
        {
            unUsePool.Enqueue(node.Value);
        }

        if(func != null)
        {
            EachForUnUse(func);
        }

        usePool.Clear();
    }

    public virtual void Clear()
    {
        usePool.Clear();
        unUsePool.Clear();

    }
}
