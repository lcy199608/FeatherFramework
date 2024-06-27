using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.Analytics;
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

public class UIInfo<T>
{
    public string Name;
    public UILayer Layer;
    public T PanelSrc;
    public object UIData;
    public Action<T> OnComplete;

    public UIInfo(string name, T panelSrc,UILayer layer = UILayer.Middle,object uiData = null,Action<T> onComplete = null)
    {
        Name = name;
        Layer = layer;
        PanelSrc = panelSrc;
        UIData = uiData;
        OnComplete = onComplete;
    }
}

public class UIMgr : DontDestroyMonoSingleton<UIMgr>
{
    public const string uiPath = "Res/UI/";
    private Dictionary<string, GameObject> panelDic = new Dictionary<string, GameObject>(); //当前打开过的UI
    private Stack<BasePanel> openUIStack = new Stack<BasePanel>(); //打开新的page时，先隐藏当前的page。关闭当前page时，再显示上一个page。
    private Queue<UIInfo<BasePanel>> uiQueue = new Queue<UIInfo<BasePanel>>(); //UI队列，用于按顺序打开UI。

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

    public void ShowUI<T>(UILayer layer = UILayer.Middle,object uiData = null,Action<T> onComplete = null) where T : BasePanel
    {
        if (UICanvas == null)
        {
            CreateUICanvas();
        }

        var uiName = typeof(T).Name;
        if (panelDic.ContainsKey(uiName))
        {
            InitShowUI<T>(panelDic[uiName], layer);
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
            });
        }
    }

    public void ShowUIQueue(Queue<UIInfo<BasePanel>> infos)
    {
        if(infos == null || infos.Count == 0)
        {
            return;
        }

        uiQueue = infos;
        if(openUIStack.Count > 0)
        {
            openUIStack.Peek().gameObject.Hide();
        }

        var uiInfo = infos.Dequeue();
        //TODO:UI队列的处理
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
    void InitShowUI<T>(GameObject panel, UILayer layer,Action<T> onComplete = null)
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

        var src = panel.GetComponent<T>();
        var panelSrc = src as BasePanel;
        panelSrc.OnShow();
        onComplete?.Invoke(src);
        Debug.LogError(panelSrc.IsStackable);
        if(!panelSrc.IsStackable)
        {
            if(openUIStack.Count > 0)
            {
                BasePanel top = openUIStack.Peek();
                top.gameObject.Hide();
            }
            openUIStack.Push(panelSrc);
        }
    }

    public void HideUI(string uiName)
    {
        if(panelDic.TryGetValue(uiName,out var panel))
        {
            BasePanel panelSrc = panel.GetComponent<BasePanel>();
            panelSrc.OnHide();
            if (openUIStack.Count > 0)
            {
                BasePanel top = openUIStack.Peek();
                if (top == panelSrc)
                {
                    openUIStack.Pop();
                    if (openUIStack.Count > 0)
                    {
                        openUIStack.Peek().gameObject.Show();
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Dont Exist This UI");
        }
    }
    public void HideUI<T>() where T : BasePanel
    {
        var uiName = typeof(T).Name;
        HideUI(uiName);
    }

    public void HideAllUI()
    {
        panelDic.Keys.ForEach(panelName =>
        {
            HideUI(panelName);
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
    public void RemoveSpecifiedUI<T>() where T : BasePanel
    {
        var uiName = typeof(T).Name;
        RemoveSpecifiedUI(uiName);
    }

    //销毁所有UI实例
    public void RemoveAllUI()
    {
        var panels = panelDic.Values;
        for (int i = 0; i < panels.Count; i++)
        {
            Destroy(panels.ElementAt(i));
        }
        panelDic.Clear();
    }
}
