using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolMgr : Singleton<PoolMgr>
{
    public Dictionary<string, List<GameObject>> poolDic = new Dictionary<string, List<GameObject>>();

    public GameObject GetObj(string name)
    {
        GameObject obj = null;

        if (poolDic.ContainsKey(name) && poolDic[name].Count > 0)
        {
            obj = poolDic[name][0];
            poolDic[name].RemoveAt(0);
            obj.SetActive(true);
        }
        else
        {
            obj = GameObject.Instantiate(ResMgr.Instance.Load<GameObject>(name));
            obj.name = name;
        }
        return obj;
    }

    public void PushObj(string name,GameObject obj)
    {
        obj.SetActive(false);
        if (poolDic.ContainsKey(name))
        {
            poolDic[name].Add(obj);
        }
        else
        {
            poolDic.Add(name, new List<GameObject>() { obj });
        }
    }
}
