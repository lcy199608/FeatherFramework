using UnityEngine;

public class LanguageGameObjectSwitch : FrameworkBehaviour
{
    public GameObject CN_S, CN_T, EN, JA, KO;
    private System.IDisposable languageSubscription;
    private void Start()
    {
        if (!Services.HasLocalization) return;
        Switch();
        languageSubscription = Services.Events.Subscribe(FrameworkEvents.LanguageChangedId, Switch);
    }

    void Switch()
    {
        if (CN_S != null)
        {
            CN_S.SetActive(false);
        }

        if (CN_T != null)
        {
            CN_T.SetActive(false);
        }

        if (EN != null)
        {
            EN.SetActive(false);
        }

        if (JA != null)
        {
            JA.SetActive(false);
        }

        if (KO != null)
        {
            KO.SetActive(false);
        }

        GameObject target = Services.Localization.CurrentLanguage switch
        {
            LanguageMgr.SupportedLanguage.ChineseSimplified => CN_S,
            LanguageMgr.SupportedLanguage.ChineseTraditional => CN_T,
            LanguageMgr.SupportedLanguage.Japanese => JA,
            LanguageMgr.SupportedLanguage.Korean => KO,
            _ => EN
        };
        (target ?? EN ?? CN_S ?? CN_T ?? JA ?? KO)?.SetActive(true);
    }

    private void OnDestroy()
    {
        languageSubscription?.Dispose();
        languageSubscription = null;
    }
}
