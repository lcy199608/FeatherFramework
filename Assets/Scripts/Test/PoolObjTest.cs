using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolObjTest : MonoBehaviour
{
    private void OnEnable()
    {
        base.Invoke("Invoke", 1);
    }

    void Invoke()
    {
        PoolMgr.Instance.PushObj(gameObject.name, gameObject);
    }
}
