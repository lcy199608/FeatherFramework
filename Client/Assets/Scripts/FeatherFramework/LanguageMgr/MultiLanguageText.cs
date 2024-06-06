using System;
using UnityEngine;

[Serializable]
public class MultiLanguageText
{
    [TextArea]
    public string 中文, English, 葡语, 西语, 法语, 日语, 韩语, 德语;

    public string Text
    {
        get
        {
            switch (GameLanguageManager.Instance.CurrentLanguage)
            {
                case GameLanguageManager.SupportedLanguage.Chinese:
                    return 中文;
                case GameLanguageManager.SupportedLanguage.English:
                    return English;
                case GameLanguageManager.SupportedLanguage.Portuguese:
                    return 葡语;
                case GameLanguageManager.SupportedLanguage.Spanish:
                    return 西语;
                case GameLanguageManager.SupportedLanguage.French:
                    return 法语;
                case GameLanguageManager.SupportedLanguage.Japanese:
                    return 日语;
                case GameLanguageManager.SupportedLanguage.Korean:
                    return 韩语;
                case GameLanguageManager.SupportedLanguage.German:
                    return 德语;
                default:
                    return English;
            }
        }
    }
}
