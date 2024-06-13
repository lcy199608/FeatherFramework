using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIEventListenerMgr : DontDestroyMonoSingleton<UIEventListenerMgr>
{
    public enum EventType
    {
        Drag,
        BeginDrag,
        EndDrag,
        Click,
        Down,
        Up,
    }

    public void AddSafeListener(GameObject obj,EventType type,Action<PointerEventData> e)
    {
        RemoveListener(obj, type);
        AddListener(obj, type, e);
    }

    public void AddListener(GameObject obj, EventType type, Action<PointerEventData> e)
    {
        UIEventListener listener = obj.GetComponent<UIEventListener>();
        if (!obj.GetComponent<UIEventListener>())
        {
            listener = obj.AddComponent<UIEventListener>();
        }

        switch (type)
        {
            case EventType.Drag:
                listener.dragEvent += e;
                break;
            case EventType.BeginDrag:
                listener.beginDragEvent += e;
                break;
            case EventType.EndDrag:
                listener.endDragEvent += e;
                break;
            case EventType.Click:
                listener.clickEvent += e;
                break;
            case EventType.Down:
                listener.downEvent += e;
                break;
            case EventType.Up:
                listener.upEvent += e;
                break;
        }
    }

    public void RemoveListener(GameObject obj, EventType type)
    {
        UIEventListener listener = obj.GetComponent<UIEventListener>();
        if (!obj.GetComponent<UIEventListener>())
            return;

        switch (type)
        {
            case EventType.Drag:
                listener.dragEvent = null;
                break;
            case EventType.BeginDrag:
                listener.beginDragEvent = null;
                break;
            case EventType.EndDrag:
                listener.endDragEvent = null;
                break;
            case EventType.Click:
                listener.clickEvent = null;
                break;
            case EventType.Down:
                listener.downEvent = null;
                break;
            case EventType.Up:
                listener.upEvent = null;
                break;
        }
    }

    public void RemoveAllListener(GameObject obj)
    {
        UIEventListener listener = obj.GetComponent<UIEventListener>();
        if (!obj.GetComponent<UIEventListener>())
            return;
        listener.dragEvent = null;
        listener.beginDragEvent = null;
        listener.endDragEvent = null;
        listener.clickEvent = null;
        listener.downEvent = null;
        listener.upEvent = null;
    }
}