using UnityEngine;

public sealed class LanguageMgr
{
    private readonly SaveDataMgr save;
    private readonly ConfigMgr config;
    private readonly EventCenter events;
    private readonly GameConfig gameConfig;

    internal LanguageMgr(SaveDataMgr save, ConfigMgr config, EventCenter events, GameConfig gameConfig)
    {
        this.save = save;
        this.config = config;
        this.events = events;
        this.gameConfig = gameConfig;
        //读取存储语言
        currentLanguage = save.GetSystemData(languageFile, SupportedLanguage.Default);
        if (currentLanguage == SupportedLanguage.Default)
        {
            currentLanguage = ResolveDefaultLanguage();
            save.SetSystemData(languageFile, currentLanguage);
        }
    }

    private bool stopped;
    internal void Shutdown() { stopped = true; }
    private void EnsureOpen() { if (stopped) throw new System.ObjectDisposedException(nameof(LanguageMgr)); }

    SupportedLanguage currentLanguage = SupportedLanguage.Default;
    const string  languageFile = "LanguageSaveData";
    public enum SupportedLanguage {Default,ChineseSimplified, ChineseTraditional, English, Japanese, Korean }

    public SupportedLanguage CurrentLanguage
    {
        get { EnsureOpen(); return currentLanguage; }

        set 
        {
            EnsureOpen();
            if (value == SupportedLanguage.Default)
            {
                value = ResolveDefaultLanguage();
            }
            if (currentLanguage == value)
            {
                return;
            }
            currentLanguage = value;
            save.SetSystemData(languageFile, value,true);
            events.Publish(FrameworkEvents.LanguageChangedId);
        }
    }

    public string GetLanguageById(int id)
    {
        EnsureOpen();
        if (config.Tables.Language.DataMap.TryGetValue(id, out var languageData))
        {
            string localizedText = CurrentLanguage switch
            {
                SupportedLanguage.ChineseSimplified => languageData.ChineseSimplified,
                SupportedLanguage.ChineseTraditional => languageData.ChineseTraditional,
                SupportedLanguage.Japanese => languageData.Japanese,
                SupportedLanguage.Korean => languageData.Korean,
                _ => languageData.English
            };
            return string.IsNullOrEmpty(localizedText)
                ? (languageData.English ?? id.ToString())
                : localizedText;
        }
        else
        {
            Debug.LogError($"cant find id:{id} in config,please check!");
            return $"{id}";
        }
    }

    private SupportedLanguage ResolveDefaultLanguage()
    {
        if (gameConfig.language != SupportedLanguage.Default)
        {
            return gameConfig.language;
        }

        switch (Application.systemLanguage)
        {
            case SystemLanguage.ChineseSimplified:
            case SystemLanguage.Chinese:
                return SupportedLanguage.ChineseSimplified;
            case SystemLanguage.ChineseTraditional:
                return SupportedLanguage.ChineseTraditional;
            case SystemLanguage.Japanese:
                return SupportedLanguage.Japanese;
            case SystemLanguage.Korean:
                return SupportedLanguage.Korean;
            default:
                return SupportedLanguage.English;
        }
    }
}
