
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public readonly struct EventId : IEquatable<EventId>
{
    public string Name { get; }

    public EventId(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Event name cannot be empty.", nameof(name));
        }
        Name = name;
    }

    public bool Equals(EventId other) => Name == other.Name;
    public override bool Equals(object obj) => obj is EventId other && Equals(other);
    public override int GetHashCode() => Name == null ? 0 : Name.GetHashCode();
}

public readonly struct EventId<T> : IEquatable<EventId<T>>
{
    public string Name { get; }

    public EventId(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Event name cannot be empty.", nameof(name));
        }
        Name = name;
    }

    public bool Equals(EventId<T> other) => Name == other.Name;
    public override bool Equals(object obj) => obj is EventId<T> other && Equals(other);
    public override int GetHashCode() => Name == null ? 0 : Name.GetHashCode();
}

public interface IEventInfo
{
    //这是一个空接口
}
internal sealed class EventInfo<T> : IEventInfo
{
    private UnityAction<T> actions;

    public EventInfo(UnityAction<T> action)
    {
        actions += action;
    }

    public void Add(UnityAction<T> action) => actions += action;
    public void Remove(UnityAction<T> action) => actions -= action;
    public bool IsEmpty => actions == null;

    public void Invoke(T payload)
    {
        var invocationList = actions?.GetInvocationList();
        if (invocationList == null)
        {
            return;
        }
        foreach (var callback in invocationList)
        {
            try
            {
                ((UnityAction<T>)callback).Invoke(payload);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
            }
        }
    }
}
internal sealed class EventInfo : IEventInfo
{
    private UnityAction actions;

    public EventInfo(UnityAction action)
    {
        actions += action;
    }

    public void Add(UnityAction action) => actions += action;
    public void Remove(UnityAction action) => actions -= action;
    public bool IsEmpty => actions == null;

    public void Invoke()
    {
        var invocationList = actions?.GetInvocationList();
        if (invocationList == null)
        {
            return;
        }
        foreach (var callback in invocationList)
        {
            try
            {
                ((UnityAction)callback).Invoke();
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
            }
        }
    }
}


