using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UILayer
{
    Bottom,
    Middle,
    Top
}

public class UIInfo
{
    public string Name;
    public UILayer Layer;

    public UIInfo(string name,UILayer layer = UILayer.Middle)
    {
        Name = name;
        Layer = layer;
    }
}

public class UIMgr : SingletonMono<UIMgr>
{
    public const string uiPath = "UI/";

    public void ShowUI(string name, UILayer layer)
    {
        ResMgr.Instance.LoadAsync<GameObject>(name, _ =>
        {
            switch (layer)
            {
                case UILayer.Bottom:
                    _.transform.SetParent(GameObject.Find("BottomLayer").transform);
                    _.transform.SetAsLastSibling();
                    break;
                case UILayer.Middle:
                    _.transform.SetParent(GameObject.Find("MiddleLayer").transform);
                    _.transform.SetAsLastSibling();
                    break;
                case UILayer.Top:
                    _.transform.SetParent(GameObject.Find("TopLayer").transform);
                    _.transform.SetAsLastSibling();
                    break;
            }
        });
        //PoolMgr.Instance.GetObj(uiPath + name, _ =>
        // {
        //     switch (layer)
        //     {
        //         case UILayer.Bottom:
        //             _.transform.SetParent(GameObject.Find("BottomLayer").transform);
        //             _.transform.SetAsLastSibling();
        //             break;
        //         case UILayer.Middle:
        //             _.transform.SetParent(GameObject.Find("MiddleLayer").transform);
        //             _.transform.SetAsLastSibling();
        //             break;
        //         case UILayer.Top:
        //             _.transform.SetParent(GameObject.Find("TopLayer").transform);
        //             _.transform.SetAsLastSibling();
        //             break;
        //     }
        // });
    }

    public void HideUI(string name)
    {
        PoolMgr.Instance.PushObj("UI", GameObject.Find(uiPath + name).gameObject);
    }
}
