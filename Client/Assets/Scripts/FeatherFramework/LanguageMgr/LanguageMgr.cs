using System;
using UnityEngine;

public sealed class LanguageMgr
{
    private const string LegacySaveKey = "LanguageSaveData";
    internal const string PreferenceSaveKey = "LanguageSaveData.v1";
    private readonly SaveDataMgr save;
    private readonly ConfigMgr config;
    private readonly EventCenter events;
    private SystemLanguage currentLanguage;
    private bool followsSystemLanguage;
    private bool stopped;

    internal sealed class Preference
    {
        public int Version = 1;
        public bool FollowSystem;
        public string Language;
    }

    internal LanguageMgr(SaveDataMgr save, ConfigMgr config, EventCenter events, GameConfig gameConfig)
    {
        this.save = save;
        this.config = config;
        this.events = events;
        var preference = save.GetSystemData<Preference>(PreferenceSaveKey, null);
        if (preference != null && preference.Version == 1 &&
            Enum.TryParse(preference.Language, out SystemLanguage stored) && Enum.IsDefined(typeof(SystemLanguage), stored))
        {
            followsSystemLanguage = preference.FollowSystem;
            currentLanguage = Normalize(followsSystemLanguage ? Application.systemLanguage : stored);
        }
        else
        {
            int legacy = save.GetSystemData(LegacySaveKey, 0);
            followsSystemLanguage = legacy == 0 && gameConfig.followSystemLanguage;
            currentLanguage = legacy > 0 && legacy <= 5
                ? FromLegacyLanguage(legacy)
                : Normalize(followsSystemLanguage ? Application.systemLanguage : gameConfig.defaultLanguage);
            // A new key prevents numeric enum collisions; preserve unknown versions and read-only saves.
            if (preference == null && !save.IsReadOnly) SavePreference(currentLanguage, followsSystemLanguage, false);
        }
    }

    public SystemLanguage CurrentLanguage
    {
        get { EnsureOpen(); return currentLanguage; }
        set
        {
            EnsureOpen();
            if (!Enum.IsDefined(typeof(SystemLanguage), value)) throw new ArgumentOutOfRangeException(nameof(value));
            SetLanguage(Normalize(value), false);
        }
    }

    public bool FollowsSystemLanguage { get { EnsureOpen(); return followsSystemLanguage; } }

    /// <summary>Resolve the system language now and remember this choice for the next startup.</summary>
    public void FollowSystemLanguage()
    {
        EnsureOpen();
        SetLanguage(Normalize(Application.systemLanguage), true);
    }

    private void SetLanguage(SystemLanguage language, bool followSystem)
    {
        if (language == currentLanguage && followSystem == followsSystemLanguage) return;
        SavePreference(language, followSystem, true);
        bool changed = language != currentLanguage;
        currentLanguage = language;
        followsSystemLanguage = followSystem;
        if (changed) events.Publish(FrameworkEvents.LanguageChangedId);
    }

    private void SavePreference(SystemLanguage language, bool followSystem, bool immediately)
    {
        save.SetSystemData(PreferenceSaveKey, new Preference
        {
            Language = language.ToString(), FollowSystem = followSystem
        }, immediately);
    }

    public string GetLanguageById(int id) => GetLanguageById(id, out _);

    /// <summary>Returns logical-order text and the language actually used after fallback.</summary>
    public string GetLanguageById(int id, out SystemLanguage resolvedLanguage)
    {
        EnsureOpen();
        resolvedLanguage = currentLanguage;
        if (config.Tables.Language.DataMap.TryGetValue(id, out var row))
        {
            string value = LanguageTableAccess.Get(row, currentLanguage);
            if (!string.IsNullOrEmpty(value)) return value;
            resolvedLanguage = SystemLanguage.English;
            return string.IsNullOrEmpty(row.English) ? id.ToString(System.Globalization.CultureInfo.InvariantCulture) : row.English;
        }
        resolvedLanguage = SystemLanguage.English;
        Debug.LogError($"Cannot find language id: {id} in Language.xlsx.");
        return id.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static SystemLanguage Normalize(SystemLanguage language)
    {
        if (language == SystemLanguage.Chinese) return SystemLanguage.ChineseSimplified;
        return language == SystemLanguage.Unknown || !Enum.IsDefined(typeof(SystemLanguage), language)
            ? SystemLanguage.English : language;
    }

    internal static SystemLanguage FromLegacyLanguage(int language) => language switch
    {
        1 => SystemLanguage.ChineseSimplified,
        2 => SystemLanguage.ChineseTraditional,
        3 => SystemLanguage.English,
        4 => SystemLanguage.Japanese,
        5 => SystemLanguage.Korean,
        _ => SystemLanguage.English
    };

    internal void Shutdown() { stopped = true; }
    private void EnsureOpen() { if (stopped) throw new ObjectDisposedException(nameof(LanguageMgr)); }
}
