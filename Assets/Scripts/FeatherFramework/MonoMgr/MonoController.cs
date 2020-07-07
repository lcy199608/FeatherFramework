using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 声明生命周期函数
/// 事件
/// 协程
/// </summary>
public class MonoController : SingletonMono<MonoController>
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

    public void print(object message) 
    {

    }

    public void CancelInvoke(string methodName)
    {

    }

    public void CancelInvoke()
    {

    }

    public void Invoke(string methodName, float time)
    {

    }

    public void InvokeRepeating(string methodName, float time, float repeatRate)
    {

    }

    public new bool IsInvoking(string methodName)
    {

    }

    public new bool IsInvoking()
    {

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

    }

    public void StopAllCoroutines()
    {

    }

    public void StopCoroutine(IEnumerator routine)
    {

    }

    public void StopCoroutine(Coroutine routine)
    {

    }

    public void StopCoroutine(string methodName)
    {

    }
}
