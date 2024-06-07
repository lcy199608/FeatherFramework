
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
        SaveHandler.Initialize();
        SaveHandler.LoadTempData(SaveHandler.GetSystemData(GameFile, 0));
        ConfigSystem.Instance.InitConfig();
    }
}
