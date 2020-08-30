using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UILayer
{
    Bottom,
    Middle,
    Top,
    System
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
    private Dictionary<string, GameObject> panelDic = new Dictionary<string, GameObject>();

    GameObject UICanvas;
    private Transform bot;
    private Transform mid;
    private Transform top;
    private Transform system;

    public void ShowUI<T>(string name, UILayer layer)
    {
        if(UICanvas == null)
        {
            //同步方式加载Canvas，过场景后不删除Canvas
            UICanvas = ResMgr.Instance.Load<GameObject>(uiPath + "UICanvas");
            Transform canvas = UICanvas.transform;
            //GameObject.DontDestroyOnLoad(UICanvas);
            //获取Canvas中的各个层级
            bot = canvas.Find("BottomLayer");
            mid = canvas.Find("MiddleLayer");
            top = canvas.Find("TopLayer");
            system = canvas.Find("SystemLayer");
        }


        if (panelDic.ContainsKey(name))
        {
            panelDic[name].transform.localPosition = Vector3.zero;
            panelDic[name].transform.localScale = Vector3.one;
            (panelDic[name].transform as RectTransform).offsetMax = Vector3.zero;
            (panelDic[name].transform as RectTransform).offsetMin = Vector3.zero;
            panelDic[name].transform.SetAsLastSibling();
            panelDic[name].gameObject.SetActive(true);

            var panel = panelDic[name].GetComponent<T>() as BasePanel;
            panel.ShowPanel();
        }
        else
        {
            ResMgr.Instance.LoadAsync<GameObject>(uiPath + name, _ =>
            {
                switch (layer)
                {
                    case UILayer.Bottom:
                        _.transform.SetParent(bot);
                        _.transform.SetAsLastSibling();
                        break;
                    case UILayer.Middle:
                        _.transform.SetParent(mid);
                        _.transform.SetAsLastSibling();
                        break;
                    case UILayer.Top:
                        _.transform.SetParent(top);
                        _.transform.SetAsLastSibling();
                        break;
                    case UILayer.System:
                        _.transform.SetParent(system);
                        _.transform.SetAsLastSibling();
                        break;
                    default:
                        break;
                }

                _.transform.localPosition = Vector3.zero;
                _.transform.localScale = Vector3.one;
                (_.transform as RectTransform).offsetMax = Vector3.zero;
                (_.transform as RectTransform).offsetMin = Vector3.zero;
                _.transform.SetAsLastSibling();
                _.gameObject.SetActive(true);

                var panel = _.GetComponent<T>() as BasePanel;
                panel.ShowPanel();

                panelDic.Add(name, _);
            });
        }
    }

    public void HideUI(string name)
    {
        panelDic[name].GetComponent<BasePanel>().HidePanel();
    }

    public void ClearCashe()
    {
        panelDic.Clear();
        Resources.UnloadUnusedAssets();
    }
}
