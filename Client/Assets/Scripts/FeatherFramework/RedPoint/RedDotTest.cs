using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RedDotTest : MonoBehaviour
{
    public Text txtMail;
    public Text txtMailSystem;
    public Text txtMailTeam;
    public Text txtMailTeamInfo1;
    public Text txtMailTeamInfo2;

    RedDotSystem rds = new RedDotSystem();

    void Start()
    {
        //初始化红点树
        rds.InitRedDotTreeNode();

        //绑定回调
        rds.SetRedDotNodeCallBack(RedDotConst.mail, MailCallBack);
        rds.SetRedDotNodeCallBack(RedDotConst.mailSystem, MailSystemCallBack);
        rds.SetRedDotNodeCallBack(RedDotConst.mailTeamInfo1, MailTeamInfo1CallBack);
        rds.SetRedDotNodeCallBack(RedDotConst.mailTeamInfo2, MailTeamInfo2CallBack);
        rds.SetRedDotNodeCallBack(RedDotConst.mailTeam, MailTeamCallBack);

        //修改红点数量
        rds.SetInvoke(RedDotConst.mailSystem, 3);
        rds.SetInvoke(RedDotConst.mailTeamInfo1, 2);
        rds.SetInvoke(RedDotConst.mailTeamInfo2, 1);

        rds.Traverse(); //打印树
    }

    //回调事件
    void MailCallBack(RedDotNode node)
    {
        txtMail.text = node.redDotNum.ToString();
        txtMail.gameObject.SetActive(node.redDotNum > 0);
        Debug.Log("NodeName: " + node.nodeName + " PointNum:" + node.redDotNum);
    }

    void MailTeamCallBack(RedDotNode node)
    {
        txtMailTeam.text = node.redDotNum.ToString();
        txtMailTeam.gameObject.SetActive(node.redDotNum > 0);
        Debug.Log("NodeName: " + node.nodeName + " PointNum:" + node.redDotNum);
    }

    void MailSystemCallBack(RedDotNode node)
    {
        txtMailSystem.text = node.redDotNum.ToString();
        txtMailSystem.gameObject.SetActive(node.redDotNum > 0);
        Debug.Log("NodeName: " + node.nodeName + " PointNum:" + node.redDotNum);
    }

    void MailTeamInfo1CallBack(RedDotNode node)
    {
        txtMailTeamInfo1.text = node.redDotNum.ToString();
        txtMailTeamInfo1.gameObject.SetActive(node.redDotNum > 0);
        Debug.Log("NodeName: " + node.nodeName + " PointNum:" + node.redDotNum);
    }

    void MailTeamInfo2CallBack(RedDotNode node)
    {
        txtMailTeamInfo2.text = node.redDotNum.ToString();
        txtMailTeamInfo2.gameObject.SetActive(node.redDotNum > 0);
        Debug.Log("NodeName: " + node.nodeName + " PointNum:" + node.redDotNum);
    }

    //移除指定红点
    public void RemoveRedDot()
    {
        rds.RemoveRedDotFromTree(RedDotConst.mailTeamInfo1);

        rds.Traverse(); //打印树
    }

    //添加指定红点
    public void AddRedDot()
    {
        rds.AddNewRedDotToTree(RedDotConst.mailTeamInfo1);
        rds.SetRedDotNodeCallBack(RedDotConst.mailTeamInfo1, MailTeamInfo1CallBack);
        rds.SetInvoke(RedDotConst.mailTeamInfo1, 2);

        rds.Traverse(); //打印树
    }
}
