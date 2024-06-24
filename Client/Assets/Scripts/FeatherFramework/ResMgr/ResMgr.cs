using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

public class ResMgr : Singleton<ResMgr>
{
    //缓存资源
    Dictionary<string, AsyncOperationHandle?> resCache = new Dictionary<string, AsyncOperationHandle?>();

    //同步加载资源
    public T Load<T>(string path) where T : UnityEngine.Object
    {
        var cacheHandle = TryGetResByCache(path);
        if (cacheHandle != null)
        {
            return (T)cacheHandle.Value.Result;
        }
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
        T res = handle.WaitForCompletion();
        if (handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded)
        {
            resCache.Add(path, handle);
            return res;
        }
        Debug.LogError("Load asset failed: " + path);
        return null;
    }

    //异步加载资源
    public void LoadAsync<T>(string path,UnityAction<T> callback) where T : UnityEngine.Object
    {
        var cacheHandle = TryGetResByCache(path);
        if (cacheHandle != null)
        {
            T res = (T)cacheHandle.Value.Result;
            callback?.Invoke(res);
            return;
        }
        MonoMgr.Instance.StartCoroutine(ReallyLoadAsync<T>(path, callback));
    }

    private IEnumerator ReallyLoadAsync<T>(string path, UnityAction<T> callback) where T : UnityEngine.Object
    {
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
        yield return handle;
        if (handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded)
        {
            callback?.Invoke(handle.Result);
        }
        Debug.LogError("Load asset failed: " + path);
    }

    //获取缓存
    AsyncOperationHandle? TryGetResByCache(string path)
    {
        if (resCache.ContainsKey(path))
        {
            return resCache[path];
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 释放指定资源
    /// </summary>
    void ReleaseRes(string path)
    {
        var cacheHandle = TryGetResByCache(path);
        if(cacheHandle!= null)
        {
            Addressables.Release(cacheHandle.Value);
            resCache.Remove(path);
        }
    }

    /// <summary>
    /// 释放所有资源
    /// </summary>
    public void ReleaseUnusedResources()
    {
        foreach (var item in resCache.Values)
        {
            Addressables.Release(item);
        }
        resCache.Clear();
        //AssetBundle.UnloadAllAssetBundles(true);
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }
}
