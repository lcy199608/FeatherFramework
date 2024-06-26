
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public static class GameManager
{
    const string GameFile = "GameFile"; //游戏档位
    public static GameConfig Config;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        //项目配置
        Addressables.InitializeAsync();
        Config = ResMgr.Instance.Load<GameConfig>("Res/GameConfig.asset");
        //日志
        Reporter.IsEnableLog = Config.isDebug;
        Debug.unityLogger.logEnabled = Config.isDebug;
        //帧率
        SetFrameRate(Config.targetFrameRate);
        //本地数据
        SaveDataMgr.Initialize();
        SaveDataMgr.LoadData(SaveDataMgr.GetSystemData(GameFile, 0));
        //配置表
        ConfigMgr.Instance.InitConfig();
        //红点
        RedDotSystem.Instance.InitRedDotTreeNode();
        //UI
        UIMgr.Instance.CreateUICanvas();
    }

    /// <summary>
    /// 帧率设置
    /// </summary>
    public static void SetFrameRate(int frameRate)
    {
        //垂直同步设置会影响帧率设置
        QualitySettings.vSyncCount = frameRate == -1 ? 1 : 0;
        Application.targetFrameRate = frameRate;
    }
}