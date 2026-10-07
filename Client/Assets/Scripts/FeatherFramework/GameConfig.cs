using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "GameConfig")]
public class GameConfig : ScriptableObject, ISerializationCallbackReceiver
{
    [Title("可选模块（启动时生效）")]
    public bool enableLocalization = true;
    public bool enableRedDots = true;

    [Title("是否启用Debug和日志插件")]
    public bool isDebug = true;

    [Title("默认语言")]
    public bool followSystemLanguage = true;
    [HideIf(nameof(followSystemLanguage))]
    public SystemLanguage defaultLanguage = SystemLanguage.English;

    // Keep the old serialized field name and integer representation until each asset is saved.
    [SerializeField, HideInInspector]
    private int language = -1;

    public void OnBeforeSerialize() { }
    public void OnAfterDeserialize()
    {
        if (language < 0) return;
        followSystemLanguage = language == 0;
        defaultLanguage = LanguageMgr.FromLegacyLanguage(language);
        language = -1;
    }

    [Title("帧率设置(0为不限制,-1为垂直同步)")]
    public int targetFrameRate = -1;
}
