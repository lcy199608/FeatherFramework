using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimerMgr : MonoSingleton<TimerMgr>
{
    public delegate void CompleteEvent();
    //bool isLog = true;//是否打印消息
    class TimerData
    {
        public int id;
        public CompleteEvent onCompleted; //完成回调事件
        public float time;   // 所需时间或帧数
        public float targetTime;   // 目标时间（如果是帧数则无效）
        public bool isIgnoreTimeScale;  // 是否忽略时间速率
        public bool isLoop;     //是否重复
        public bool isSecond;   //是否以秒为单位 否则为帧数
    }

    int timerId = 0;
    Dictionary<int, TimerData> timerDict = new Dictionary<int, TimerData>();
    List<int> tempRemoveTimer = new List<int>();
    void Update()
    {
        foreach (var (id, timeData) in timerDict)
        {
            float nowTime = TimeNow(timeData.isIgnoreTimeScale);
            if (nowTime >= timeData.targetTime)
            {
                timeData.onCompleted?.Invoke();
                if (timeData.isLoop)
                {
                    timeData.targetTime = nowTime + timeData.time;
                }
                else
                {
                    tempRemoveTimer.Add(id);
                }
            }
            else
            {
                timeData.targetTime = nowTime + timeData.time;
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

    // 创建一个新的定时器
    public int CreateNewTimer(float time, CompleteEvent onCompleted, bool isLoop = false, bool isSecond = true,bool isIgnoreTimeScale = false)
    {
        timerId += 1;
        timerDict.Add(timerId, new TimerData()
        {
            id = timerId,
            onCompleted = onCompleted,
            time = time,
            targetTime = time + TimeNow(isIgnoreTimeScale),
            isIgnoreTimeScale = isIgnoreTimeScale,
            isLoop = isLoop,
            isSecond = isSecond
        });

        // 如果不是以秒为单位，则执行延迟帧数
        if (!isSecond)
        {
            StartCoroutine(DelayedExecution(timerDict[timerId]));
        }
        return timerId;
    }

    // 停止指定定时器
    public void RemoveTimer(int id)
    {
        if (timerDict.ContainsKey(id))
        {
            timerDict.Remove(id);
        }
    }

    //清除所有定时器
    public void RemoveAllTimer()
    {
        timerDict.Clear();
    }

    IEnumerator DelayedExecution(TimerData data)
    {
        // 等待指定的帧数
        for (int i = 0; i < data.time; i++)
        {
            yield return null; // 等待下一帧
        }
        // 执行动作
        data.onCompleted?.Invoke();
        if (data.isLoop)
        {
            DelayedExecution(data);
        }
        else
        {
            RemoveTimer(data.id);
        }
    }
}
