using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using System;
using UnityEngine.Events;

public enum AudioType
{
    BGM,
    EFFECT
}

public class AudioMgr : DontDestroyMonoSingleton<AudioMgr>
{
    const string path = "Audios/";

    const string volFile = "BGMVolSaveData";
    const string effectVolFile = "EffectVolSaveData";

    private AudioSource tempAudio;
    private Dictionary<AudioSource, AudioType> audioSources = new Dictionary<AudioSource, AudioType>();

    float bmgVol;
    float effectVol;

    private float BGMVol
    {
        get { return bmgVol; }
        set 
        {
            if(value != BGMVol) 
            {
                bmgVol = value;
                ChangeBGMVolume(BGMVol);
            }
        }
    }

    private float EffectVol
    {
        get { return effectVol; }
        set 
        {
            if(value != EffectVol)
            {
                effectVol = value;
                ChangeEffectVolume(EffectVol);
            }
        }
    }

    public AudioMgr()
    {
        BGMVol = SaveDataMgr.GetSystemData(volFile, 1.0f);
        EffectVol = SaveDataMgr.GetSystemData(effectVolFile, 1.0f);
    }

    // 播放音效
    public void PlayAudio(string clipName, AudioType type, float fadeTime = 0, float delayTime = 0)
    {
        GetAudioClip(clipName, clip => {
            tempAudio = GetAudioSource();
            tempAudio.volume = 0;
            tempAudio.clip = clip;
            tempAudio.loop = false;

            if (audioSources.ContainsKey(tempAudio))
            {
                audioSources[tempAudio] = type;
            }
            else
            {
                audioSources.Add(tempAudio, type);
            }

            tempAudio.PlayDelayed(delayTime);
            switch (type)
            {
                case AudioType.BGM:
                    tempAudio.DOFade(BGMVol, fadeTime).SetDelay(delayTime).Restart();
                    break;
                case AudioType.EFFECT:
                    tempAudio.DOFade(EffectVol, fadeTime).SetDelay(delayTime).Restart();
                    break;
            }
        });
    }

    // 播放循环音频
    public void PlayLoopAudio(string clipName, AudioType type, float fadeTime = 0, float delayTime = 0)
    {
        GetAudioClip(clipName, clip => {
            tempAudio = GetAudioSource();
            tempAudio.volume = 0;
            tempAudio.clip = clip;
            tempAudio.loop = true;

            if (audioSources.ContainsKey(tempAudio))
            {
                audioSources[tempAudio] = type;
            }
            else
            {
                audioSources.Add(tempAudio, type);
            }

            tempAudio.PlayDelayed(delayTime);

            switch (type)
            {
                case AudioType.BGM:
                    tempAudio.DOFade(BGMVol, fadeTime).SetDelay(delayTime).Restart();
                    break;
                case AudioType.EFFECT:
                    tempAudio.DOFade(EffectVol, fadeTime).SetDelay(delayTime).Restart();
                    break;
            }
        });
    }

    // 获取音频文件
    private void GetAudioClip(string clipName, UnityAction<AudioClip> action)
    {
        ResMgr.Instance.LoadAsync(path + clipName, action);
    }

    // 获取AudioSource组件
    private AudioSource GetAudioSource()
    {
        var allAudioSources = audioSources.Keys;
        foreach (var audio in allAudioSources)
        {
            if (!audio.isPlaying)
            {
                return audio;
            }
        }
        return AddAudioSource();
    }

    // 增加组件
    private AudioSource AddAudioSource()
    {
        tempAudio = gameObject.AddComponent<AudioSource>();
        tempAudio.playOnAwake = false;
        tempAudio.spatialBlend = 0;
        return tempAudio;
    }

    // 停止某个循环的音频
    public void StopAudio(string clipName, float fadeTime = 0)
    {
        var tempAudios = audioSources.Keys;
        foreach (var audio in tempAudios)
        {
            if(audio.clip.name == clipName && audio.isPlaying)
            {
                if (fadeTime != 0)
                {
                    DOTween.To(() => audio.volume, vol => audio.volume = vol, 0, fadeTime)
                        .OnComplete(() => audio.Stop());
                }
                else
                {
                    audio.Stop();
                }
            }
        }
    }

    // 改变音量
    private void ChangeBGMVolume(float v)
    {
        foreach (var kv in audioSources)
        {
            if (kv.Value == AudioType.BGM)
            {
                kv.Key.volume = v;
            }
        }
    }

    private void ChangeEffectVolume(float v)
    {
        foreach (var kv in audioSources)
        {
            if (kv.Value == AudioType.EFFECT)
            {
                kv.Key.volume = v;
            }
        }
    }

    public void MuteBG()
    {
        if (BGMVol != 0)
        {
            ChangeBGMVolume(0);
        }
        SaveBGMVolume();
    }

    public void MuteEffect()
    {
        if (EffectVol != 0)
        {
            ChangeEffectVolume(0);
        }
        SaveEffectVolume();
    }

    /// <summary>
    /// 保存音量数据 为防止频繁写入所以没在改音量的位置直接保存
    /// </summary>
    public void Save()
    {
        SaveBGMVolume();
        SaveEffectVolume();
    }

    public void SaveBGMVolume()
    {
        SaveDataMgr.SetSystemData(volFile, BGMVol,true);
    }

    public void SaveEffectVolume()
    {
        SaveDataMgr.SetSystemData(effectVolFile, EffectVol, true);
    }
}