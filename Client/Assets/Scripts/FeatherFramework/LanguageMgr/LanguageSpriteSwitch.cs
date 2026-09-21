using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LanguageSpriteSwitch : FrameworkBehaviour
{
    Image image;
    private System.IDisposable languageSubscription;
    public Sprite CN_S, CN_T, EN, JA, KO;
    private void Start()
    {
        if (!Services.HasLocalization) return;
        image = GetComponent<Image>();
        Switch();
        languageSubscription = Services.Events.Subscribe(FrameworkEvents.LanguageChangedId, Switch);
    }

    void Switch()
    {
        Sprite target = Services.Localization.CurrentLanguage switch
        {
            LanguageMgr.SupportedLanguage.ChineseSimplified => CN_S,
            LanguageMgr.SupportedLanguage.ChineseTraditional => CN_T,
            LanguageMgr.SupportedLanguage.Japanese => JA,
            LanguageMgr.SupportedLanguage.Korean => KO,
            _ => EN
        };
        image.sprite = target ?? EN ?? CN_S ?? CN_T ?? JA ?? KO;
    }

    private void OnDestroy()
    {
        languageSubscription?.Dispose();
        languageSubscription = null;
    }
}
