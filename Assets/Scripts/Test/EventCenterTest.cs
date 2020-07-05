using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventCenterTest : MonoBehaviour
{
    void Awake()
    {
        EventCenter.Instance.AddEventListener("Test", GetReward);
    }

    void GetReward(object obj)
    {
        Debug.Log((obj as int?).ToString());
    }
}
