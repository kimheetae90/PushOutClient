using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    
    private Dictionary<string, UIObject> uiDic;

    private void Awake()
    {
        Instance = this;
        uiDic = new Dictionary<string, UIObject>();
    }

    public UIObject Load(string key)
    {
        UIObject loadUI = null;
        if (!uiDic.TryGetValue(key, out loadUI))
        {
            GameObject resource = ResourceLoader.Instance.GetResource<GameObject>(key);
            if (resource == null)
            {
                return null;
            }

            GameObject prefab = Instantiate(resource);
            if (prefab == null)
            {
                Debug.LogError("[UIManager]Resource doesn't Instantiateed! key : " + key);
                return null;
            }

            loadUI = prefab.GetComponent<UIObject>();
            if(loadUI == null)
            {
                Debug.LogError("[UIManager]Resource doesn't contain UIObject! key : " + key);
                return null;
            }

            loadUI.transform.SetParent(this.transform, false);
            uiDic.Add(key, loadUI);
        }

        return loadUI;
    }

    public void Unload(string key)
    {
        UIObject uiObject = null;
        if (uiDic.TryGetValue(key, out uiObject))
        {
            uiDic.Remove(key);
            DestroyImmediate(uiObject.gameObject);
        }

        ResourceLoader.Instance.Unload(key);
    }

    public UIObject Get(string key)
    {
        if(uiDic.ContainsKey(key))
        {
            return uiDic[key];
        }

        return null;
    }
}