using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PoolTest : MonoBehaviour
{
    private Transform parent;
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PoolMgr.Instance.GetObj("Image", _ => { _.transform.SetParent(parent == null ? parent = GameObject.Find("Canvas").transform : parent); _.transform.localPosition = Vector3.zero; });
        }

        if (Input.GetMouseButtonDown(1))
        {
            PoolMgr.Instance.GetObj("Test1", _ => { });
        }
    }
}
