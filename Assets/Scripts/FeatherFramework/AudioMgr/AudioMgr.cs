using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using System;
using UnityEngine.Events;

public enum AudioType
{
    BG,
    EFFECT
}

public class AudioMgr : SingletonMono<AudioMgr>
{
    const string path = "Audios/";

    const string volFile = "VolFile";
    const string effectVolFile = "EffectVolFile";

    private AudioSource tempAudio;
    private List<AudioClip> audios = new List<AudioClip>();
    private List<AudioSource> allAudioSources = new List<AudioSource>();

    private Dictionary<AudioSource, AudioType> audioTypes = new Dictionary<AudioSource, AudioType>();

    float vol;
    float effectVol;

    private float Vol
    {
        get { return vol; }
        set { vol = value; }
    }

    private float EffectVol
    {
        get { return effectVol; }
        set { effectVol = value; }
    }

    private void Awake()
    {
        vol = PlayerPrefs.GetFloat(volFile, 1.0f);
        effectVol = PlayerPrefs.GetFloat(effectVolFile, 0.7f);
    }

    // 播放音效
    public void PlayAudio(string clipName, AudioType type, float fadeTime = 0, float delayTime = 0)
    {
        GetAudioClip(clipName, _ => {
            tempAudio = GetAudioSource();
            tempAudio.volume = 0;
            tempAudio.clip = _;
            tempAudio.loop = false;

            if (audioTypes.ContainsKey(tempAudio))
            {
                audioTypes[tempAudio] = type;
            }
            else
            {
                audioTypes.Add(tempAudio, type);
            }

            tempAudio.PlayDelayed(delayTime);
            switch (type)
            {
                case AudioType.BG:
                    tempAudio.DOFade(Vol, fadeTime).SetDelay(delayTime).Restart();
                    break;
                case AudioType.EFFECT:
                    tempAudio.DOFade(effectVol, fadeTime).SetDelay(delayTime).Restart();
                    break;
            }
            allAudioSources.Add(tempAudio);
        });
    }

    // 播放循环音频
    public void PlayLoopAudio(string clipName, AudioType type, float fadeTime = 0, float delayTime = 0)
    {
        GetAudioClip(clipName, _ => {
            tempAudio = GetAudioSource();
            tempAudio.volume = 0;
            tempAudio.clip = _;
            tempAudio.loop = true;

            if (audioTypes.ContainsKey(tempAudio))
            {
                audioTypes[tempAudio] = type;
            }
            else
            {
                audioTypes.Add(tempAudio, type);
            }

            tempAudio.PlayDelayed(delayTime);

            switch (type)
            {
                case AudioType.BG:
                    tempAudio.DOFade(Vol, fadeTime).SetDelay(delayTime).Restart();
                    break;
                case AudioType.EFFECT:
                    tempAudio.DOFade(effectVol, fadeTime).SetDelay(delayTime).Restart();
                    break;
            }

            allAudioSources.Add(tempAudio);
        });
    }

    // 获取音频文件
    private void GetAudioClip(string clipName, UnityAction<AudioClip> action)
    {
        if (audios.Any(_ => _.name == clipName))
        {
            action(audios.First(_ => _.name == clipName));
        }
        else
        {
            action += _ => audios.Add(_);
            ResMgr.Instance.LoadAsync(path + clipName, action);
        }
    }

    // 获取AudioSource组件
    private AudioSource GetAudioSource()
    {
        if (allAudioSources.Any(_ => !_.isPlaying))
        {
            tempAudio = allAudioSources.First(_ => !_.isPlaying);
            return tempAudio;
        }
        else
        {
            return AddAudioSource();
        }
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
    public void StopAudio(string clipName, float fadeTime)
    {
        allAudioSources.Where(_ => _.clip.name == clipName).ToList().ForEach(_ =>
        {
            if (fadeTime != 0)
            {
                DOTween.To(() => _.volume, x => _.volume = x, 0, fadeTime).OnComplete(() => _.Stop());
            }
            else
            {
                _.Stop();
            }
        });
    }

    // 改变音量
    public void ChangeBGVolume(float v)
    {
        Vol = v;
        audioTypes?.Where(_ => _.Value == AudioType.BG).ToList().ForEach(_ => _.Key.volume = v);
    }

    public void ChangeEffectVolume(float v)
    {
        EffectVol = v;
        audioTypes?.Where(_ => _.Value == AudioType.EFFECT).ToList().ForEach(_ => _.Key.volume = v);
    }

    public void MuteBG()
    {
        if (Vol != 0)
            ChangeBGVolume(0);
        Save();
    }

    public void MuteEffect()
    {
        if (EffectVol != 0)
            ChangeEffectVolume(0);
        Save();
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(volFile, Vol);
        PlayerPrefs.SetFloat(effectVolFile, EffectVol);
    }
}