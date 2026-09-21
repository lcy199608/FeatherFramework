using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AudioAction : FrameworkBehaviour
{
    public bool isStartPlay;
    public bool isStartStop;
    public bool isOnEnablePlay;
    public bool isOnEnableStop;
    public bool isDestroyPlay;
    public bool isDestroyStop;
    public float delayTime = 0;
    public AudioType type = AudioType.EFFECT;
    public string clipName;
    public bool isLoop;
    public float fadeTime = 0;
    [Tooltip("Allow a non-looping voice to finish independently after this component is destroyed.")]
    public bool continueOneShotAfterDestroy;
    private AudioVoiceHandle currentVoice;

    private void Start()
    {
        if (isStartPlay)
            PlayAudio();
        if (isStartStop)
            ScheduleStop();
    }

    private void OnEnable()
    {
        CancelInvoke(nameof(StopAudio));
        if (isOnEnablePlay && !isStartPlay)
            PlayAudio();
        if (isOnEnableStop && !isStartStop)
            ScheduleStop();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(StopAudio));
        if (isDestroyStop)
            ScheduleStop();
        if (isDestroyPlay)
            PlayAudio();
    }

    private void ScheduleStop()
    {
        if (delayTime <= 0)
        {
            StopAudio();
            return;
        }
        Invoke(nameof(StopAudio), delayTime);
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(StopAudio));
        if (isLoop || !continueOneShotAfterDestroy) currentVoice.Stop();
        currentVoice = default;
    }

    public void PlayAudio()
    {
        if (!Framework.IsReady)
        {
            return;
        }
        currentVoice.Stop();
        if (isLoop)
        {
            currentVoice = Services.Audio.PlayLoopAudio(clipName, type, fadeTime, delayTime);
        }
        else
        {
            currentVoice = Services.Audio.PlayAudio(clipName, type, fadeTime, delayTime);
        }
    }

    public void StopAudio()
    {
        if (!Framework.IsReady)
        {
            return;
        }
        if (currentVoice.IsValid)
        {
            currentVoice.Stop(fadeTime);
            currentVoice = default;
        }
    }
}
