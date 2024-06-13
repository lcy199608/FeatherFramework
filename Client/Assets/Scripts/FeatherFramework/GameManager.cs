
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameManager
{
    const string GameFile = "GameFile"; //游戏档位

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        //本地数据
        SaveDataMgr.Initialize();
        SaveDataMgr.LoadData(SaveDataMgr.GetSystemData(GameFile, 0));
        //配置表
        ConfigMgr.Instance.InitConfig();
    }
}
