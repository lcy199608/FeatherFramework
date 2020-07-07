using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 事件中心
/// 1.dictionary
/// 2.委托
/// 3.观察者模式
/// </summary>
public class EventCenter : Singleton<EventCenter>
{
    //key 事件的名字
    //value 对应的监听这个事件的委托函数们
    private Dictionary<string, UnityAction<object>> eventDic = new Dictionary<string, UnityAction<object>>();

    //添加事件监听
    //比如Start
    public void AddEventListener(string name,UnityAction<object> action)
    {
        if (eventDic.ContainsKey(name))
        {
            eventDic[name] += action;
        }
        else
        {
            eventDic.Add(name, action);
        }
    }

    //移除事件监听
    //比如Destroy
    public void RemoveEventListener(string name,UnityAction<object> action)
    {
        if (eventDic.ContainsKey(name))
        {
            eventDic[name] -= action;
        }
    }

    //触发事件监听
    public void EventTrigger(string name,object obj)
    {
        if (eventDic.ContainsKey(name))
        {
            eventDic[name].Invoke(obj);
        }
    }

    //切场景等需要清空的情况
    public void Clear()
    {
        eventDic.Clear();
    }
}