public sealed class EventCenter
{
    //字典中，key对应着事件的名字，
    //value对应的是监听这个事件对应的委托方法们（重点圈住：们）
    private readonly struct EventKey : IEquatable<EventKey>
    {
        public readonly string Name;
        public readonly Type PayloadType;

        public EventKey(string name, Type payloadType)
        {
            Name = name;
            PayloadType = payloadType;
        }

        public bool Equals(EventKey other) => Name == other.Name && PayloadType == other.PayloadType;
        public override bool Equals(object obj) => obj is EventKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Name, PayloadType);
    }

    private static readonly Type EmptyPayloadType = typeof(void);
    private readonly Dictionary<EventKey, IEventInfo> eventDic = new Dictionary<EventKey, IEventInfo>();

    private bool stopped;
    private void EnsureOpen() { if (stopped) throw new ObjectDisposedException(nameof(EventCenter)); }
    internal void Shutdown() { stopped = true; Clear(); }

    //添加事件监听
    //第一个参数：事件的名字
    //第二个参数：处理事件的方法
    internal void AddEventListener<T>(string name, UnityAction<T> action)
    {
        EnsureOpen();
        ValidateSubscription(name, action);
        var key = new EventKey(name, typeof(T));
        //有没有对应的事件监听
        //有的情况
        if (eventDic.TryGetValue(key, out var eventInfo))
        {
            ((EventInfo<T>)eventInfo).Add(action);
        }
        //没有的情况
        else
        {
            eventDic.Add(key, new EventInfo<T>(action));
        }
    }
    //对于不需要参数的情况的重载方法
    internal void AddEventListener(string name, UnityAction action)
    {
        EnsureOpen();
        ValidateSubscription(name, action);
        var key = new EventKey(name, EmptyPayloadType);
        if (eventDic.TryGetValue(key, out var eventInfo))
        {
            ((EventInfo)eventInfo).Add(action);
        }
        else
        {
            eventDic.Add(key, new EventInfo(action));
        }
    }

    //通过事件名字进行事件触发
    internal void EventTrigger<T>(string name, T info)
    {
        EnsureOpen();
        var key = new EventKey(name, typeof(T));
        //有没有对应的事件监听
        //有的情况（有人关心这个事件）
        if (eventDic.TryGetValue(key, out var eventInfo))
        {
            ((EventInfo<T>)eventInfo).Invoke(info);
        }
    }
    //对于不需要参数的情况的重载方法
    internal void EventTrigger(string name)
    {
        EnsureOpen();
        var key = new EventKey(name, EmptyPayloadType);
        if (eventDic.TryGetValue(key, out var eventInfo))
        {
            ((EventInfo)eventInfo).Invoke();
        }
    }
    //移除对应的事件监听
    internal void RemoveEventListener<T>(string name, UnityAction<T> action)
    {
        var key = new EventKey(name, typeof(T));
        if (eventDic.TryGetValue(key, out var eventInfo))
        {
            var typedInfo = (EventInfo<T>)eventInfo;
            typedInfo.Remove(action);
            if (typedInfo.IsEmpty)
            {
                eventDic.Remove(key);
            }
        }
    }
    //对于不需要参数的情况的重载方法
    internal void RemoveEventListener(string name, UnityAction action)
    {
        var key = new EventKey(name, EmptyPayloadType);
        if (eventDic.TryGetValue(key, out var eventInfo))
        {
            var typedInfo = (EventInfo)eventInfo;
            typedInfo.Remove(action);
            if (typedInfo.IsEmpty)
            {
                eventDic.Remove(key);
            }
        }
    }

    //清空所有事件监听(主要用在切换场景时)
    public void Clear()
    {
        eventDic.Clear();
    }

    internal IDisposable Subscribe<T>(string name, UnityAction<T> action)
    {
        UnityAction<T> registration = value => action(value);
        ValidateSubscription(name, action);
        AddEventListener(name, registration);
        return new Subscription(() => RemoveEventListener(name, registration));
    }

    internal IDisposable Subscribe(string name, UnityAction action)
    {
        UnityAction registration = () => action();
        ValidateSubscription(name, action);
        AddEventListener(name, registration);
        return new Subscription(() => RemoveEventListener(name, registration));
    }

    internal void Publish<T>(string name, T payload) => EventTrigger(name, payload);
    internal void Publish(string name) => EventTrigger(name);

    public IDisposable Subscribe<T>(EventId<T> id, UnityAction<T> action)
    {
        return Subscribe(id.Name, action);
    }

    public IDisposable Subscribe(EventId id, UnityAction action)
    {
        return Subscribe(id.Name, action);
    }

    public void Publish<T>(EventId<T> id, T payload) => Publish(id.Name, payload);
    public void Publish(EventId id) => Publish(id.Name);

    private static void ValidateSubscription(string name, Delegate action)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Event name cannot be empty.", nameof(name));
        }

        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }
    }

    private sealed class Subscription : IDisposable
    {
        private Action unsubscribe;

        public Subscription(Action unsubscribe)
        {
            this.unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            unsubscribe?.Invoke();
            unsubscribe = null;
        }
    }
}

public static class FrameworkEvents
{
    private const string FrameworkReady = "FrameworkReady";
    private const string LanguageChanged = "LanguageChanged";
    private const string SceneProgressChanged = "SceneProgressChanged";

    public static readonly EventId FrameworkReadyId = new EventId(FrameworkReady);
    public static readonly EventId LanguageChangedId = new EventId(LanguageChanged);
    public static readonly EventId<AsyncOperation> SceneProgressChangedId =
        new EventId<AsyncOperation>(SceneProgressChanged);
}
