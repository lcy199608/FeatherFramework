using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Internal;

/// <summary>
/// 声明生命周期函数
/// 事件
/// 协程
/// </summary>
public class MonoMgr : SingletonMono<MonoMgr>
{
    public event UnityAction updateEvent;

    void Update()
    {
        updateEvent?.Invoke();
    }

    public void AddUpdateListener(UnityAction action)
    {
        updateEvent += action;
    }

    public void RemoveUpdateListener(UnityAction action)
    {
        updateEvent -= action;
    }

    public new void print(object message) 
    {
        MonoBehaviour.print(message);
    }

    public new void CancelInvoke(string methodName)
    {
        base.CancelInvoke(methodName);
    }

    public new void CancelInvoke()
    {
        base.CancelInvoke();
    }

    public new void Invoke(string methodName, float time)
    {
        base.Invoke(methodName, time);
    }

    public new void InvokeRepeating(string methodName, float time, float repeatRate)
    {
        base.InvokeRepeating(methodName, time, repeatRate);
    }

    public new bool IsInvoking(string methodName)
    {
        return base.IsInvoking(methodName);
    }

    public new bool IsInvoking()
    {
        return base.IsInvoking();
    }

    public new Coroutine StartCoroutine(string methodName)
    {
        return base.StartCoroutine(methodName);
    }

    public new Coroutine StartCoroutine(IEnumerator routine)
    {
        return base.StartCoroutine(routine);
    }

    public new Coroutine StartCoroutine(string methodName, [DefaultValue("null")] object value)
    {
        return base.StartCoroutine(methodName, value);
    }

    public new void StopAllCoroutines()
    {
        base.StopAllCoroutines();
    }

    public new void StopCoroutine(IEnumerator routine)
    {
        base.StopCoroutine(routine);
    }

    public new void StopCoroutine(Coroutine routine)
    {
        base.StopCoroutine(routine);
    }

    public new void StopCoroutine(string methodName)
    {
        base.StopCoroutine(methodName);
    }
}
