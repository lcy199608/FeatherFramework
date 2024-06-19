using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RedDotNode
{
    public string nodePath = string.Empty; //节点路径
    public string nodeName = string.Empty; //节点名称
    public int redDotNum = 0; //红点数量
    public RedDotNode parent = null; //父节点
    public RedDotSystem.OnRedDotNumChange numChangeFunc; //发生变化的回调函数

    //子节点
    public Dictionary<string, RedDotNode> dicChildren = new Dictionary<string, RedDotNode>();

    public RedDotNode(string nodePath, string nodeName, RedDotNode parent)
    {
        this.nodePath = nodePath;
        this.nodeName = nodeName;
        this.parent = parent;
    }

    /// <summary>
    /// 设置当前节点的红点数量
    /// </summary>
    /// <param name="rdNum"></param>
    public void SetRedDotNum(int rdNum)
    {
        if (dicChildren.Count > 0) //红点数量只能设置叶子节点
        {
            Debug.LogError("Only Can Set Leaf Nodes!");
            return;
        }
        redDotNum = rdNum;

        NotifyRedDotNumChange();

        //向上通知红点
        if (nodeName != RedDotSystem.Instance.mRootNode.nodeName && parent.nodeName != string.Empty)
        {
            parent.ChangeRedDotNum();
        }
    }

    /// <summary>
    /// 计算当前红点数量
    /// </summary>
    public void ChangeRedDotNum()
    {
        int num = 0;

        //计算红点总数
        foreach (var node in dicChildren.Values)
        {
            num += node.redDotNum;
        }
        if(num != redDotNum) //红点有变化
        {
            redDotNum = num;
            NotifyRedDotNumChange();
        }

        //向上通知红点
        if(nodeName != RedDotSystem.Instance.mRootNode.nodeName && parent.nodeName != string.Empty)
        {
            parent.ChangeRedDotNum();
        }
    }

    /// <summary>
    /// 通知红点数量变化
    /// </summary>
    public void NotifyRedDotNumChange()
    {
        numChangeFunc?.Invoke(this);
    }
}
