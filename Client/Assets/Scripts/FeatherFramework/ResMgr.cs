using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class ResMgr
{
    private readonly struct AssetKey : IEquatable<AssetKey>
    {
        public readonly string Address;
        public readonly Type Type;

        public AssetKey(string address, Type type)
        {
            Address = address;
            Type = type;
        }

        public bool Equals(AssetKey other) => Address == other.Address && Type == other.Type;
        public override bool Equals(object obj) => obj is AssetKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Address, Type);
    }

    private sealed class LoadRequest
    {
        public AsyncOperationHandle Handle;
        public readonly List<Action<UnityEngine.Object>> Callbacks = new List<Action<UnityEngine.Object>>();
        public bool Completed;
        public bool Cancelled;
    }

    private readonly MonoBehaviour coroutineRunner;
    private readonly Dictionary<AssetKey, AsyncOperationHandle> cache = new Dictionary<AssetKey, AsyncOperationHandle>();
    private readonly Dictionary<AssetKey, LoadRequest> activeRequests = new Dictionary<AssetKey, LoadRequest>();
    private readonly Dictionary<AssetKey, AssetScope> owners = new Dictionary<AssetKey, AssetScope>();

    // An opt-in exclusive lifetime for level-local assets. Shared framework assets use the normal cache.
    private bool stopped;
    private void EnsureOpen() { if (stopped) throw new ObjectDisposedException(nameof(ResMgr)); }
    public AssetScope CreateScope() { EnsureOpen(); return new AssetScope(this); }
    internal void Shutdown() { if (stopped) return; stopped = true; ReleaseAll(); }

    public sealed class AssetScope : IDisposable
    {
        private readonly ResMgr assets;
        private readonly Dictionary<AssetKey, Action> releases = new Dictionary<AssetKey, Action>();
        private bool disposed;
        internal AssetScope(ResMgr assets) { this.assets = assets; }

        public void LoadAsync<T>(string address, UnityAction<T> callback) where T : UnityEngine.Object
        {
            if (disposed) throw new ObjectDisposedException(nameof(AssetScope));
            assets.EnsureOpen();
            ValidateAddress(address);
            var key = new AssetKey(address, typeof(T));
            if (!releases.ContainsKey(key))
            {
                if (assets.owners.ContainsKey(key) || assets.cache.ContainsKey(key) || assets.activeRequests.ContainsKey(key))
                    throw new InvalidOperationException($"Asset '{address}' already has another owner; use its existing owner for sharing.");
                assets.owners.Add(key, this);
                releases.Add(key, () => assets.ReleaseRes<T>(address));
            }
            assets.LoadAsyncCore<T>(address, value => { if (!disposed) callback?.Invoke(value); }, this);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var pair in releases)
            {
                assets.owners.Remove(pair.Key);
                pair.Value();
            }
            releases.Clear();
        }
    }

    private void CheckOwner(AssetKey key, AssetScope scope = null)
    {
        if (owners.TryGetValue(key, out var owner) && owner != scope)
            throw new InvalidOperationException($"Asset '{key.Address}' belongs to an AssetScope; access and release it through that owner.");
    }

    internal ResMgr(MonoBehaviour coroutineRunner)
    {
        this.coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));
    }

    public T Load<T>(string address) where T : UnityEngine.Object
    {
        EnsureOpen();
        ValidateAddress(address);
        var key = new AssetKey(address, typeof(T));
        CheckOwner(key);
        if (cache.TryGetValue(key, out var cached))
        {
            return cached.Result as T;
        }

        if (activeRequests.TryGetValue(key, out var activeRequest))
        {
            activeRequest.Handle.WaitForCompletion();
            T result = activeRequest.Handle.Status == AsyncOperationStatus.Succeeded
                ? activeRequest.Handle.Result as T
                : null;
            CompleteRequest(address, key, activeRequest);
            return result;
        }

        var request = new LoadRequest
        {
            Handle = Addressables.LoadAssetAsync<T>(address)
        };
        activeRequests.Add(key, request);
        request.Handle.WaitForCompletion();
        T loadedAsset = request.Handle.Status == AsyncOperationStatus.Succeeded
            ? request.Handle.Result as T
            : null;
        CompleteRequest(address, key, request);
        return loadedAsset;
    }

    public void LoadAsync<T>(string address, UnityAction<T> callback) where T : UnityEngine.Object
        => LoadAsyncCore(address, callback, null);

    private void LoadAsyncCore<T>(string address, UnityAction<T> callback, AssetScope scope) where T : UnityEngine.Object
    {
        EnsureOpen();
        ValidateAddress(address);
        var key = new AssetKey(address, typeof(T));
        CheckOwner(key, scope);
        if (cache.TryGetValue(key, out var cached))
        {
            try
            {
                callback?.Invoke(cached.Result as T);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            return;
        }

        if (!activeRequests.TryGetValue(key, out var request))
        {
            request = new LoadRequest
            {
                Handle = Addressables.LoadAssetAsync<T>(address)
            };
            request.Callbacks.Add(result => callback?.Invoke(result as T));
            activeRequests.Add(key, request);
            coroutineRunner.StartCoroutine(LoadAndNotify<T>(address, key, request));
            return;
        }

        request.Callbacks.Add(result => callback?.Invoke(result as T));
    }

    public void ReleaseRes<T>(string address) where T : UnityEngine.Object
    {
        var key = new AssetKey(address, typeof(T));
        CheckOwner(key);
        CancelRequest(key);
        if (cache.TryGetValue(key, out var handle))
        {
            Addressables.Release(handle);
            cache.Remove(key);
        }
    }

    // Compatibility overload. Prefer ReleaseRes<T> so ownership includes the loaded type.
    internal void ReleaseRes(string address, bool immediately = false)
    {
        var keys = new List<AssetKey>();
        foreach (var pair in cache)
        {
            if (pair.Key.Address == address)
            {
                Addressables.Release(pair.Value);
                keys.Add(pair.Key);
            }
        }
        foreach (var key in keys)
        {
            cache.Remove(key);
        }

        keys.Clear();
        foreach (var pair in activeRequests)
        {
            if (pair.Key.Address == address)
            {
                keys.Add(pair.Key);
            }
        }
        foreach (var key in keys)
        {
            CancelRequest(key);
        }

        if (immediately)
        {
            Resources.UnloadUnusedAssets();
        }
    }

    internal void ReleaseAll()
    {
        foreach (var owner in new HashSet<AssetScope>(owners.Values)) owner.Dispose();
        foreach (var handle in cache.Values)
        {
            Addressables.Release(handle);
        }
        cache.Clear();

        foreach (var request in activeRequests.Values)
        {
            request.Cancelled = true;
            request.Callbacks.Clear();
            if (request.Handle.IsValid())
            {
                Addressables.Release(request.Handle);
            }
        }
        activeRequests.Clear();
    }

    internal void ReleaseUnusedResources()
    {
        Resources.UnloadUnusedAssets();
    }

    private IEnumerator LoadAndNotify<T>(string address, AssetKey key, LoadRequest request)
        where T : UnityEngine.Object
    {
        yield return request.Handle;
        CompleteRequest(address, key, request);
    }

    private void CompleteRequest(string address, AssetKey key, LoadRequest request)
    {
        if (request.Completed)
        {
            return;
        }

        request.Completed = true;
        if (activeRequests.TryGetValue(key, out var active) && ReferenceEquals(active, request))
        {
            activeRequests.Remove(key);
        }

        if (!request.Handle.IsValid())
        {
            return;
        }

        bool succeeded = request.Handle.Status == AsyncOperationStatus.Succeeded;
        if (succeeded && !request.Cancelled)
        {
            cache[key] = request.Handle;
        }
        else
        {
            if (!succeeded)
            {
                Debug.LogError($"Failed to load asset '{address}'.");
            }
            Addressables.Release(request.Handle);
        }

        if (request.Cancelled)
        {
            request.Callbacks.Clear();
            return;
        }

        var callbacks = request.Callbacks.ToArray();
        request.Callbacks.Clear();
        UnityEngine.Object result = succeeded ? request.Handle.Result as UnityEngine.Object : null;
        foreach (var callback in callbacks)
        {
            try
            {
                callback?.Invoke(result);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    private static void ValidateAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Asset address cannot be empty.", nameof(address));
        }
    }

    private void CancelRequest(AssetKey key)
    {
        if (!activeRequests.TryGetValue(key, out var request))
        {
            return;
        }
        activeRequests.Remove(key);
        request.Cancelled = true;
        request.Callbacks.Clear();
        if (request.Handle.IsValid())
        {
            Addressables.Release(request.Handle);
        }
    }
}
