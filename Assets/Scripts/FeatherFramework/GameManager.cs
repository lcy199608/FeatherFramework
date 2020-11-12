
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameManager
{
    const string GameFile = "GameFile"; //游戏档位

    public static int nextSceneId = 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SaveHandler.Initialize();
        SaveHandler.LoadTempData(SaveHandler.GetSystemData(GameFile, 0));

        SceneManager.activeSceneChanged += (temp, activeScene) =>
        {
            UIMgr.Instance.ClearCashe();
        };
    }
}
