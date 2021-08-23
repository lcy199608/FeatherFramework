using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using DG.Tweening;
public class TestPanel : BasePanel
{
    public override void ShowPanel()
    {
        
    }

    public override void HidePanel()
    {

    }

    public void Test(int index)
    {

        Debug.Log(index);
        gameObject.transform.localScale = Vector3.zero;
    }

//auto
    

    public override void Init()
    {
        base.Init();
        
    }
}
