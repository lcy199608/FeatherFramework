using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Events;
using static UnityEditor.PlayerSettings;

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
    public object UIData;
    public Action<BasePanel> OnComplete;

    public UIInfo(string name, UILayer layer = UILayer.Middle, object uiData = null, Action<BasePanel> onComplete = null)
    {
        Name = name;
        Layer = layer;
        UIData = uiData;
        OnComplete = onComplete;
    }
}

public class UIMgr : DontDestroyMonoSingleton<UIMgr>
{
    public const string uiPath = "Res/UI/";
    private Dictionary<string, GameObject> panelCacheDic = new Dictionary<string, GameObject>(); //当前打开过的UI
    private List<UIInfo> openUIStack = new List<UIInfo>(); //模仿栈，先进后出。 打开新的page时，先隐藏当前的page。关闭当前page时，再显示上一个page。

    private Transform bot;
    private Transform mid;
    private Transform top;
    private Transform sys;
    public Canvas UICanvas;
    private Camera uiCamera;
    public Camera UICamera
    {
        get
        {
            if (uiCamera == null)
            {
                var cameraObj = transform.Find("UICamera");
                if (cameraObj != null)
                {
                    UICamera = cameraObj.GetComponent<Camera>();
                }
            }
            return uiCamera;
        }
        set { uiCamera = value; }
    }


    /// <summary>
    /// 获取UI上的脚本
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="uiName"></param>
    /// <returns></returns>
    public bool TryGetUI<T>(out T panel)
    {
        var uiName = typeof(T).Name;

        if (panelCacheDic.ContainsKey(uiName))
        {
            panel = panelCacheDic[uiName].GetComponent<T>();
            return true;
        }
        else
        {
            Debug.LogError("Dont Exist This UI!");
            panel = default(T);
            return false;
        }
    }

    public void ShowUI<T>(UILayer layer = UILayer.Middle, object uiData = null, Action<BasePanel> onComplete = null) where T : BasePanel
    {
        var uiName = typeof(T).Name;
        ShowUI(uiName, layer, uiData, onComplete);
    }

    public void ShowUI(string uiName, UILayer layer = UILayer.Middle, object uiData = null, Action<BasePanel> onComplete = null)
    {
        if (UICanvas == null)
        {
            CreateUICanvas();
        }

        if (panelCacheDic.ContainsKey(uiName))
        {
            InitShowUI(panelCacheDic[uiName], layer, onComplete);
        }
        else
        {
            var panel = ResMgr.Instance.Load<GameObject>(uiPath + uiName + ".prefab");
            var panelTemp = Instantiate(panel);
            panelTemp.name = uiName;
            var panelSrc = panelTemp.GetComponent<BasePanel>();
            panelSrc.uiData = uiData;
            panelSrc.OnInit();
            InitShowUI(panelTemp, layer, onComplete);
            panelCacheDic.Add(uiName, panelTemp);
        }
    }

    /// <summary>
    /// 连续打开一组UI(按先后顺序显示)
    /// </summary>
    public void ShowUIQueue(params UIInfo[] infos)
    {
        if (infos == null || infos.Length == 0)
        {
            return;
        }
        //关闭当前打开的面板
        if (openUIStack.Count > 0)
        {
            string topUIName = openUIStack.Last().Name;
            if (panelCacheDic.TryGetValue(topUIName, out var topUI))
            {
                topUI.gameObject.Hide();
            }
        }
        for (int i = infos.Length - 1; i >= 0; i--)
        {
            openUIStack.Add(infos[i]);
        }
        //打开队列第一个
        var uiInfo = openUIStack.Last();
        ShowUI(uiInfo.Name, uiInfo.Layer, uiInfo.UIData, uiInfo.OnComplete);
    }

    public void CreateUICanvas()
    {
        //同步方式加载Canvas，过场景后不删除Canvas
        UICanvas = Instantiate(ResMgr.Instance.Load<GameObject>(uiPath + "UICanvas" + ".prefab")).GetComponent<Canvas>();
        Transform canvas = UICanvas.transform;
        //GameObject.DontDestroyOnLoad(UICanvas);
        //获取Canvas中的各个层级
        bot = canvas.Find("BottomLayer");
        mid = canvas.Find("MiddleLayer");
        top = canvas.Find("TopLayer");
        sys = canvas.Find("SystemLayer");
    }

