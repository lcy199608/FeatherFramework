using System;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.Image))]
public class LanguageSpriteSwitch : FrameworkBehaviour
{
    [Serializable]
    public sealed class Variant
    {
        public SystemLanguage language = SystemLanguage.English;
        public Sprite sprite;
    }

    // Legacy serialized references remain valid; variants take precedence.
    public Sprite CN_S, CN_T, EN, JA, KO;
    public Variant[] variants = Array.Empty<Variant>();
    private UnityEngine.UI.Image image;
    private IDisposable languageSubscription;

    private void Start()
    {
        if (!Services.HasLocalization) return;
        image = GetComponent<UnityEngine.UI.Image>();
        RefreshLanguage();
        languageSubscription = Services.Events.Subscribe(FrameworkEvents.LanguageChangedId, RefreshLanguage);
    }

    public void RefreshLanguage()
    {
        if (!Services.HasLocalization) return;
        if (image == null) image = GetComponent<UnityEngine.UI.Image>();
        image.sprite = Resolve(Services.Localization.CurrentLanguage);
    }

    internal Sprite Resolve(SystemLanguage language)
    {
        var target = Find(language) ?? Legacy(language) ?? Find(SystemLanguage.English) ?? EN ?? CN_S ?? CN_T ?? JA ?? KO;
        if (target == null && variants != null)
            foreach (var variant in variants) if (variant?.sprite != null) return variant.sprite;
        return target;
    }

    private Sprite Find(SystemLanguage language)
    {
        if (variants != null)
            foreach (var variant in variants)
                if (variant?.sprite != null && LanguageMgr.Normalize(variant.language) == LanguageMgr.Normalize(language)) return variant.sprite;
        return null;
    }

    private Sprite Legacy(SystemLanguage language) => LanguageMgr.Normalize(language) switch
    {
        SystemLanguage.ChineseSimplified => CN_S,
        SystemLanguage.ChineseTraditional => CN_T,
        SystemLanguage.English => EN,
        SystemLanguage.Japanese => JA,
        SystemLanguage.Korean => KO,
        _ => null
    };

    private void OnDestroy() { languageSubscription?.Dispose(); languageSubscription = null; }
}
