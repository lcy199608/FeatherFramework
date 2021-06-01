using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Linq;
using UnityEngine;
using UnityEngine.Events;

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

    public UIInfo(string name, UILayer layer = UILayer.Middle)
    {
        Name = name;
        Layer = layer;
    }
}

public class UIMgr : SingletonMono<UIMgr>
{
    public const string uiPath = "UI/";
    private Dictionary<string, GameObject> panelDic = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> panelCacheDic = new Dictionary<string, GameObject>();

    GameObject UICanvas;
    private Transform bot;
    private Transform mid;
    private Transform top;
    private Transform system;

    /// <summary>
    /// 获取UI上的脚本
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="uiName"></param>
    /// <returns></returns>
    public T GetUI<T>(bool isShow = false, UILayer layer = UILayer.Middle)
    {
        T temp;
        var uiName = typeof(T).Name;

        if (panelDic.ContainsKey(uiName))
        {
            temp = panelDic[uiName].GetComponent<T>();
        }
        else
        {
            //同步加载UI然后返回组件

            if (UICanvas == null)
            {
                UICanvas = ResMgr.Instance.Load<GameObject>(uiPath + "UICanvas");
                Transform canvas = UICanvas.transform;
                GameObject.DontDestroyOnLoad(UICanvas);
                bot = canvas.Find("BottomLayer");
                mid = canvas.Find("MiddleLayer");
                top = canvas.Find("TopLayer");
                system = canvas.Find("SystemLayer");
            }

            UICanvas.GetComponent<Canvas>().worldCamera = GameObject.Find("UICamera").GetComponent<Camera>();

            GameObject cache;

            if (panelCacheDic.ContainsKey(uiName))
            {
                cache = panelCacheDic[uiName];
            }
            else
            {
                cache = Resources.Load<GameObject>(uiPath + uiName);
                panelCacheDic.Add(uiName, cache); //缓存UI
            }



            var obj = Instantiate(cache);
            temp = obj.GetComponent<T>();
            var panel = temp as BasePanel;
            panel.Init();
            panelDic.Add(uiName, obj);
        }

        if(isShow)
            InitShowUI<T>(panelDic[uiName], layer, isShow);

        if (temp == null)
            Debug.LogError("获取组件失败！");

        return temp;
    }

    public void ShowUI<T>(UILayer layer = UILayer.Middle)
    {
        if (UICanvas == null)
        {
            //同步方式加载Canvas，过场景后不删除Canvas
            UICanvas = ResMgr.Instance.Load<GameObject>(uiPath + "UICanvas");
            Transform canvas = UICanvas.transform;
            GameObject.DontDestroyOnLoad(UICanvas);
            //获取Canvas中的各个层级
            bot = canvas.Find("BottomLayer");
            mid = canvas.Find("MiddleLayer");
            top = canvas.Find("TopLayer");
            system = canvas.Find("SystemLayer");
        }

        UICanvas.GetComponent<Canvas>().worldCamera = GameObject.Find("UICamera").GetComponent<Camera>();

        var uiName = typeof(T).Name;

        if (panelDic.ContainsKey(uiName))
        {
            InitShowUI<T>(panelDic[uiName], layer);
        }
        else
        {
            if (panelCacheDic.ContainsKey(uiName))
            {
                var obj = panelCacheDic[uiName];

                var panelTemp = Instantiate(obj);
                var panel = panelTemp.GetComponent<T>() as BasePanel;
                panel.Init();

                InitShowUI<T>(panelTemp, layer);
                panelDic.Add(uiName, panelTemp);
            }
            else
            {
                var a = Resources.LoadAsync<GameObject>(uiPath + uiName);
                a.completed += _ =>
                {
                    var obj = a.asset as GameObject;
                    panelCacheDic.Add(uiName, obj); //缓存UI

                    var panelTemp = Instantiate(obj);
                    var panel = panelTemp.GetComponent<T>() as BasePanel;
                    panel.Init();

                    InitShowUI<T>(panelTemp, layer);
                    panelDic.Add(uiName, panelTemp);
                }; 
            }
        }
    }

    /// <summary>
    /// 初始化要显示的UI
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="_"></param>
    /// <param name="layer"></param>
    void InitShowUI<T>(GameObject _, UILayer layer,bool isShow = true)
    {
        switch (layer)
        {
            case UILayer.Bottom:
                _.transform.SetParent(bot);
                break;
            case UILayer.Middle:
                _.transform.SetParent(mid);
                break;
            case UILayer.Top:
                _.transform.SetParent(top);
                break;
            case UILayer.System:
                _.transform.SetParent(system);
                break;
            default:
                break;
        }

        _.transform.localPosition = Vector3.zero;
        _.transform.localScale = Vector3.one;
        (_.transform as RectTransform).offsetMax = Vector3.zero;
        (_.transform as RectTransform).offsetMin = Vector3.zero;
        _.transform.SetAsLastSibling();
        _.gameObject.SetActive(isShow);

        var panel = _.GetComponent<T>() as BasePanel;
        if(isShow)
            panel.ShowPanel();
    }

    public void HideUI(string name)
    {
        panelDic[name].GetComponent<BasePanel>().HidePanel();
    }
    public void HideUI<T>()
    {
        var name = typeof(T).Name;
        panelDic[name].GetComponent<BasePanel>().HidePanel();
    }

    public void HideAllUI()
    {
        panelDic.Values.ToList().ForEach(_ => 
        {
            try
            {
                _.GetComponent<BasePanel>().HidePanel();
                _.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.LogError("HideAllUI Throw Exception!");
            }
        });
    }

    public void RemoveSpecifiedUI(string name)
    {
        if (panelDic.ContainsKey(name))
        {
            panelDic[name].Destroy();
            panelDic.Remove(name);
        }
        else
        {
            Debug.LogError("panelDic不存在此UI!");
        }
    }

    //移除指定UI实例
    public void RemoveSpecifiedUI<T>()
    {
        var name = typeof(T).Name;
        if (panelDic.ContainsKey(name))
        {
            panelDic[name].Destroy();
            panelDic.Remove(name);
        }
        else
        {
            Debug.LogError("panelDic不存在此UI!");
        }
    }

    //销毁所有UI实例
    public void ClearPanelDic()
    {
        panelDic.Values.ToList().ForEach(_ => _.Destroy());
        panelDic.Clear();
    }

    //清理所有缓存，除非UI缓存太多不然尽量不要用
    public void ClearAllCache()
    {
        panelCacheDic.Clear();
        Resources.UnloadUnusedAssets();
    }
}
