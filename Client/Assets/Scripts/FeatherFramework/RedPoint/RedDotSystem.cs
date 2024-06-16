using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedDotSystem : MonoBehaviour
{
    public delegate void OnRedDotNumChange(RedDotNode node); //红点变化通知
    RedDotNode mRootNode; //红点树Root节点

    static List<string> redDotTreeList = new List<string> //初始化红点树
    {
        //填入红点树信息

        RedDotConst.main,
        RedDotConst.mail,
        RedDotConst.mailSystem,
        RedDotConst.mailTeam,
        RedDotConst.mailTeamInfo1,
        RedDotConst.mailTeamInfo2,
    };

    /// <summary>
    /// 初始化红点树结构
    /// </summary>
    public void InitRedDotTreeNode()
    {
        mRootNode = new RedDotNode(); //根节点
        mRootNode.nodeName = RedDotConst.main; //设置根节点名称

        foreach (var s in redDotTreeList)
        {
            AddNewRedDotToTree(s);
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
        Debug.Log(node.nodeName);
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
    public void AddNewRedDotToTree(string strNode)
    {
        var node = mRootNode;
        var treeNodeAy = strNode.Split('.'); //切割节点信息
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
                    node.dicChildren.Add(treeNodeAy[i], new RedDotNode());
                }
                node.dicChildren[treeNodeAy[i]].nodeName = treeNodeAy[i];
                node.dicChildren[treeNodeAy[i]].parent = node;

                node = node.dicChildren[treeNodeAy[i]]; //进入子节点，继续遍历
            }
        }
    }

    public void RemoveRedDotFromTree(string strNode)
    {
        var node = mRootNode;

        var treeNodeAy = strNode.Split('.'); //切割节点信息
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

            RemoveNode(strNode, node);
        }
        else
        {
            Debug.LogError("You Are Trying To Delete Root!");
        }
    }

    void RemoveNode(string strNode, RedDotNode node)
    {
        SetInvoke(strNode, 0);
        node.parent.dicChildren.Remove(node.nodeName);
        node.parent = null;
    }

    /// <summary>
    /// 设置红点回调（如果是移除又添加的需要重新绑定事件）
    /// </summary>
    /// <param name="strNode"></param>
    /// <param name="callBack"></param>
    public void SetRedDotNodeCallBack(string strNode,RedDotSystem.OnRedDotNumChange callBack)
    {
        var nodeList = strNode.Split('.'); //分析树节点
        if(nodeList.Length == 1)
        {
            if(nodeList[0] != RedDotConst.main)
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
    public void SetInvoke(string strNode,int rpNum)
    {
        var nodeList = strNode.Split('.'); //分析树节点

        //判断根节点是否符合
        if(nodeList.Length == 1)
        {
            if(nodeList[0] != RedDotConst.main)
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
