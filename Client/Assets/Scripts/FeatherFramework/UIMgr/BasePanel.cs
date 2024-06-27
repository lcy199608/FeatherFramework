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

    protected bool isStackable = false;
    public virtual void OnInit() { }
    public abstract void OnShow();
    public abstract void OnHide();
}
