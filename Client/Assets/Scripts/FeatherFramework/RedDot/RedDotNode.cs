using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class RedDotNode
{
    public string nodePath { get; } //节点路径
    public string nodeName { get; internal set; } //节点名称
    public int redDotNum { get; private set; } //红点数量
    public RedDotNode parent { get; internal set; } //父节点
    internal RedDotSystem.OnRedDotNumChange numChangeFunc; //发生变化的回调函数
    private readonly List<RedDotSystem.OnRedDotNumChange> listeners = new List<RedDotSystem.OnRedDotNumChange>();

    //子节点
    internal readonly Dictionary<string, RedDotNode> Children = new Dictionary<string, RedDotNode>();
    public IReadOnlyDictionary<string, RedDotNode> dicChildren { get; }

    internal RedDotNode(string nodePath, string nodeName, RedDotNode parent)
    {
        dicChildren = new System.Collections.ObjectModel.ReadOnlyDictionary<string, RedDotNode>(Children);
        this.nodePath = nodePath;
        this.nodeName = nodeName;
        this.parent = parent;
    }

    /// <summary>
    /// 设置当前节点的红点数量
    /// </summary>
    /// <param name="rdNum"></param>
    internal void SetRedDotNum(int rdNum)
    {
        if (dicChildren.Count > 0) //红点数量只能设置叶子节点
        {
            Debug.LogError("Only Can Set Leaf Nodes!");
            return;
        }
        redDotNum = Mathf.Max(0, rdNum);

        NotifyRedDotNumChange();

        //向上通知红点
        if (parent != null)
        {
            parent.ChangeRedDotNum();
        }
    }

    /// <summary>
    /// 计算当前红点数量
    /// </summary>
    internal void ChangeRedDotNum()
    {
        long total = 0;

        //计算红点总数
        foreach (var node in dicChildren.Values)
        {
            total += node.redDotNum;
        }
        int num = (int)Math.Min(int.MaxValue, total);
        if(num != redDotNum) //红点有变化
        {
            redDotNum = num;
            NotifyRedDotNumChange();
        }

        //向上通知红点
        if(parent != null)
        {
            parent.ChangeRedDotNum();
        }
    }

    /// <summary>
    /// 通知红点数量变化
    /// </summary>
    internal void NotifyRedDotNumChange()
    {
        InvokeListener(numChangeFunc);
        var snapshot = listeners.ToArray();
        foreach (var listener in snapshot)
        {
            InvokeListener(listener);
        }
    }

    internal void ClearListeners() { listeners.Clear(); numChangeFunc = null; foreach (var child in Children.Values) child.ClearListeners(); }

    private void InvokeListener(RedDotSystem.OnRedDotNumChange listener)
    {
        try
        {
            listener?.Invoke(this);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    internal IDisposable Subscribe(RedDotSystem.OnRedDotNumChange listener)
    {
        if (listener == null)
        {
            throw new ArgumentNullException(nameof(listener));
        }
        listeners.Add(listener);
        return new CallbackSubscription(() => listeners.Remove(listener));
    }

    private sealed class CallbackSubscription : IDisposable
    {
        private Action dispose;

        public CallbackSubscription(Action dispose)
        {
            this.dispose = dispose;
        }

        public void Dispose()
        {
            dispose?.Invoke();
            dispose = null;
        }
    }
}
