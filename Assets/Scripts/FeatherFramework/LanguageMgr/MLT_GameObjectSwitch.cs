using UnityEngine;

public class MLT_GameObjectSwitch : MonoBehaviour
{
    public GameObject CN, EN, Portuguese, Spanish, French, Japanese, Korean, German;
    private void Start()
    {
        Switch();

        EventCenter.Instance.AddEventListener("语言切换", Switch);
    }

    void Switch()
    {
        if (CN != null)
        {
            CN.SetActive(false);
        }

        if (EN != null)
        {
            EN.SetActive(false);
        }

        if (Portuguese != null)
        {
            Portuguese.SetActive(false);
        }

        if (Spanish != null)
        {
            Spanish.SetActive(false);
        }

        if (French != null)
        {
            French.SetActive(false);
        }

        if (Japanese != null)
        {
            Japanese.SetActive(false);
        }

        if (Korean != null)
        {
            Korean.SetActive(false);
        }

        if (German != null)
        {
            German.SetActive(false);
        }

        switch (GameLanguageManager.Instance.CurrentLanguage)
        {
            case GameLanguageManager.SupportedLanguage.Chinese:
                CN?.SetActive(true);
                break;
            case GameLanguageManager.SupportedLanguage.English:
                EN?.SetActive(true);
                break;
            case GameLanguageManager.SupportedLanguage.Portuguese:
                Portuguese?.SetActive(true);
                break;
            case GameLanguageManager.SupportedLanguage.Spanish:
                Spanish?.SetActive(true);
                break;
            case GameLanguageManager.SupportedLanguage.French:
                French?.SetActive(true);
                break;
            case GameLanguageManager.SupportedLanguage.Japanese:
                Japanese?.SetActive(true);
                break;
            case GameLanguageManager.SupportedLanguage.Korean:
                Korean?.SetActive(true);
                break;
            case GameLanguageManager.SupportedLanguage.German:
                German?.SetActive(true);
                break;
            default:
                CN?.SetActive(true);
                break;
        }
    }

    private void OnDestroy()
    {
        EventCenter.Instance.RemoveEventListener("语言切换", Switch);
    }
}
