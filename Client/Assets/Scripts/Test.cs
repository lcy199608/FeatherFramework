using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using cfg;

public class Test : MonoBehaviour
{
    List<GameObject> cache = new List<GameObject>();
    void Start()
    {
        //UIMgr.Instance.ShowUI<JoystickPanel>();
    }

    public void TestFunc()
    {
        ResMgr.Instance.LoadAsync<GameObject>("Res/UI/JoystickPanel.prefab", (obj) => { cache.Add(Instantiate(obj)); });
        ResMgr.Instance.LoadAsync<GameObject>("Res/UI/JoystickPanel.prefab", (obj) => { cache.Add(Instantiate(obj)); });
        ResMgr.Instance.LoadAsync<GameObject>("Res/UI/JoystickPanel.prefab", (obj) => { cache.Add(Instantiate(obj)); });
        ResMgr.Instance.LoadAsync<GameObject>("Res/UI/JoystickPanel.prefab", (obj) => { cache.Add(Instantiate(obj)); });
    }

    public void TestFunc2()
    {
        ResMgr.Instance.ReleaseRes("Res/UI/JoystickPanel.prefab",true);
        //ResMgr.Instance.ReleaseUnusedResources();
    }

    public void TestFunc3()
    {
        cache.ForEach(obj => Destroy(obj));
        cache.Clear();
    }
}
