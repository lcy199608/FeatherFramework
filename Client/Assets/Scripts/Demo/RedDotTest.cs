using cfg;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RedDotTest : FrameworkBehaviour
{
    public TextOfEnhance txtTest1;
    public TextOfEnhance txtTest2;
    public TextOfEnhance txtTestChild1;
    public TextOfEnhance txtTestChild2;

    private System.IDisposable redDotTest1Subscription;
    private System.IDisposable redDotTest2Subscription;
    private System.IDisposable redDotChild1Subscription;
    private System.IDisposable redDotChild2Subscription;

    void Start()
    {
        if (!Services.HasRedDots) { enabled = false; return; }
        redDotTest1Subscription = Services.RedDots.SubscribeRedDotNode(RedDotType.RedDotTest1, Test1CallBack);
        redDotTest2Subscription = Services.RedDots.SubscribeRedDotNode(RedDotType.RedDotTest2, Test2CallBack);
        redDotChild1Subscription = Services.RedDots.SubscribeRedDotNode(RedDotType.RedDotTestChild1, TestChild1CallBack);
        redDotChild2Subscription = Services.RedDots.SubscribeRedDotNode(RedDotType.RedDotTestChild2, TestChild2CallBack);
    }

    private void OnDestroy()
    {
        redDotTest1Subscription?.Dispose();
        redDotTest2Subscription?.Dispose();
        redDotChild1Subscription?.Dispose();
        redDotChild2Subscription?.Dispose();
    }

    //回调事件
    void Test1CallBack(RedDotNode node)
    {
        txtTest1.text = node.redDotNum.ToString();
        Debug.Log("NodeName: " + node.nodeName + " PointNum:" + node.redDotNum);
    }

    void Test2CallBack(RedDotNode node)
    {
        txtTest2.text = node.redDotNum.ToString();
        Debug.Log("NodeName: " + node.nodeName + " PointNum:" + node.redDotNum);
    }

    void TestChild1CallBack(RedDotNode node)
    {
        txtTestChild1.text = node.redDotNum.ToString();
        Debug.Log("NodeName: " + node.nodeName + " PointNum:" + node.redDotNum);
    }

    void TestChild2CallBack(RedDotNode node)
    {
        txtTestChild2.text = node.redDotNum.ToString();
        Debug.Log("NodeName: " + node.nodeName + " PointNum:" + node.redDotNum);
    }

    int redDotChild1Count = 0;
    public void AddChild1RedDot()
    {
        if (!Services.HasRedDots) return;
        Services.RedDots.SetInvoke(RedDotType.RedDotTestChild1, ++redDotChild1Count);
        Services.RedDots.Traverse(); //打印树
    }
    public void RemoveChild1RedDot()
    {
        if (!Services.HasRedDots) return;
        Services.RedDots.SetInvoke(RedDotType.RedDotTestChild1, --redDotChild1Count);
        Services.RedDots.Traverse(); //打印树
    }
    int redDotTest2Count = 0;
    public void AddTest2RedDot()
    {
        if (!Services.HasRedDots) return;
        Services.RedDots.SetInvoke(RedDotType.RedDotTest2, ++redDotTest2Count);
        Services.RedDots.Traverse(); //打印树
    }
    public void RemoveTest2RedDot()
    {
        if (!Services.HasRedDots) return;
        Services.RedDots.SetInvoke(RedDotType.RedDotTest2, --redDotTest2Count);
        Services.RedDots.Traverse(); //打印树
    }
}
