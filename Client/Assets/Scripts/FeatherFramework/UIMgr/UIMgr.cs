using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public enum UILayer
{
    Bottom,
    Middle,
    Top,
    System
}

public enum UIType
{
    Root,
    Page,
    Child,
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

public class UIMgr : DontDestroyMonoSingleton<UIMgr>
{
    public const string uiPath = "Res/UI/";
    private Dictionary<string, GameObject> panelDic = new Dictionary<string, GameObject>(); //当前打开过的UI

    public Canvas UICanvas;
    public Camera UICamera;
    private Transform bot;
    private Transform mid;
    private Transform top;
    private Transform sys;

    /// <summary>
    /// 获取UI上的脚本
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="uiName"></param>
    /// <returns></returns>
    public bool TryGetUI<T>(out T panel)
    {
        var uiName = typeof(T).Name;

        if (panelDic.ContainsKey(uiName))
        {
            panel = panelDic[uiName].GetComponent<T>();
            return true;
        }
        else
        {
            Debug.LogError("Dont Exist This UI!");
            panel = default(T);
            return false;
        }
    }

    public void ShowUI<T>(UILayer layer = UILayer.Middle,object uiData = null,Action onCompleted = null)
    {
        if (UICanvas == null)
        {
            CreateUICanvas();
        }

        var uiName = typeof(T).Name;
        if (panelDic.ContainsKey(uiName))
        {
            InitShowUI<T>(panelDic[uiName], layer);
            onCompleted?.Invoke();
        }
        else
        {
            ResMgr.Instance.LoadAsync<GameObject>(uiPath + uiName + ".prefab", panel =>
            {
                var panelTemp = Instantiate(panel);
                var panelSrc = panelTemp.GetComponent<T>() as BasePanel;
                panelSrc.uiData = uiData;
                panelSrc.OnInit();
                InitShowUI<T>(panelTemp, layer);
                panelDic.Add(uiName, panelTemp);
                onCompleted?.Invoke();
            });
        }
    }

    public void CreateUICanvas()
    {
        //同步方式加载Canvas，过场景后不删除Canvas
        UICanvas = Instantiate(ResMgr.Instance.Load<GameObject>(uiPath + "UICanvas" + ".prefab")).GetComponent<Canvas>();
        Transform canvas = UICanvas.transform;
        GameObject.DontDestroyOnLoad(UICanvas);
        //获取Canvas中的各个层级
        bot = canvas.Find("BottomLayer");
        mid = canvas.Find("MiddleLayer");
        top = canvas.Find("TopLayer");
        sys = canvas.Find("SystemLayer");
        UICamera = GameObject.Find("UICamera").GetComponent<Camera>();
        UICanvas.worldCamera = UICamera;
    }

    // 初始化要显示的UI
    void InitShowUI<T>(GameObject panel, UILayer layer)
    {
        switch (layer)
        {
            case UILayer.Bottom:
                panel.transform.SetParent(bot);
                break;
            case UILayer.Middle:
                panel.transform.SetParent(mid);
                break;
            case UILayer.Top:
                panel.transform.SetParent(top);
                break;
            case UILayer.System:
                panel.transform.SetParent(sys);
                break;
            default:
                break;
        }

        panel.transform.localPosition = Vector3.zero;
        panel.transform.localScale = Vector3.one;
        (panel.transform as RectTransform).offsetMax = Vector3.zero;
        (panel.transform as RectTransform).offsetMin = Vector3.zero;
        panel.transform.SetAsLastSibling();
        panel.gameObject.SetActive(true);

        var panelSrc = panel.GetComponent<T>() as BasePanel;
        panelSrc.OnShow();
    }

    public void HideUI(string uiName)
    {
        panelDic[uiName].GetComponent<BasePanel>().OnHide();
    }
    public void HideUI<T>()
    {
        var uiName = typeof(T).Name;
        panelDic[uiName].GetComponent<BasePanel>().OnHide();
    }

    public void HideAllUI()
    {
        panelDic.Values.ToList().ForEach(panel => 
        {
            try
            {
                panel.GetComponent<BasePanel>().OnHide();
                panel.SetActive(false);
            }
            catch (Exception e)
            {
                Debug.LogError("HideAllUI Throw Exception! \n" + e.Message);
            }
        });
    }

    public void RemoveSpecifiedUI(string uiName)
    {
        if (panelDic.ContainsKey(uiName))
        {
            panelDic[uiName].Destroy();
            panelDic.Remove(uiName);
        }
        else
        {
            Debug.LogError("PanelDic Dont Exist This UI!");
        }
    }

    //移除指定UI实例
    public void RemoveSpecifiedUI<T>()
    {
        var uiName = typeof(T).Name;
        if (panelDic.ContainsKey(uiName))
        {
            panelDic[uiName].Destroy();
            panelDic.Remove(uiName);
        }
        else
        {
            Debug.LogError("PanelDic Dont Exist This UI!");
        }
    }

    //销毁所有UI实例
    public void ClearPanelDic()
    {
        var panels = panelDic.Values;
        for (int i = 0; i < panels.Count; i++)
        {
            Destroy(panels.ElementAt(i));
        }
        panelDic.Clear();
    }
}
