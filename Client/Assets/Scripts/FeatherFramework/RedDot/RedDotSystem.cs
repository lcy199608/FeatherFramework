using cfg;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RedDotSystem : Singleton<RedDotSystem>
{
    public delegate void OnRedDotNumChange(RedDotNode node); //红点变化通知
    public RedDotNode mRootNode; //红点树Root节点
    static Dictionary<RedDotType,string> redDotTreeList = new Dictionary<RedDotType, string>(); //初始化红点树

    /// <summary>
    /// 初始化红点树结构
    /// </summary>
    public void InitRedDotTreeNode()
    {
        redDotTreeList.Clear();
        InitRedDotData();
        string rootPath = GetPath(RedDotType.Root); //获取根节点路径
        mRootNode = new RedDotNode(rootPath, rootPath,null); //根节点
        foreach (var s in redDotTreeList.Values)
        {
            AddNewRedDotToTree(s);
        }
    }

    void InitRedDotData()
    {
        foreach (var data in ConfigMgr.Config.RedDot.DataList)
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
                    node.dicChildren.Add(treeNodeAy[i], new RedDotNode(strNode, treeNodeAy[i], node));
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
        var node = mRootNode;
        string strNode = GetPath(type);
        var treeNodeAy = strNode.Split('/'); //切割节点信息
        if (treeNodeAy[0] != mRootNode.nodeName) //如果根节点不符合，报错并跳过该节点
        {
            Debug.LogError("RedDotTree Root Node Error:" + treeNodeAy[0]);
            return;
        }

        if (treeNodeAy.Length > 1) //如果存在子节点
        {
            //遍历获取最末目标节点
            for (int i = 1; i < treeNodeAy.Length; i++)
            {
                //判断该节点是否在红点树内
                if (!node.dicChildren.ContainsKey(treeNodeAy[i]))
                {
                    Debug.LogError("Does Not Contains Child Node: " + treeNodeAy[i]);
                    return;
                }

                node = node.dicChildren[treeNodeAy[i]];
            }

            RemoveNode(type, node);
        }
        else
        {
            Debug.LogError("You Are Trying To Delete Root!");
        }
    }

    void RemoveNode(RedDotType type, RedDotNode node)
    {
        SetInvoke(type, 0);
        node.parent.dicChildren.Remove(node.nodeName);
        node.parent = null;
    }

    /// <summary>
    /// 设置红点回调（如果是移除又添加的需要重新绑定事件）
    /// </summary>
    /// <param name="strNode"></param>
    /// <param name="callBack"></param>
    public void SetRedDotNodeCallBack(RedDotType type,RedDotSystem.OnRedDotNumChange callBack)
    {
        string strNode = GetPath(type);
        var nodeList = strNode.Split('/'); //分析树节点
        if(nodeList.Length == 1)
        {
            if(nodeList[0] != GetPath(RedDotType.Root))
            {
                //根节点不对
                Debug.LogError("Get Wrong Root Node! Current Is " + nodeList[0]);
                return;
            }
        }

        var node = mRootNode;

        //遍历传入key并获取最后一个节点添加回调
        for (int i = 1; i < nodeList.Length; i++)
        {
            //判断该节点是否在红点树内
            if (!node.dicChildren.ContainsKey(nodeList[i]))
            {
                Debug.LogError("Does Not Contains Child Node: " + nodeList[i]);
                return;
            }
            node = node.dicChildren[nodeList[i]]; //获取当前遍历到的节点

            if(i == nodeList.Length - 1) //最后一个节点设置回调
            {
                node.numChangeFunc = callBack;
                return;
            }
        }
    }

    /// <summary>
    /// 设置指定节点数量
    /// </summary>
    /// <param name="strNode"></param>
    /// <param name="rpNum"></param>
    public void SetInvoke(RedDotType type, int rpNum)
    {
        string strNode = GetPath(type);
        var nodeList = strNode.Split('/'); //分析树节点

        //判断根节点是否符合
        if(nodeList.Length == 1)
        {
            if(nodeList[0] != GetPath(RedDotType.Root))
            {
                Debug.LogError("Get Wrong Root Node! Current Is " + nodeList[0]);
                return;
            }
        }

        var node = mRootNode;
        for (int i = 1; i < nodeList.Length; i++)
        {
            //判断该遍历节点是否在树中
            if (!node.dicChildren.ContainsKey(nodeList[i]))
            {
                Debug.LogError("Does Not Contains Child Node: " + nodeList[i]);
                return;
            }

            node = node.dicChildren[nodeList[i]];

            if(i == nodeList.Length - 1) //最后一个节点
            {
                node.SetRedDotNum(rpNum); //设置节点的红点数量
            }
        }
    }
}
