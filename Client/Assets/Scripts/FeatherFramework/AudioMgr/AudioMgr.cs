using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public enum AudioType
{
    BGM,
    EFFECT
}

public readonly struct AudioVoiceHandle : IEquatable<AudioVoiceHandle>
{
    internal readonly AudioMgr Owner;
    internal readonly int Id;

    internal AudioVoiceHandle(AudioMgr owner, int id)
    {
        Owner = owner;
        Id = id;
    }

    public bool IsValid => Owner != null && Owner.HasVoice(Id);

    public void Stop(float fadeTime = 0)
    {
        Owner?.Stop(this, fadeTime);
    }

    public bool Equals(AudioVoiceHandle other) => Owner == other.Owner && Id == other.Id;
    public override bool Equals(object obj) => obj is AudioVoiceHandle other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Owner, Id);
}

public sealed class AudioMgr : MonoBehaviour
{
    private sealed class Voice
    {
        public int Id;
        public string ClipName;
        public AudioType Type;
        public bool Loop;
        public bool Cancelled;
        public bool StopRequested;
        public float BaseVolume;
        public float ReservedUntil;
        public AudioSource Source;
        public Tween Fade;
    }

    private readonly List<Voice> completedVoices = new List<Voice>();
    private readonly List<AudioSource> audioSources = new List<AudioSource>();
    private readonly Dictionary<AudioSource, Voice> sourceVoices = new Dictionary<AudioSource, Voice>();
    private readonly Dictionary<int, Voice> voices = new Dictionary<int, Voice>();
    private Action<string, UnityAction<AudioClip>> loadClip;
    private SaveDataMgr save;
    private int voiceId;
    private bool isInitialized;

    private const string Path = "Audios/";
    private const string BgmVolumeKey = "BGMVolSaveData";
    private const string EffectVolumeKey = "EffectVolSaveData";

    private float bgmVolume = 1f;
    private float effectVolume = 1f;

