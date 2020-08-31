using UnityEngine;

public class GameLanguageManager : Singleton<GameLanguageManager>
{
    public GameLanguageManager()
    {
        currentLanguage = SaveHandler.GetSystemData(languageFile, currentLanguage);
        EventCenter.Instance.EventTrigger("语言切换");
    }

    SupportedLanguage? currentLanguage;
    const string  languageFile = "LanguageFile";
    public enum SupportedLanguage { Chinese, English, Portuguese, Spanish, French, Japanese, Korean, German }

    public SupportedLanguage CurrentLanguage
    {
        get
        {
            if (currentLanguage == null)
            {
                if (Application.systemLanguage == SystemLanguage.Chinese || Application.systemLanguage == SystemLanguage.ChineseSimplified || Application.systemLanguage == SystemLanguage.ChineseTraditional)
                {
                    currentLanguage = SupportedLanguage.Chinese;
                }
                else if (Application.systemLanguage == SystemLanguage.English)
                {
                    currentLanguage = SupportedLanguage.English;
                }
                //else if (Application.systemLanguage == SystemLanguage.Portuguese)
                //{
                //    currentLanguage = SupportedLanguage.Portuguese;
                //}
                //else if (Application.systemLanguage == SystemLanguage.Spanish)
                //{
                //    currentLanguage = SupportedLanguage.Spanish;
                //}
                //else if (Application.systemLanguage == SystemLanguage.French)
                //{
                //    currentLanguage = SupportedLanguage.French;
                //}
                //else if (Application.systemLanguage == SystemLanguage.Japanese)
                //{
                //    currentLanguage = SupportedLanguage.Japanese;
                //}
                //else if (Application.systemLanguage == SystemLanguage.Korean)
                //{
                //    currentLanguage = SupportedLanguage.Korean;
                //}
                //else if (Application.systemLanguage == SystemLanguage.German)
                //{
                //    currentLanguage = SupportedLanguage.German;
                //}
                else
                {
                    currentLanguage = SupportedLanguage.Chinese;
                }
            }
            currentLanguage = SaveHandler.GetSystemData(languageFile, currentLanguage);
            return currentLanguage.Value;
        }

        set 
        { 
            SaveHandler.SetSystemData(languageFile, value,true);
            EventCenter.Instance.EventTrigger("语言切换");
        }
    }
}