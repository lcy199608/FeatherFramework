using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum AudioType
{
    Ambient, //背景音乐
    Effect //音效
}

public class AudioMgr : SingletonMono<AudioMgr>
{
    const string effectVolFile = "EffectVol";
    const string ambientVolFile = "AmbientVol";

    private AudioSource tempAudio;
    public List<AudioClip> audios;
    private List<AudioSource> allAudioSources = new List<AudioSource>();
    private Dictionary<string, AudioSource> loopAudioSources = new Dictionary<string, AudioSource>();
    private List<AudioSource> ambientAudios = new List<AudioSource>();
    private List<AudioSource> effectAudios = new List<AudioSource>();

    float effectVol;
    float ambientVol;

    public float EffectVol
    {
        get { return effectVol; }
        set { effectVol = value; }
    }
    public float AmbientVol
    {
        get { return ambientVol; }
        set { ambientVol = value; }
    }

    private void Awake()
    {
        effectVol = PlayerPrefs.GetFloat(effectVolFile, 1.0f);
        ambientVol = PlayerPrefs.GetFloat(ambientVolFile, 1.0f);
    }

    // 播放音效
    public void PlayAudio(string clipName, float delayTime = 0)
    {
        ClearAudioSources();
        var tempClip = GetAudioClip(clipName);
        if(tempClip == null)
        {
            return;
        }
        tempAudio.clip = tempClip;
        tempAudio = GetAudioSource();
        tempAudio.loop = false;
        tempAudio.PlayDelayed(delayTime);
        tempAudio.volume = EffectVol;
        allAudioSources.Add(tempAudio);
        effectAudios.Add(tempAudio);
    }

    // 播放循环音频
    public void PlayLoopAudio(string clipName, AudioType type = AudioType.Ambient, float delayTime = 0)
    {
        ClearAudioSources();
        if (loopAudioSources.ContainsKey(clipName))
            return;
        var tempClip = GetAudioClip(clipName);
        if (tempClip == null)
        {
            return;
        }
        tempAudio.clip = tempClip;
        tempAudio = GetAudioSource();
        tempAudio.loop = true;
        tempAudio.PlayDelayed(delayTime);
        loopAudioSources.Add(clipName, tempAudio);
        switch (type)
        {
            case AudioType.Effect:
                tempAudio.volume = EffectVol;
                effectAudios.Add(tempAudio);
                break;
            case AudioType.Ambient:
                tempAudio.volume = AmbientVol;
                ambientAudios.Add(tempAudio);
                break;
        }
    }

    // 获取音频文件
    private AudioClip GetAudioClip(string clipName)
    {
        if(audios.Any(_ => _.name == clipName))
        {
            return audios.First(_ => _.name == clipName);
        }
        else
        {
            Debug.LogError("没有找到对应音频文件！");
            return null;
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
        tempAudio.spatialBlend = 0;
        return tempAudio;
    }

    private void ClearAudioSources()
    {
        if(ambientAudios.Any(_ => !_.isPlaying))
        {
            ambientAudios = ambientAudios.Where(_ => _.isPlaying).ToList();
        }

        if (effectAudios.Any(_ => !_.isPlaying))
        {
            effectAudios = effectAudios.Where(_ => _.isPlaying).ToList();
        }
    }

    // 停止某个循环的音频
    public void StopLoopAudio(string clipName)
    {
        if (!loopAudioSources.ContainsKey(clipName) || loopAudioSources[clipName] == null)
            return;
        tempAudio = loopAudioSources[clipName];
        tempAudio.Stop();
        tempAudio.loop = false;
        if (ambientAudios.Contains(tempAudio))
            ambientAudios.Remove(tempAudio);
        if (effectAudios.Contains(tempAudio))
            effectAudios.Remove(tempAudio);
        loopAudioSources.Remove(clipName);
    }

    // 循环音频全部停止播放
    public void StopAllLoopAudio()
    {
        ClearAudioSources();
        loopAudioSources.Where(_ => _.Value.isPlaying == true && _.Value.loop).ToList().ForEach(_ => StopLoopAudio(_.Key));
    }

    // 停止全部
    public void StopAllAudio()
    {
        ClearAudioSources();
        allAudioSources.Where(_ => _.isPlaying).ToList().ForEach(_ => _.Stop());
    }

    // 改变音量
    public void ChangeEffectVolume(float v)
    {
        EffectVol = v;
        ClearAudioSources();
        for (int i = 0; i < effectAudios.Count; i++)
        {
            effectAudios[i].volume = v;
        }
    }
    public void ChangeAmbientVolume(float v)
    {
        AmbientVol = v;
        ClearAudioSources();
        for (int i = 0; i < ambientAudios.Count; i++)
        {
            ambientAudios[i].volume = v;
        }
    }

    public void Mute()
    {
        if(EffectVol != 0)
            ChangeEffectVolume(0);
        if(AmbientVol != 0)
            ChangeAmbientVolume(0);
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(effectVolFile, effectVol);
        PlayerPrefs.SetFloat(ambientVolFile, ambientVol);
    }
}