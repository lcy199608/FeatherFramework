using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using System;
using UnityEngine.Events;

public class AudioMgr : SingletonMono<AudioMgr>
{
    const string path = "Audios/";
    const string volFile = "VolFile";

    private AudioSource tempAudio;
    private List<AudioClip> audios = new List<AudioClip>();
    private List<AudioSource> allAudioSources = new List<AudioSource>();

    float vol;

    public float Vol
    {
        get { return vol; }
        set { vol = value; }
    }

    private void Awake()
    {
        vol = PlayerPrefs.GetFloat(volFile, 1.0f);
    }

    // 播放音效
    public void PlayAudio(string clipName, float fadeTime = 0)
    {
        GetAudioClip(clipName,_ => {
            tempAudio = GetAudioSource();
            tempAudio.volume = 0;
            tempAudio.clip = _;
            tempAudio.loop = false;
            tempAudio.Play();
            tempAudio.DOFade(Vol, fadeTime).Restart();
            allAudioSources.Add(tempAudio);
        });
    }

    // 播放循环音频
    public void PlayLoopAudio(string clipName, float fadeTime = 0)
    {
        GetAudioClip(clipName,_ => {
            tempAudio = GetAudioSource();
            tempAudio.volume = 0;
            tempAudio.clip = _;
            tempAudio.loop = true;
            tempAudio.Play();
            tempAudio.DOFade(Vol, fadeTime).Restart();
            allAudioSources.Add(tempAudio);
        });
    }

    // 获取音频文件
    private void GetAudioClip(string clipName,UnityAction<AudioClip> action)
    {
        if(audios.Any(_ => _.name == clipName))
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
    public void StopAudio(string clipName)
    {
        allAudioSources.Where(_ => _.clip.name == clipName).ToList().ForEach(_ => _.Stop());
    }

    // 改变音量
    public void ChangeVolume(float v)
    {
        Vol = v;
        for (int i = 0; i < allAudioSources.Count; i++)
        {
            allAudioSources[i].volume = v;
        }
    }

    public void Mute()
    {
        if(Vol != 0)
            ChangeVolume(0);
        Save();
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(volFile, Vol);
    }
}