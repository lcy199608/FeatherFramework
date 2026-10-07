using System;
using System.Collections.Generic;
using UnityEngine;

public class LanguageGameObjectSwitch : FrameworkBehaviour
{
    [Serializable]
    public sealed class Variant
    {
        public SystemLanguage language = SystemLanguage.English;
        public GameObject gameObject;
    }

    public GameObject CN_S, CN_T, EN, JA, KO;
    public Variant[] variants = Array.Empty<Variant>();
    private IDisposable languageSubscription;
    private readonly HashSet<GameObject> objects = new HashSet<GameObject>();

    private void Start()
    {
        if (!Services.HasLocalization) return;
        RefreshLanguage();
        languageSubscription = Services.Events.Subscribe(FrameworkEvents.LanguageChangedId, RefreshLanguage);
    }

    public void RefreshLanguage()
    {
        if (Services.HasLocalization) ApplyLanguage(Services.Localization.CurrentLanguage);
    }

    internal void ApplyLanguage(SystemLanguage language)
    {
        objects.Clear();
        objects.Add(CN_S); objects.Add(CN_T); objects.Add(EN); objects.Add(JA); objects.Add(KO);
        if (variants != null) foreach (var variant in variants) if (variant != null) objects.Add(variant.gameObject);
        var target = Find(language) ?? Legacy(language) ?? Find(SystemLanguage.English) ?? EN ?? CN_S ?? CN_T ?? JA ?? KO;
        if (target == null && variants != null)
            foreach (var variant in variants) if (variant?.gameObject != null) { target = variant.gameObject; break; }
        foreach (var item in objects) if (item != null && item != target && item.activeSelf) item.SetActive(false);
        if (target != null && !target.activeSelf) target.SetActive(true);
    }

    private GameObject Find(SystemLanguage language)
    {
        if (variants != null)
            foreach (var variant in variants)
                if (variant?.gameObject != null && LanguageMgr.Normalize(variant.language) == LanguageMgr.Normalize(language)) return variant.gameObject;
        return null;
    }

    private GameObject Legacy(SystemLanguage language) => LanguageMgr.Normalize(language) switch
    {
        SystemLanguage.ChineseSimplified => CN_S,
        SystemLanguage.ChineseTraditional => CN_T,
        SystemLanguage.English => EN,
        SystemLanguage.Japanese => JA,
        SystemLanguage.Korean => KO,
        _ => null
    };

    private void OnDestroy() { languageSubscription?.Dispose(); languageSubscription = null; objects.Clear(); }
}
