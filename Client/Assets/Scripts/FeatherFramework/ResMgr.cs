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
    List<string> isLoadRes = new List<string>(); //正在加载的资源(防止重复缓存)
    Dictionary<string, List<UnityAction<UnityEngine.Object>>> loadCallbacks = new Dictionary<string, List<UnityAction<UnityEngine.Object>>>(); //加载完回调

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
        if (!isLoadRes.Contains(path))
        {
            isLoadRes.Add(path);
            MonoMgr.Instance.StartCoroutine(ReallyLoadAsync<T>(path, callback));
        }
        else
        {
            UnityAction<UnityEngine.Object> action = (result) => callback((T)result);
            if (loadCallbacks.ContainsKey(path))
            {
                var tempListAction = loadCallbacks[path];
                tempListAction.Add(action);
                loadCallbacks[path] = tempListAction;
            }
            else
            {
                loadCallbacks.Add(path, new List<UnityAction<UnityEngine.Object>>() { action });
            }
        }
    }

    private IEnumerator ReallyLoadAsync<T>(string path, UnityAction<T> callback) where T : UnityEngine.Object
    {
        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(path);
        yield return handle;
        if (handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded)
        {
            resCache.Add(path, handle);
            callback?.Invoke(handle.Result);
            if (loadCallbacks.ContainsKey(path))
            {
                var tempListAction = loadCallbacks[path];
                foreach (var action in tempListAction)
                {
                    action(handle.Result);
                }
                loadCallbacks.Remove(path);
            }
        }
        else
        {
            Debug.LogError("Load asset failed: " + path);
        }
        isLoadRes.Remove(path);
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
    /// <param name="path">资源路径</param>
    /// <param name="immediately">是否立即释放缓存（不推荐）</param>
    public void ReleaseRes(string path,bool immediately = false)
    {
        var cacheHandle = TryGetResByCache(path);
        if(cacheHandle != null)
        {
            Addressables.Release(cacheHandle.Value);
            resCache.Remove(path);
            if (immediately)
            {
                Resources.UnloadUnusedAssets();
            }
        }
        else
        {
            Debug.LogError("Dont Exist This Resource");
        }
    }

    /// <summary>
    /// 释放没有被使用的资源
    /// </summary>
    public void ReleaseUnusedResources()
    {
        //AssetBundle.UnloadAllAssetBundles(true);
        Resources.UnloadUnusedAssets();
        GC.Collect();
    }
}
