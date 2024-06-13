using Newtonsoft.Json.Linq;
using UnityEngine;

public class LanguageMgr : Singleton<LanguageMgr>
{
    public LanguageMgr()
    {
        //读取存储语言
        currentLanguage = SaveDataMgr.GetSystemData(languageFile, currentLanguage);
    }

    SupportedLanguage currentLanguage = SupportedLanguage.Default;
    const string  languageFile = "LanguageSaveData";
    public enum SupportedLanguage {Default,ChineseSimplified, ChineseTraditional, English, Japanese, Korean }

    public SupportedLanguage CurrentLanguage
    {
        get
        {
            if (currentLanguage == SupportedLanguage.Default)
            {
                //判断配置语言
                //currentLanguage = ;
                if (currentLanguage != SupportedLanguage.Default)
                {
                    return currentLanguage;
                }
                //根据设备语言切换
                if (Application.systemLanguage == SystemLanguage.ChineseSimplified)
                {
                    currentLanguage = SupportedLanguage.ChineseSimplified;
                }
                else if(Application.systemLanguage == SystemLanguage.ChineseTraditional)
                {
                    currentLanguage = SupportedLanguage.ChineseTraditional;
                }
                else if (Application.systemLanguage == SystemLanguage.Chinese)
                {
                    currentLanguage = SupportedLanguage.ChineseSimplified;
                }
                else if (Application.systemLanguage == SystemLanguage.English)
                {
                    currentLanguage = SupportedLanguage.English;
                }
                else if (Application.systemLanguage == SystemLanguage.Japanese)
                {
                    currentLanguage = SupportedLanguage.Japanese;
                }
                else if (Application.systemLanguage == SystemLanguage.Korean)
                {
                    currentLanguage = SupportedLanguage.Korean;
                }
                else
                {
                    currentLanguage = SupportedLanguage.English;
                }
                SaveDataMgr.SetSystemData(languageFile, currentLanguage);
            }
            return currentLanguage;
        }

        set 
        { 
            SaveDataMgr.SetSystemData(languageFile, value,true);
            EventCenter.Instance.EventTrigger("LanguageSwitch");
        }
    }

    public string GetLanguageById(int id)
    {
        if (ConfigMgr.Config.Language.DataMap.ContainsKey(id))
        {
            var languageData = ConfigMgr.Config.Language.Get(id);
            if (Application.systemLanguage == SystemLanguage.ChineseSimplified)
            {
                return languageData.ChineseSimplified;
            }
            else if (Application.systemLanguage == SystemLanguage.ChineseTraditional)
            {
                return languageData.ChineseTraditional;
            }
            else if (Application.systemLanguage == SystemLanguage.Chinese)
            {
                return languageData.ChineseSimplified;
            }
            else if (Application.systemLanguage == SystemLanguage.English)
            {
                return languageData.English;
            }
            else if (Application.systemLanguage == SystemLanguage.Japanese)
            {
                return languageData.Japanese;
            }
            else if (Application.systemLanguage == SystemLanguage.Korean)
            {
                return languageData.Korean;
            }
            else
            {
                return languageData.English;
            }
        }
        else
        {
            Debug.LogError($"cant find id:{id} in config,please check!");
            return $"{id}";
        }
    }
}