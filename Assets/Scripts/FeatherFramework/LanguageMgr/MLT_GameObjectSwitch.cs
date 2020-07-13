using UnityEngine;

public class MLT_GameObjectSwitch : MonoBehaviour
{
    public GameObject CN, EN, Portuguese, Spanish, French, Japanese, Korean, German;
    private void Awake()
    {
        Switch();

        EventCenter.Instance.AddEventListener("语言切换", Switch);
    }

    void Switch()
    {
        CN?.SetActive(false);
        EN?.SetActive(false);
        Portuguese?.SetActive(false);
        Spanish?.SetActive(false);
        French?.SetActive(false);
        Japanese?.SetActive(false);
        Korean?.SetActive(false);
        German?.SetActive(false);

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
                break;
        }
    }

    private void OnDestroy()
    {
        EventCenter.Instance.RemoveEventListener("语言切换", Switch);
    }
}
