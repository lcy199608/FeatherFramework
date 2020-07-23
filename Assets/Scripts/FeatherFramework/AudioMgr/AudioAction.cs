using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AudioAction : MonoBehaviour
{
    public string clipName;
    public bool isLoop;
    public ulong delayTime = 0;
    public float fadeTime = 0;

    public void PlayAudio()
    {
        if (isLoop)
        {
            AudioMgr.Instance.PlayLoopAudio(clipName, fadeTime);
        }
        else
        {
            AudioMgr.Instance.PlayAudio(clipName, fadeTime);
        }
    }

    public void StopAudio()
    {
        AudioMgr.Instance.StopAudio(clipName);
    }
}
