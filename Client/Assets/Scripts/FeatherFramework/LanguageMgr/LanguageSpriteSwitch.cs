using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class LanguageSpriteSwitch : MonoBehaviour
{
    Image image;
    public Sprite CN_S, CN_T, EN, JA, KO;
    private void Start()
    {
        image = GetComponent<Image>();
        Switch();
        EventCenter.Instance.AddEventListener("LanguageSwitch", Switch);
    }

    void Switch()
    {
        switch (LanguageMgr.Instance.CurrentLanguage)
        {
            case LanguageMgr.SupportedLanguage.ChineseSimplified:
                if(CN_S!= null)
                {
                    image.sprite = CN_S;
                }
                break;
            case LanguageMgr.SupportedLanguage.ChineseTraditional:
                if(CN_T!= null)
                {
                    image.sprite = CN_T;
                }
                break;
            case LanguageMgr.SupportedLanguage.English:
                if(EN!= null)
                {
                    image.sprite = EN;
                }
                break;
            case LanguageMgr.SupportedLanguage.Japanese:
                if(JA!= null)
                {
                    image.sprite = JA;
                }
                break;
            case LanguageMgr.SupportedLanguage.Korean:
                if(KO!= null)
                {
                    image.sprite = KO;
                }
                break;
        }
    }

    private void OnDestroy()
    {
        EventCenter.Instance.RemoveEventListener("LanguageSwitch", Switch);
    }
}
