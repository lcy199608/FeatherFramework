using UnityEngine;

// Strongly typed access: a removed/renamed Excel column fails compilation.
internal static class LanguageTableAccess
{
    internal static string Get(cfg.LanguageInfo row, SystemLanguage language) => language switch
    {
        SystemLanguage.Afrikaans => row.Afrikaans,
        SystemLanguage.Arabic => row.Arabic,
        SystemLanguage.Basque => row.Basque,
        SystemLanguage.Belarusian => row.Belarusian,
        SystemLanguage.Bulgarian => row.Bulgarian,
        SystemLanguage.Catalan => row.Catalan,
        SystemLanguage.Czech => row.Czech,
        SystemLanguage.Danish => row.Danish,
        SystemLanguage.Dutch => row.Dutch,
        SystemLanguage.English => row.English,
        SystemLanguage.Estonian => row.Estonian,
        SystemLanguage.Faroese => row.Faroese,
        SystemLanguage.Finnish => row.Finnish,
        SystemLanguage.French => row.French,
        SystemLanguage.German => row.German,
        SystemLanguage.Greek => row.Greek,
        SystemLanguage.Hebrew => row.Hebrew,
        SystemLanguage.Icelandic => row.Icelandic,
        SystemLanguage.Indonesian => row.Indonesian,
        SystemLanguage.Italian => row.Italian,
        SystemLanguage.Japanese => row.Japanese,
        SystemLanguage.Korean => row.Korean,
        SystemLanguage.Latvian => row.Latvian,
        SystemLanguage.Lithuanian => row.Lithuanian,
        SystemLanguage.Norwegian => row.Norwegian,
        SystemLanguage.Polish => row.Polish,
        SystemLanguage.Portuguese => row.Portuguese,
        SystemLanguage.Romanian => row.Romanian,
        SystemLanguage.Russian => row.Russian,
        SystemLanguage.SerboCroatian => row.SerboCroatian,
        SystemLanguage.Slovak => row.Slovak,
        SystemLanguage.Slovenian => row.Slovenian,
        SystemLanguage.Spanish => row.Spanish,
        SystemLanguage.Swedish => row.Swedish,
        SystemLanguage.Thai => row.Thai,
        SystemLanguage.Turkish => row.Turkish,
        SystemLanguage.Ukrainian => row.Ukrainian,
        SystemLanguage.Vietnamese => row.Vietnamese,
        SystemLanguage.ChineseSimplified => row.ChineseSimplified,
        SystemLanguage.ChineseTraditional => row.ChineseTraditional,
        SystemLanguage.Hindi => row.Hindi,
        SystemLanguage.Hungarian => row.Hungarian,
        SystemLanguage.Chinese => row.ChineseSimplified,
        _ => row.English
    };
}