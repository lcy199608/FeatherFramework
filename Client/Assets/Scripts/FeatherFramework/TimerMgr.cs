using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public readonly struct TimerHandle : IDisposable
{
    private readonly TimerMgr owner;
    private readonly int id;

    internal TimerHandle(TimerMgr owner, int id)
    {
        this.owner = owner;
        this.id = id;
    }

    public bool IsValid => owner != null && owner.HasTimer(id);

    public void Dispose()
    {
        owner?.RemoveTimer(id);
    }
}

public sealed class TimerMgr
{
    private readonly MonoBehaviour coroutineRunner;
    public delegate void CompleteEvent();
    class TimerData
    {
        public int id;
        public CompleteEvent onCompleted; //完成回调事件
        public float time;   // 所需时间或帧数
        public float targetTime;   // 目标时间（如果是帧数则无效）
        public bool isIgnoreTimeScale;  // 是否忽略时间速率
        public bool isLoop;     //是否重复
        public int executeCount;   //循环次数
        public bool isSecond;   //是否以秒为单位 否则为帧数
        public Coroutine coroutine; //标记协程
    }

    private int timerId;
    private bool stopped;
    internal bool HasTimer(int id) => !stopped && timerDict.ContainsKey(id);
    internal void Shutdown() { if (stopped) return; stopped = true; RemoveAllTimer(); }
    private readonly Dictionary<int, TimerData> timerDict = new Dictionary<int, TimerData>();
    private readonly List<TimerData> timerSnapshot = new List<TimerData>();
    private readonly List<int> tempRemoveTimer = new List<int>();

    internal TimerMgr(MonoBehaviour coroutineRunner)
    {
        this.coroutineRunner = coroutineRunner ?? throw new ArgumentNullException(nameof(coroutineRunner));
    }

    internal void Tick()
    {
        if (stopped) return;
        timerSnapshot.Clear();
        timerSnapshot.AddRange(timerDict.Values);
        for(int i = timerSnapshot.Count - 1; i >= 0 ; i--)
        {
            TimerData timeData = timerSnapshot[i];
            if (!timeData.isSecond || !timerDict.TryGetValue(timeData.id, out var registered)
                || !ReferenceEquals(registered, timeData))
            {
                continue;
            }

            float nowTime = TimeNow(timeData.isIgnoreTimeScale);
            if (nowTime >= timeData.targetTime)
            {
                InvokeCallback(timeData);
                if (!timerDict.ContainsKey(timeData.id))
                {
                    continue;
                }

                timeData.executeCount -= 1;
                if (timeData.isLoop)
                {
                    timeData.targetTime = nowTime + timeData.time;
                }
                else
                {
                    if(timeData.executeCount <= 0)
                    {
                        tempRemoveTimer.Add(timeData.id);
                    }
                    else
                    {
                        timeData.targetTime = nowTime + timeData.time;
                    }
                }
            }
        }
        for(int i = 0; i < tempRemoveTimer.Count; i++)
        {
            RemoveTimer(tempRemoveTimer[i]);
        }
        tempRemoveTimer.Clear();
    }

    // 获取当前时间
    float TimeNow(bool isIgnoreTimeScale)
    {
        return isIgnoreTimeScale ? Time.realtimeSinceStartup : Time.time;
    }

    /// <summary>
    /// 创建一个新的定时器（支持循环）
    /// </summary>
    /// <param name="time">延迟时间（秒）</param>
    /// <param name="onCompleted">结束回调</param>
    /// <param name="isLoop">是否循环,false则只执行一次</param>
    /// <param name="isSecond">秒/帧数</param>
    /// <param name="isIgnoreTimeScale">是否受TimeScale影响</param>
    /// <returns></returns>
    internal int CreateNewTimer(float time, CompleteEvent onCompleted, bool isLoop = false, bool isSecond = true,bool isIgnoreTimeScale = false)
    {
        if (stopped) throw new ObjectDisposedException(nameof(TimerMgr));
        ValidateTimer(time, onCompleted, isSecond);
        if (isLoop && time <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(time), time, "Looping timers require a positive interval.");
        }
        int createdId = checked(++timerId);
        timerDict.Add(createdId, new TimerData()
        {
            id = createdId,
            onCompleted = onCompleted,
            time = time,
            targetTime = time + TimeNow(isIgnoreTimeScale),
            isIgnoreTimeScale = isIgnoreTimeScale,
            isLoop = isLoop,
            executeCount = 1,
            isSecond = isSecond
        });

