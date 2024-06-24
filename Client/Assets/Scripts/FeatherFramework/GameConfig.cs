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

    [Title("帧率设置(0为不限制,-1为垂直同步)")]
    public int targetFrameRate = -1;
}