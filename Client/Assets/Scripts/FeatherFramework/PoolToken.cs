using System;
using UnityEngine;

public sealed class PoolToken : MonoBehaviour
{
    public string Key => Identity.Name;
    internal PoolMgr Owner { get; private set; }
    internal PoolMgr.PoolKey Identity { get; private set; }

    internal void Initialize(PoolMgr owner, PoolMgr.PoolKey key)
    {
        if (owner == null || string.IsNullOrWhiteSpace(key.Name))
        {
            throw new ArgumentException("Pool key cannot be empty.", nameof(key));
        }
        if (Owner != null && (Owner != owner || !Identity.Equals(key)))
        {
            throw new InvalidOperationException($"Pool token key cannot change from '{Key}' to '{key.Name}'.");
        }
        Owner = owner;
        Identity = key;
    }
}
