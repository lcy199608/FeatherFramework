using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventCenterTriTest : MonoBehaviour
{
    void Start()
    {
        EventCenter.Instance.EventTrigger("Test", 10);
    }
}
