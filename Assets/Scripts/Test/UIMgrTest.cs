using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMgrTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        UIMgr.Instance.ShowUI("TT",UILayer.Bottom);
        //UIMgr.Instance.ShowUI("GG", UILayer.Bottom);
        //UIMgr.Instance.ShowUI("CC", UILayer.Bottom);
        //Invoke("Hide",5.0f);
        //Invoke("Clear", 10.0f);
    }

    void Hide()
    {
        UIMgr.Instance.HideUI("TT");
        UIMgr.Instance.HideUI("GG");
        UIMgr.Instance.HideUI("CC");
    }

    void Clear()
    {
        PoolMgr.Instance.Clear();
    }
}
