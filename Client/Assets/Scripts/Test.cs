using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using cfg;

public class Test : MonoBehaviour
{
    int id;
    void Start()
    {
        //UnityEngine.Debug.LogFormat(LanguageMgr.Instance.GetLanguageById(1));
        id = TimerMgr.Instance.CreateNewTimer(2, TestFunc, true,true);
        //TimerMgr.Instance.RemoveTimer(id);
        //TimerMgr.Instance.CreateNewTimer(3, TestFunc, false,true,true);
    }

    void TestFunc()
    {
        Debug.LogWarning("Complete");
        TimerMgr.Instance.RemoveTimer(id);
    }
}
