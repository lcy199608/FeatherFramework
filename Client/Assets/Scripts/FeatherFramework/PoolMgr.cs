using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

internal sealed class PoolData
{
    //抽屉中，对象挂载的父节点
    public GameObject fatherObj;
    //对象的容器
    public Stack<GameObject> poolList;

    public PoolData(GameObject obj, GameObject poolObj)
    {
        //根据obj创建一个同名父类空物体，它的父物体为总Pool空物体
        fatherObj = new GameObject(obj.name);
        fatherObj.transform.parent = poolObj.transform;

        poolList = new Stack<GameObject>();

        PushObj(obj);
    }

    //向抽屉里面压东西并且设置好父对象
    public void PushObj(GameObject obj)
    {
        //存起来
        poolList.Push(obj);
        //设置父对象
        obj.transform.SetParent(fatherObj.transform);
        //失活，让其隐藏
        obj.SetActive(false);
    }

    //向抽屉中取东西
    public GameObject TakeObj()
    {
        while (poolList.Count > 0)
        {
            var obj = poolList.Pop();
            if (obj != null) return obj;
        }
        return null;
    }
}

public sealed class PoolMgr
{
    internal readonly struct PoolKey : System.IEquatable<PoolKey>
    {
        public readonly string Name;
        private readonly GameObject template;

        public PoolKey(string address)
        {
            Name = address;
            template = null;
        }

        public PoolKey(GameObject template)
        {
            this.template = template;
            Name = "clone:" + template.GetInstanceID();
        }

        public bool Equals(PoolKey other) => Name == other.Name && ReferenceEquals(template, other.template);
        public override bool Equals(object obj) => obj is PoolKey other && Equals(other);
        public override int GetHashCode() => Name == null ? 0 : Name.GetHashCode();
    }

    private readonly ResMgr assets;
    private readonly Transform persistentRoot;

    internal PoolMgr(ResMgr assets, Transform persistentRoot)
    {
        this.assets = assets ?? throw new System.ArgumentNullException(nameof(assets));
        this.persistentRoot = persistentRoot ?? throw new System.ArgumentNullException(nameof(persistentRoot));
    }
    private readonly Dictionary<PoolKey, PoolData> pools = new Dictionary<PoolKey, PoolData>();

    private GameObject poolObj;
    private int generation;
    private bool stopped;
    private Transform staging;

    private void EnsureOpen() { if (stopped) throw new System.ObjectDisposedException(nameof(PoolMgr)); }

    internal void Shutdown()
    {
        if (stopped) return;
        stopped = true;
        Clear();
        if (staging != null) DestroyOwned(staging.gameObject);
        staging = null;
    }

    private static void DestroyOwned(GameObject value)
    {
        if (Application.isPlaying) GameObject.Destroy(value);
        else GameObject.DestroyImmediate(value);
    }

    private GameObject CreateInstance(GameObject template, PoolKey key, Transform layout)
    {
        if (staging == null)
        {
            var holder = new GameObject("PoolStaging");
            holder.SetActive(false);
            holder.transform.SetParent(persistentRoot, false);
            staging = holder.transform;
        }
        var instance = GameObject.Instantiate(template, staging, false);
        instance.SetActive(false);
        instance.name = template.name;
        EnsureToken(instance, key);
        PrepareSpawn(instance, layout);
        return instance;
    }

    //取得游戏物体
    public void GetObj(string name, UnityAction<GameObject> callback)
    {
        EnsureOpen();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new System.ArgumentException("Pool key cannot be empty.", nameof(name));
        }

        var key = new PoolKey(name);
        var pooledObject = pools.TryGetValue(key, out var pool) ? pool.TakeObj() : null;
        if (pooledObject != null)
        {
            //拖过委托返回给外部，让外部进行使用
            PrepareSpawn(pooledObject, null);
            callback?.Invoke(pooledObject);
        }
        else
        {
            //缓存池中没有该物体，我们去目录中加载
            //外面传一个预设体的路径和名字，我内部就去加载它
            int requestGeneration = generation;
            assets.LoadAsync<GameObject>(name, o => {
                if (requestGeneration != generation)
                {
                    callback?.Invoke(null);
                    return;
                }
                if (o == null)
                {
                    Debug.LogError($"Pool asset not found: {name}");
                    callback?.Invoke(null);
                    return;
                }

                var instance = CreateInstance(o, key, null);
                instance.name = name;
                callback?.Invoke(instance);
            });
        }
    }

    //外界返还游戏物体
    public void PushObj(GameObject obj)
    {
        if (obj == null)
        {
            throw new System.ArgumentNullException(nameof(obj));
        }

        var token = obj.GetComponent<PoolToken>();
        if (token == null || token.Owner != this || string.IsNullOrWhiteSpace(token.Key))
        {
            Debug.LogError($"Cannot return {obj.name} to the pool because it was not spawned by PoolMgr.");
            return;
        }

        if (stopped) { DestroyOwned(obj); return; }

        if (poolObj == null)
        {
            poolObj = new GameObject("ObjPool");
            poolObj.transform.SetParent(persistentRoot, false);

        }
        //里面有记录这个键
        if (pools.TryGetValue(token.Identity, out var pool))
        {
            if (pool.poolList.Contains(obj))
            {
                Debug.LogError($"Object {obj.name} was returned to the pool more than once.");
                return;
            }
            pool.PushObj(obj);
        }
        //未曾记录这个键
        else
        {
            pools.Add(token.Identity, new PoolData(obj, poolObj));
        }
    }

    //通过传入GameObject取得游戏物体
    public void GetCloneObj(GameObject obj, UnityAction<GameObject> callback)
    {
        EnsureOpen();
        if (obj == null)
        {
            throw new System.ArgumentNullException(nameof(obj));
        }

        var token = obj.GetComponent<PoolToken>();
        var key = token != null && token.Owner == this ? token.Identity : new PoolKey(obj);
        var pooledObject = pools.TryGetValue(key, out var pool) ? pool.TakeObj() : null;
        if (pooledObject != null)
        {
            //拖过委托返回给外部，让外部进行使用
            var o = pooledObject;
            PrepareSpawn(o, obj.transform);
            callback?.Invoke(o);
        }
        else
        {
            var o = CreateInstance(obj, key, obj.transform);
            callback?.Invoke(o);
        }
    }

    //清空缓存池的方法
    //主要用在场景切换时
    public void Clear()
    {
        generation += 1;
        // Active instances are owned by their callers and remain valid across a pool clear.
        pools.Clear();
        if (poolObj != null)
        {
            DestroyOwned(poolObj);
            poolObj = null;
        }
    }

    private void EnsureToken(GameObject instance, PoolKey key)
    {
        var token = instance.GetComponent<PoolToken>();
        if (token == null)
        {
            token = instance.AddComponent<PoolToken>();
        }
        token.Initialize(this, key);
    }

    private static void PrepareSpawn(GameObject instance, Transform template)
    {
        Transform parent = template == null ? null : template.parent;
        instance.transform.SetParent(parent, false);
        if (template == null)
        {
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
        }
        else
        {
            instance.transform.localPosition = template.localPosition;
            instance.transform.localRotation = template.localRotation;
            instance.transform.localScale = template.localScale;
        }
        instance.SetActive(true);
    }
}
