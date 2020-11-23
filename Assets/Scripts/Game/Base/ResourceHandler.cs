using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ResourceHandler
{
    public static void LoadLevelAsset()
    {
        var temp = Resources.LoadAsync<GameObject>(GameManager.levelPathTemp);
        temp.completed += asset =>
        {
            if (asset.isDone)
            {
                GameManager.levelGo = temp.asset as GameObject;
                GameManager.isGoingToLevel = false;
                GameManager.levelPathTemp = string.Empty;
            }
        };
    }
}

