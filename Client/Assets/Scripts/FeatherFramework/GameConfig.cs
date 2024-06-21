using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "GameConfig")]
public class GameConfig : ScriptableObject
{
    [Title("是否启用Debug和日志插件")]
    public bool isDebug = true;

    [Title("默认语言")]
    [EnumPaging]
    public LanguageMgr.SupportedLanguage language;
}
