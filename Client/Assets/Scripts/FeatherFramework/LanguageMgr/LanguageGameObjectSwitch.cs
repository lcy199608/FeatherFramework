using UnityEngine;

public class LanguageGameObjectSwitch : MonoBehaviour
{
    public GameObject CN_S, CN_T, EN, JA, KO;
    private void Start()
    {
        Switch();
        EventCenter.Instance.AddEventListener("LanguageSwitch", Switch);
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

        switch (LanguageMgr.Instance.CurrentLanguage)
        {
            case LanguageMgr.SupportedLanguage.ChineseSimplified:
                CN_S?.SetActive(true);
                break;
            case LanguageMgr.SupportedLanguage.ChineseTraditional:
                CN_T?.SetActive(true);
                break;
            case LanguageMgr.SupportedLanguage.English:
                EN?.SetActive(true);
                break;
            case LanguageMgr.SupportedLanguage.Japanese:
                JA?.SetActive(true);
                break;
            case LanguageMgr.SupportedLanguage.Korean:
                KO?.SetActive(true);
                break;
        }
    }

    private void OnDestroy()
    {
        EventCenter.Instance.RemoveEventListener("LanguageSwitch", Switch);
    }
}
