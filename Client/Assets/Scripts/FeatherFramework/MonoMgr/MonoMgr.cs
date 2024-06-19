using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Internal;
using System.Linq;

/// <summary>
/// 声明生命周期函数
/// 事件
/// 协程
/// </summary>
public class MonoMgr : DontDestroyMonoSingleton<MonoMgr>
{
    public new Coroutine StartCoroutine(IEnumerator routine)
    {
        return base.StartCoroutine(routine);
    }

    public new void StopCoroutine(IEnumerator routine)
    {
        base.StopCoroutine(routine);
    }

    public new void StopCoroutine(Coroutine routine)
    {
        base.StopCoroutine(routine);
    }

    //协程调用简化
    public void DelayToCall(Action action,float time)
    {
        StartCoroutine(DelayToCallEnumerator(action, time));
    }

    private IEnumerator DelayToCallEnumerator(Action action,float time)
    {
        yield return new WaitForSeconds(time);
        action();
    }
}
