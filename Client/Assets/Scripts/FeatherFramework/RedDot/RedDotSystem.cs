using cfg;
using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RedDotSystem
{
    private readonly ConfigMgr config;

    internal RedDotSystem(ConfigMgr config)
    {
        this.config = config;
    }
    private bool stopped;
    private void EnsureOpen() { if (stopped) throw new ObjectDisposedException(nameof(RedDotSystem)); }
    internal void Shutdown() { stopped = true; mRootNode?.ClearListeners(); }
    public delegate void OnRedDotNumChange(RedDotNode node); //红点变化通知
    public RedDotNode mRootNode { get; private set; } //红点树Root节点
    private readonly Dictionary<RedDotType,string> redDotTreeList = new Dictionary<RedDotType, string>(); //初始化红点树

    /// <summary>
    /// 初始化红点树结构
    /// </summary>
    internal void InitRedDotTreeNode()
    {
        redDotTreeList.Clear();
        InitRedDotData();
        string rootPath = GetPath(RedDotType.Root); //获取根节点路径
        if (string.IsNullOrWhiteSpace(rootPath) || rootPath.Contains("/"))
        {
            throw new InvalidOperationException("The red dot root path must be one non-empty segment.");
        }
        mRootNode = new RedDotNode(rootPath, rootPath,null); //根节点
        foreach (var s in redDotTreeList.Values)
        {
            ValidatePath(s, rootPath);
            AddNewRedDotToTree(s);
        }
    }

    void InitRedDotData()
    {
        foreach (var data in config.Tables.RedDot.DataList)
        {
            if (!redDotTreeList.ContainsKey(data.Type))
            {
                redDotTreeList.Add(data.Type, data.Path);
            }
            else
            {
                Debug.LogError("RedDotType Already Exists! Check RedDot Config Please.");
            }
        }
    }

    string GetPath(RedDotType type)
    {
        if (redDotTreeList.ContainsKey(type))
        {
            return redDotTreeList[type];
        }
        else
        {
            Debug.LogError("RedDotType Not Exists! Check RedDot Config Please.");
            return string.Empty;
        }
    }

    /// <summary>
    /// 遍历所有节点（从根节点开始）
    /// </summary>
    public void Traverse()
    {
        EnsureOpen();
        if (mRootNode == null)
        {
            Debug.LogError("Red dot tree has not been initialized.");
            return;
        }
        TraverseTree(mRootNode);
    }

    /// <summary>
    /// 遍历该节点下的所有节点
    /// </summary>
    /// <param name="node"></param>
    void TraverseTree(RedDotNode node)
    {
        Debug.Log("name: " + node.nodeName + " num: " + node.redDotNum);
        if (node.dicChildren.Count == 0)
        {
            return;
        }

        foreach (var item in node.dicChildren.Values)
        {
            TraverseTree(item);
        }
    }

    /// <summary>
    /// 在红点树中添加新节点
    /// </summary>
    /// <param name="strNode"></param>
    void AddNewRedDotToTree(string strNode)
    {
        var node = mRootNode;
        var treeNodeAy = strNode.Split('/'); //切割节点信息
        if (treeNodeAy[0] != mRootNode.nodeName) //如果根节点不符合，报错并跳过该节点
        {
            Debug.LogError("RedDotTree Root Node Error:" + treeNodeAy[0]);
            return;
        }

        if (treeNodeAy.Length > 1) //如果存在子节点
        {
            for (int i = 1; i < treeNodeAy.Length; i++)
            {
                //如果treeNodeAy[i]节点还不是当前节点的子节点，则添加
                if (!node.dicChildren.ContainsKey(treeNodeAy[i]))
                {
                    node.Children.Add(treeNodeAy[i], new RedDotNode(node.nodePath + "/" + treeNodeAy[i], treeNodeAy[i], node));
                }
                else
                {
                    node.dicChildren[treeNodeAy[i]].nodeName = treeNodeAy[i];
                    node.dicChildren[treeNodeAy[i]].parent = node;
                }

                node = node.dicChildren[treeNodeAy[i]]; //进入子节点，继续遍历
            }
        }
    }

    public void RemoveRedDotFromTree(RedDotType type)
    {
        EnsureOpen();
        string strNode = GetPath(type);
        if (!TryGetNode(strNode, out var node))
        {
            return;
        }
        if (node == mRootNode)
        {
            Debug.LogError("You Are Trying To Delete Root!");
            return;
        }
        RemoveNode(node);
    }

    void RemoveNode(RedDotNode node)
    {
        var parent = node.parent;
        parent.Children.Remove(node.nodeName);
        node.parent = null;
        parent.ChangeRedDotNum();
    }

    /// <summary>
    /// 设置红点回调（如果是移除又添加的需要重新绑定事件）
    /// </summary>
    /// <param name="strNode"></param>
    /// <param name="callBack"></param>
    internal void SetRedDotNodeCallBack(RedDotType type,RedDotSystem.OnRedDotNumChange callBack)
    {
        string strNode = GetPath(type);
        if (TryGetNode(strNode, out var node))
        {
            node.numChangeFunc = callBack;
        }
    }

    public IDisposable SubscribeRedDotNode(RedDotType type, OnRedDotNumChange callBack)
    {
        EnsureOpen();
        if (callBack == null)
        {
            throw new ArgumentNullException(nameof(callBack));
        }
        string path = GetPath(type);
        return TryGetNode(path, out var node) ? node.Subscribe(callBack) : null;
    }

    /// <summary>
    /// 设置指定节点数量
    /// </summary>
    /// <param name="strNode"></param>
    /// <param name="rpNum"></param>
    public void SetInvoke(RedDotType type, int rpNum)
    {
        EnsureOpen();
        string strNode = GetPath(type);
        if (TryGetNode(strNode, out var node))
        {
            node.SetRedDotNum(Mathf.Max(0, rpNum));
        }
    }

    private bool TryGetNode(string path, out RedDotNode node)
    {
        node = null;
        if (mRootNode == null || string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var segments = path.Split('/');
        if (segments.Length == 0 || segments[0] != mRootNode.nodeName)
        {
            Debug.LogError($"Red dot path '{path}' does not start at root '{mRootNode.nodeName}'.");
            return false;
        }

        node = mRootNode;
        for (int i = 1; i < segments.Length; i++)
        {
            if (!node.dicChildren.TryGetValue(segments[i], out node))
            {
                Debug.LogError($"Red dot path '{path}' does not contain node '{segments[i]}'.");
                return false;
            }
        }
        return true;
    }

    private static void ValidatePath(string path, string rootPath)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Red dot paths cannot be empty.");
        }
        var segments = path.Split('/');
        if (segments[0] != rootPath || Array.Exists(segments, string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException($"Invalid red dot path: '{path}'.");
        }
    }
}
