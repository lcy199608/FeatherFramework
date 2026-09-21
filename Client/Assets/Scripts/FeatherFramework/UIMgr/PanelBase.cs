using UnityEngine;
using System.Threading.Tasks;

public abstract class PanelBase : MonoBehaviour
{
    public object uiData;

    // 新面板覆盖 Type；两个旧布尔属性只供兼容入口使用。
    public virtual UIType Type => IsRoot ? UIType.Root : UIType.Page;
    public virtual UILayer Layer => Type == UIType.Root ? UILayer.Bottom :
        Type == UIType.Child && Handle?.Owner?.Panel != null ? Handle.Owner.ActualLayer : UILayer.Middle;
    public PanelHandle Handle { get; internal set; }

    protected Task<PanelHandle> OpenChild<T>(object data = null) where T : PanelBase
    {
        if (Handle == null) throw new System.InvalidOperationException("Panel is not open.");
        return Handle.Manager.OpenChild<T>(Handle, data);
    }

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
    /// <summary>本次打开结束时清理；暂时被其他 Page 遮挡不会触发。</summary>
    public virtual void OnClose() { }
    public virtual void OnDispose() { }

    private bool disposed;

    internal void DisposeOnce()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        try
        {
            OnDispose();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
