using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Reflection;
using UnityEngine.UI;

public class Test : SingletonMono<Test>
{
    private void OnGUI()
    {
        if (GUILayout.Button("ShowUI"))
        {
            UIMgr.Instance.ShowUI<TestPanel>();
        }

        if (GUILayout.Button("GetUI"))
        {
            UIMgr.Instance.GetUI<TestPanel>().Test(10);
        }

        if (GUILayout.Button("RemoveSpecifiedUI"))
        {
            UIMgr.Instance.RemoveSpecifiedUI<TestPanel>();
        }

        if (GUILayout.Button("ClearPanelDic"))
        {
            UIMgr.Instance.ClearPanelDic();
        }

        if (GUILayout.Button("ClearAllCache"))
        {
            UIMgr.Instance.ClearAllCache();
        }
    }
}
