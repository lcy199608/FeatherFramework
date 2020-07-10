using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PoolMgr.Instance.GetObj("Test");
        }

        if (Input.GetMouseButtonDown(1))
        {
            PoolMgr.Instance.GetObj("Test1");
        }
    }
}
