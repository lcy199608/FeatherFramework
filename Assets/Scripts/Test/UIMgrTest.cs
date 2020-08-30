using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMgrTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        UIMgr.Instance.ShowUI<TestPanel>("TestPanel", UILayer.Bottom);
        Invoke("Hide", 5);
    }

    void Hide()
    {
        UIMgr.Instance.HideUI("TestPanel");
        UIMgr.Instance.ShowUI<TestPanel>("TestPanel", UILayer.Bottom);
    }
}
