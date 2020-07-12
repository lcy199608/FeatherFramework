using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ResMgr : Singleton<ResMgr>
{
    //同步加载资源
    public T Load<T>(string name) where T:Object
    {
        T res = Resources.Load<T>(name);

        //如果是GameObject省去创建
        if (res is GameObject)
            return GameObject.Instantiate(res);
        else
            return res;
    }

    //异步加载资源
    public void LoadAsync<T>(string name,UnityAction<T> callback) where T : Object
    {
        MonoMgr.Instance.StartCoroutine(ReallyLoadAsync(name, callback));
    }

    //开启异步加载
    private IEnumerator ReallyLoadAsync<T>(string name, UnityAction<T> callback) where T: Object
    {
        ResourceRequest r = Resources.LoadAsync(name);
        yield return r;

        if (r.asset is GameObject)
            callback(GameObject.Instantiate(r.asset) as T);
        else
            callback(r.asset as T);
    }
}