    private float BGMVol
    {
        get => bgmVolume;
        set
        {
            ValidateNumber(value, nameof(value));
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(clamped, bgmVolume))
            {
                return;
            }
            bgmVolume = clamped;
            ChangeVolume(AudioType.BGM, bgmVolume);
        }
    }

    private float EffectVol
    {
        get => effectVolume;
        set
        {
            ValidateNumber(value, nameof(value));
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(clamped, effectVolume))
            {
                return;
            }
            effectVolume = clamped;
            ChangeVolume(AudioType.EFFECT, effectVolume);
        }
    }

    internal void Initialize(ResMgr assets, SaveDataMgr save)
    {
        if (assets == null) throw new ArgumentNullException(nameof(assets));
        Initialize(assets.LoadAsync<AudioClip>, save);
    }

    internal void Initialize(Action<string, UnityAction<AudioClip>> loader, SaveDataMgr save)
    {
        if (isInitialized)
        {
            return;
        }

        loadClip = loader ?? throw new ArgumentNullException(nameof(loader));
        this.save = save ?? throw new ArgumentNullException(nameof(save));
        BGMVol = save.GetSystemData(BgmVolumeKey, 1f);
        EffectVol = save.GetSystemData(EffectVolumeKey, 1f);
        isInitialized = true;
    }

    public AudioVoiceHandle PlayAudio(string clipName, AudioType type, float fadeTime = 0, float delayTime = 0)
    {
        return PlayInternal(clipName, type, false, fadeTime, delayTime);
    }

    public AudioVoiceHandle PlayLoopAudio(string clipName, AudioType type, float fadeTime = 0, float delayTime = 0)
    {
        return PlayInternal(clipName, type, true, fadeTime, delayTime);
    }

    private AudioVoiceHandle PlayInternal(string clipName, AudioType type, bool loop, float fadeTime, float delayTime)
    {
        if (!isInitialized)
        {
            Debug.LogError("Audio service is not initialized.");
            return default;
        }
        if (string.IsNullOrWhiteSpace(clipName))
        {
            throw new ArgumentException("Audio clip name cannot be empty.", nameof(clipName));
        }
        ValidateNumber(fadeTime, nameof(fadeTime));
        ValidateNumber(delayTime, nameof(delayTime));
        if (!Enum.IsDefined(typeof(AudioType), type)) throw new ArgumentOutOfRangeException(nameof(type));
        if (fadeTime < 0 || delayTime < 0)
        {
            throw new ArgumentOutOfRangeException(fadeTime < 0 ? nameof(fadeTime) : nameof(delayTime));
        }

        if (type == AudioType.BGM)
        {
            StopAll(AudioType.BGM);
        }

        var voice = new Voice
        {
            Id = ++voiceId,
            ClipName = clipName,
            Type = type,
            Loop = loop,
            BaseVolume = 1f
        };
        voices.Add(voice.Id, voice);

        GetAudioClip(clipName, clip => StartVoice(voice, clip, fadeTime, delayTime));
        return new AudioVoiceHandle(this, voice.Id);
    }

    private void StartVoice(Voice voice, AudioClip clip, float fadeTime, float delayTime)
    {
        if (!voices.ContainsKey(voice.Id) || voice.Cancelled)
        {
            return;
        }
        if (clip == null)
        {
            Debug.LogError($"Audio clip not found: {voice.ClipName}");
            RemoveVoice(voice);
            return;
        }

        var source = GetAudioSource();
        if (sourceVoices.TryGetValue(source, out var oldVoice))
        {
            RemoveVoice(oldVoice);
        }

        voice.Source = source;
        sourceVoices[source] = voice;
        source.clip = clip;
        source.loop = voice.Loop;
        source.playOnAwake = false;
        voice.BaseVolume = 1f;
        source.volume = fadeTime > 0 ? 0 : GetChannelVolume(voice.Type);
        voice.ReservedUntil = Time.unscaledTime + delayTime;
        source.PlayDelayed(delayTime);

        if (fadeTime > 0)
        {
            voice.Fade = DOTween.To(
                    () => source.volume,
                    value => source.volume = value,
                    GetChannelVolume(voice.Type),
                    fadeTime)
                .SetUpdate(true)
                .SetTarget(source)
                .SetDelay(delayTime);
        }
    }

    private void GetAudioClip(string clipName, UnityAction<AudioClip> callback)
    {
        if (loadClip == null)
        {
            callback?.Invoke(null);
            return;
        }
        loadClip(Path + clipName, callback);
    }

    private AudioSource GetAudioSource()
    {
        float now = Time.unscaledTime;
        for (int i = 0; i < audioSources.Count; i++)
        {
            var source = audioSources[i];
            if (source != null && !source.isPlaying && sourceVoices.TryGetValue(source, out var voice)
                && (AudioListener.pause || voice.ReservedUntil > now))
            {
                continue;
            }
            if (source != null && !source.isPlaying)
            {
                return source;
            }
        }

        var newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        newSource.spatialBlend = 0;
        audioSources.Add(newSource);
        return newSource;
    }

    internal void Stop(AudioVoiceHandle handle, float fadeTime = 0)
    {
        ValidateNumber(fadeTime, nameof(fadeTime));
        if (fadeTime < 0) throw new ArgumentOutOfRangeException(nameof(fadeTime));
        if (handle.Owner != this || !voices.TryGetValue(handle.Id, out var voice))
        {
            return;
        }
        voice.Cancelled = true;
        if (voice.Source == null)
        {
            RemoveVoice(voice);
            return;
        }

        KillFade(voice);
        if (fadeTime > 0 && voice.Source.isPlaying)
        {
            voice.StopRequested = true;
            voice.Fade = DOTween.To(() => voice.Source.volume, value => voice.Source.volume = value, 0, fadeTime)
                .SetUpdate(true)
                .SetTarget(voice.Source)
                .OnComplete(() => FinishStop(voice));
        }
        else
        {
            FinishStop(voice);
        }
    }

    private void FinishStop(Voice voice)
    {
        if (voice.Source != null)
        {
            voice.Source.Stop();
            voice.Source.clip = null;
        }
        RemoveVoice(voice);
    }

    internal void StopAudio(string clipName, float fadeTime = 0)
    {
        var matchingVoices = new List<Voice>();
        foreach (var voice in voices.Values)
        {
            if (voice.ClipName == clipName)
            {
                matchingVoices.Add(voice);
            }
        }
        foreach (var voice in matchingVoices)
        {
            Stop(new AudioVoiceHandle(this, voice.Id), fadeTime);
        }
    }

    public void StopAll(AudioType? type = null)
    {
        var activeVoices = new List<Voice>(voices.Values);
        foreach (var voice in activeVoices)
        {
            if (!type.HasValue || voice.Type == type.Value)
            {
                Stop(new AudioVoiceHandle(this, voice.Id));
            }
        }
    }

    private void RemoveVoice(Voice voice)
    {
        KillFade(voice);
        if (voice.Source != null && sourceVoices.TryGetValue(voice.Source, out var sourceVoice)
            && ReferenceEquals(sourceVoice, voice))
        {
            sourceVoices.Remove(voice.Source);
        }
        voices.Remove(voice.Id);
        voice.Source = null;
    }

    private static void KillFade(Voice voice)
    {
        if (voice.Fade != null)
        {
            voice.Fade.Kill();
            voice.Fade = null;
        }
        if (voice.Source != null)
        {
            DOTween.Kill(voice.Source);
        }
    }

    private void ChangeVolume(AudioType type, float volume)
    {
        foreach (var voice in voices.Values)
        {
            if (voice.Type != type || voice.Source == null)
            {
                continue;
            }
            if (voice.StopRequested)
            {
                continue;
            }
            KillFade(voice);
            voice.Source.volume = voice.BaseVolume * volume;
        }
    }

    internal bool HasVoice(int id)
    {
        return id > 0 && voices.ContainsKey(id);
    }

    private float GetChannelVolume(AudioType type)
    {
        return type == AudioType.BGM ? BGMVol : EffectVol;
    }

    private void Update()
    {
        if (voices.Count == 0 || AudioListener.pause)
        {
            return;
        }

        float now = Time.unscaledTime;
        completedVoices.Clear();
        foreach (var voice in voices.Values)
        {
            if (!voice.Loop && !voice.StopRequested && voice.Source != null
                && now >= voice.ReservedUntil && !voice.Source.isPlaying)
            {
                completedVoices.Add(voice);
            }
        }
        foreach (var voice in completedVoices)
        {
            if (voice.Source != null)
            {
                voice.Source.clip = null;
            }
            RemoveVoice(voice);
        }
    }

    public void MuteBG()
    {
        BGMVol = 0;
        SaveBGMVolume();
    }

    public void MuteEffect()
    {
        EffectVol = 0;
        SaveEffectVolume();
    }

    public void SetBGMVolume(float volume)
    {
        BGMVol = volume;
    }

    public void SetEffectVolume(float volume)
    {
        EffectVol = volume;
    }

    public void Save()
    {
        SaveBGMVolume();
        SaveEffectVolume();
    }

    public void SaveBGMVolume()
    {
        save?.SetSystemData(BgmVolumeKey, BGMVol, true);
    }

    public void SaveEffectVolume()
    {
        save?.SetSystemData(EffectVolumeKey, EffectVol, true);
    }

    internal void Shutdown()
    {
        isInitialized = false;
        StopAll();
        foreach (var source in audioSources)
        {
            if (source == null) continue;
            source.Stop();
            source.clip = null;
            if (Application.isPlaying) Destroy(source);
            else DestroyImmediate(source);
        }
        audioSources.Clear();
        sourceVoices.Clear();
        loadClip = null;
        save = null;
    }

    private static void ValidateNumber(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(name);
    }

    private void OnDestroy() => Shutdown();
}
