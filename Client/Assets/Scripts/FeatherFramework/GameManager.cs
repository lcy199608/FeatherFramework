
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameManager
{
    const string GameFile = "GameFile"; //游戏档位
    public static GameConfig gameConfig;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        //项目配置
        gameConfig = ResMgr.Instance.Load<GameConfig>("GameConfig");
        if (gameConfig.isDebug)
        {
            Reporter.IsEnableLog = true;
            Debug.unityLogger.logEnabled = true;
        }
        else
        {
            Debug.unityLogger.logEnabled = false;
            Reporter.IsEnableLog = false;
        }
        //本地数据
        SaveDataMgr.Initialize();
        SaveDataMgr.LoadData(SaveDataMgr.GetSystemData(GameFile, 0));
        //配置表
        ConfigMgr.Instance.InitConfig();
        //红点
        RedDotSystem.Instance.InitRedDotTreeNode();
    }
}
