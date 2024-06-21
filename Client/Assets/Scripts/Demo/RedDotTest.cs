using cfg;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RedDotTest : MonoBehaviour
{
    public TextOfEnhance txtTest1;
    public TextOfEnhance txtTest2;
    public TextOfEnhance txtTestChild1;
    public TextOfEnhance txtTestChild2;

    void Start()
    {
        //绑定红点回调
        RedDotSystem.Instance.SetRedDotNodeCallBack(RedDotType.RedDotTest1, Test1CallBack);
        RedDotSystem.Instance.SetRedDotNodeCallBack(RedDotType.RedDotTest2, Test2CallBack);
        RedDotSystem.Instance.SetRedDotNodeCallBack(RedDotType.RedDotTestChild1, TestChild1CallBack);
        RedDotSystem.Instance.SetRedDotNodeCallBack(RedDotType.RedDotTestChild2, TestChild2CallBack);
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
        RedDotSystem.Instance.SetInvoke(RedDotType.RedDotTestChild1, ++redDotChild1Count);
        RedDotSystem.Instance.Traverse(); //打印树
    }
    public void RemoveChild1RedDot()
    {
        RedDotSystem.Instance.SetInvoke(RedDotType.RedDotTestChild1, --redDotChild1Count);
        RedDotSystem.Instance.Traverse(); //打印树
    }
    int redDotTest2Count = 0;
    public void AddTest2RedDot()
    {
        RedDotSystem.Instance.SetInvoke(RedDotType.RedDotTest2, ++redDotTest2Count);
        RedDotSystem.Instance.Traverse(); //打印树
    }
    public void RemoveTest2RedDot()
    {
        RedDotSystem.Instance.SetInvoke(RedDotType.RedDotTest2, --redDotTest2Count);
        RedDotSystem.Instance.Traverse(); //打印树
    }
}