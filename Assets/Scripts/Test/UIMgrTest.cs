using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMgrTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        UIMgr.Instance.ShowUI("TT",UILayer.Bottom);
        Invoke("Hide",5.0f);
    }

    void Hide()
    {
        UIMgr.Instance.HideUI("TT");
    }
}
