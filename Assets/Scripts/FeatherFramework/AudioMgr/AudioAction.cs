using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AudioAction : MonoBehaviour
{
    public bool isStartPlay;
    public bool isStartStop;
    public bool isDestroyPlay;
    public bool isDestroyStop;
    public float delayTime = 0;
    public AudioType type = AudioType.EFFECT;
    public string clipName;
    public bool isLoop;
    public float fadeTime = 0;

    private void Start()
    {
        if (isStartPlay)
            PlayAudio();
        if (isStartStop)
            Invoke("StopAudio", delayTime);
    }

    private void OnDisable()
    {
        if (isDestroyStop)
            Invoke("StopAudio", delayTime);
        if (isDestroyPlay)
            PlayAudio();
    }

    public void PlayAudio()
    {
        if (isLoop)
        {
            AudioMgr.Instance.PlayLoopAudio(clipName, type, fadeTime, delayTime);
        }
        else
        {
            AudioMgr.Instance.PlayAudio(clipName, type, fadeTime, delayTime);
        }
    }

    public void StopAudio()
    {
        AudioMgr.Instance.StopAudio(clipName, fadeTime);
    }
}