    // 初始化要显示的UI
    void InitShowUI(GameObject panel, UILayer layer, Action<BasePanel> onComplete = null)
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

        var panelSrc = panel.GetComponent<BasePanel>();
        panelSrc.OnShow();
        onComplete?.Invoke(panelSrc);

        if(panelSrc.IsRoot)
        {
            HideAllUI();
            panel.gameObject.SetActive(true);
        }
        else
        {
            if (!panelSrc.IsStackable)
            {
                if (openUIStack.Count > 0)
                {
                    string topUIName = openUIStack.Last().Name;
                    if (topUIName != panel.name)
                    {
                        if (panelCacheDic.TryGetValue(topUIName, out var topUI))
                        {
                            topUI.gameObject.Hide();
                        }
                        openUIStack.Add(new UIInfo(panel.name, layer, panelSrc.uiData, onComplete));
                    }
                }
                else
                {
                    openUIStack.Add(new UIInfo(panel.name, layer, panelSrc.uiData, onComplete));
                }
            }
        }

    }

    public void HideUI(string uiName)
    {
        if (panelCacheDic.TryGetValue(uiName, out var panel))
        {
            BasePanel panelSrc = panel.GetComponent<BasePanel>();
            panelSrc.OnHide();
            if (openUIStack.Count > 0)
            {
                var topUIInfo = openUIStack.Last();
                if (topUIInfo.Name == uiName)
                {
                    openUIStack.RemoveAt(openUIStack.Count - 1);
                    if (openUIStack.Count > 0)
                    {
                        topUIInfo = openUIStack.Last();
                        if (panelCacheDic.TryGetValue(topUIInfo.Name, out var topPanel))
                        {
                            topPanel.gameObject.Show();
                        }
                        else{
                            ShowUI(topUIInfo.Name, topUIInfo.Layer, topUIInfo.UIData, topUIInfo.OnComplete);
                        }
                    }
                }
                else
                {
                    RemoveSpecifiedUIFromStack(uiName);
                }
            }
        }
        else
        {
            Debug.LogError("Dont Exist This UI");
        }
    }

    public void RemoveSpecifiedUIFromStack(string uiName)
    {
        if (openUIStack.Count > 0)
        {
            for (int i = openUIStack.Count - 1; i >= 0; i--)
            {
                var topUIInfo = openUIStack[i];
                if (topUIInfo.Name == uiName)
                {
                    openUIStack.RemoveAt(i);
                    return;
                }
            }
        }
    }

    public void HideUI<T>() where T : BasePanel
    {
        var uiName = typeof(T).Name;
        HideUI(uiName);
    }

    public void HideAllUI()
    {
        panelCacheDic.Values.ForEach(panel =>
        {
            panel.Hide();
        });
        openUIStack.Clear();
    }

    public void RemoveSpecifiedUI(string uiName)
    {
        if (panelCacheDic.TryGetValue(uiName, out var uiPanel))
        {
            for (int i = 0; i < openUIStack.Count; i++)
            {
                var uiInfo = openUIStack[i];
                if (uiInfo.Name == uiName)
                {
                    HideUI(uiName);
                    if (openUIStack.Contains(uiInfo))
                    {
                        openUIStack.Remove(uiInfo);
                    }
                    break;
                }
            }
            uiPanel.Destroy();
            panelCacheDic.Remove(uiName);
        }
        else
        {
            Debug.LogError("PanelDic Dont Exist This UI!");
        }
    }

    //移除指定UI实例
    public void RemoveSpecifiedUI<T>() where T : BasePanel
    {
        var uiName = typeof(T).Name;
        RemoveSpecifiedUI(uiName);
    }

    //销毁所有UI实例
    public void RemoveAllUI()
    {
        var panels = panelCacheDic.Values;
        for (int i = 0; i < panels.Count; i++)
        {
            Destroy(panels.ElementAt(i));
        }
        openUIStack.Clear();
        panelCacheDic.Clear();
    }
}
