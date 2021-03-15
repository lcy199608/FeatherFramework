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

        if (GUILayout.Button("RemoveS"))
        {
            UIMgr.Instance.RemoveSpecifiedUI<TestPanel>();
        }

        if (GUILayout.Button("RemoveP"))
        {
            UIMgr.Instance.ClearPanelDic();
        }

        if (GUILayout.Button("RemoveC"))
        {
            UIMgr.Instance.ClearAllCache();
        }

        GameObject go = null;
        go.Show();
    }
}