        // 如果不是以秒为单位，则执行延迟帧数
        if (!isSecond)
        {
            timerDict[createdId].coroutine = coroutineRunner.StartCoroutine(DelayedExecution(timerDict[createdId]));
        }
        return createdId;
    }

    /// <summary>
    /// 创建一个新的定时器（支持执行次数）
    /// </summary>
    /// <param name="time">延迟时间（秒）</param>
    /// <param name="onCompleted">结束回调</param>
    /// <param name="count">执行次数</param>
    /// <param name="isSecond">秒/帧数</param>
    /// <param name="isIgnoreTimeScale">是否受TimeScale影响</param>
    /// <returns></returns>
    internal int CreateNewCountTimer(float time, CompleteEvent onCompleted, int count, bool isSecond = true, bool isIgnoreTimeScale = false)
    {
        if (stopped) throw new ObjectDisposedException(nameof(TimerMgr));
        ValidateTimer(time, onCompleted, isSecond);
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Timer count must be greater than zero.");
        }
        if (!isSecond && count > 1 && time <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(time), time, "Repeated frame timers require a positive interval.");
        }

        int createdId = checked(++timerId);
        timerDict.Add(createdId, new TimerData()
        {
            id = createdId,
            onCompleted = onCompleted,
            time = time,
            targetTime = time + TimeNow(isIgnoreTimeScale),
            isIgnoreTimeScale = isIgnoreTimeScale,
            isLoop = false,
            executeCount = count,
            isSecond = isSecond
        });

        // 如果不是以秒为单位，则执行延迟帧数
        if (!isSecond)
        {
            timerDict[createdId].coroutine = coroutineRunner.StartCoroutine(DelayedExecution(timerDict[createdId]));
        }
        return createdId;
    }

    /// <summary>
    /// 移除指定定时器
    /// </summary>
    internal void RemoveTimer(int id)
    {
        if (timerDict.ContainsKey(id))
        {
            TimerData data = timerDict[id];
            if (!data.isSecond)
            {
                if (data.coroutine != null)
                {
                    coroutineRunner.StopCoroutine(data.coroutine);
                }
            }
            data = null;
            timerDict.Remove(id);
        }
    }

    /// <summary>
    /// 清除所有定时器
    /// </summary>
    internal void RemoveAllTimer()
    {
        var timerIds = new List<int>(timerDict.Keys);
        for (int i = timerIds.Count - 1; i >= 0; i--)
        {
            RemoveTimer(timerIds[i]);
        }
    }

    IEnumerator DelayedExecution(TimerData data)
    {
        // 等待指定的帧数
        for (int i = 0; i < Mathf.Max(1, data.time); i++)
        {
            yield return null; // 等待下一帧
        }
        if (!timerDict.ContainsKey(data.id) || stopped) yield break;
        // 执行动作
        InvokeCallback(data);
        if (!timerDict.ContainsKey(data.id))
        {
            yield break;
        }

        data.executeCount -= 1;
        if (data.isLoop)
        {
            // 防止完成事件中移除了定时器，不判断会导致依然执行协程
            if(data != null && timerDict.ContainsKey(data.id))
            {
                data.coroutine = coroutineRunner.StartCoroutine(DelayedExecution(data));
            }
        }
        else
        {
            if(data.executeCount <= 0)
            {
                RemoveTimer(data.id);
            }
            else
            {
                if (data != null && timerDict.ContainsKey(data.id))
                {
                    data.coroutine = coroutineRunner.StartCoroutine(DelayedExecution(data));
                }
            }
        }
    }

    public TimerHandle AfterSeconds(float seconds, CompleteEvent onCompleted, bool isIgnoreTimeScale = false)
    {
        return new TimerHandle(this, CreateNewTimer(seconds, onCompleted, false, true, isIgnoreTimeScale));
    }

    public TimerHandle EverySeconds(float interval, CompleteEvent onCompleted, bool isIgnoreTimeScale = false)
    {
        return new TimerHandle(this, CreateNewTimer(interval, onCompleted, true, true, isIgnoreTimeScale));
    }

    public TimerHandle AfterFrames(int frames, CompleteEvent onCompleted)
    {
        return new TimerHandle(this, CreateNewTimer(frames, onCompleted, false, false));
    }

    public TimerHandle EveryFrames(int interval, CompleteEvent onCompleted)
    {
        return new TimerHandle(this, CreateNewTimer(interval, onCompleted, true, false));
    }

    private static void InvokeCallback(TimerData data)
    {
        try
        {
            data.onCompleted?.Invoke();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void ValidateTimer(float time, CompleteEvent onCompleted, bool isSecond)
    {
        if (float.IsNaN(time) || float.IsInfinity(time) || time < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(time), time, "Timer duration cannot be negative.");
        }

        if (!isSecond && time != Mathf.Floor(time))
        {
            throw new ArgumentException("Frame timers require a whole number of frames.", nameof(time));
        }

        if (onCompleted == null)
        {
            throw new ArgumentNullException(nameof(onCompleted));
        }
    }
}
