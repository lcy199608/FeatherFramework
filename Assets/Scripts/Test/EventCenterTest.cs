using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventCenterTest : MonoBehaviour
{
    void Awake()
    {
        EventCenter.Instance.AddEventListener<int>("Test", GetReward);
    }

    void GetReward(int a)
    {
        Debug.Log(a.ToString());
    }
}
