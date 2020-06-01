using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler<Template> where Template : new()
{
    protected List<Template> usePool;
    protected Queue<Template> unUsePool;

    public int Count { get { return usePool.Count + unUsePool.Count; } }
    public int ActiveCount { get { return usePool.Count; } }
    public int DeactiveCount { get { return unUsePool.Count; } }

    public void Initiallize(int initSize = 0)
    {
        usePool = new List<Template>();
        unUsePool = new Queue<Template>();

        PreProcessInitiallize();

        for (int i = 0; i < initSize; i++)
        {
            CreateNewObject();
        }
    }

    protected virtual void PreProcessInitiallize() { }

    public Template Get()
    {
        if(unUsePool.Count == 0)
        {
            CreateNewObject();
        }

        Template getObject = unUsePool.Dequeue();
        PostProcessGet(getObject);
        usePool.Add(getObject);

        return getObject;
    }

    protected virtual void PostProcessGet(Template getObject) { }

    protected virtual void CreateNewObject()
    {
        Template newObject = new Template();
        unUsePool.Enqueue(newObject);
    }

    public void Add(Template newObject)
    {
        if(!usePool.Contains(newObject))
        {
            Debug.LogError("Pool doesn't contain New Object!");
            return;
        }

        if(unUsePool.Contains(newObject))
        {
            Debug.LogError("Pool already contain New Object!");
            return;
        }

        unUsePool.Enqueue(newObject);
    }

    public void Return(Template returnObject)
    {
        if (!usePool.Contains(returnObject))
        {
            Debug.LogError("Pool doesn't contain New Object!");
            return;
        }

        if (unUsePool.Contains(returnObject))
        {
            Debug.LogError("Pool already contain New Object!");
            return;
        }

        PreProcessReturn(returnObject);

        usePool.Remove(returnObject);
        unUsePool.Enqueue(returnObject);
    }

    protected virtual void PreProcessReturn(Template returnObject) { }

    public void Each(System.Action<Template> func)
    {
        foreach(var node in usePool)
        {
            func(node);
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
        for(int i=0;i<poolSize;i++)
        {
            unUsePool.Enqueue(usePool[i]);
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
