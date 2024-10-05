using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BasePanel : MonoBehaviour
{
    public object uiData;

    /// <summary>
    /// 是否可叠加在其他UI之上（比如提示气泡之类则为true）
    /// </summary>
    public virtual bool IsStackable
    {
        get;
        protected set;
    }

    /// <summary>
    /// 是否为根页面（同时只能存在一个根页面,打开新的根页面会隐藏所有弹窗）
    /// </summary>
    public virtual bool IsRoot
    {
        get;
        protected set;
    }

    public virtual void OnInit() { }
    public abstract void OnShow();
    public abstract void OnHide();
}
