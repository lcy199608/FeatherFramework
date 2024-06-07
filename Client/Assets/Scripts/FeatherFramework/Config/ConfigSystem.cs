using cfg;
using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ConfigSystem : Singleton<ConfigSystem>
{
    public static Tables Config => config;
    private static Tables config;

    public void InitConfig()
    {
        config = new Tables(LoadByteBuf);
        Debug.Log("== load config succ==");
    }

    private JSONNode LoadByteBuf(string file)
    {
        return JSON.Parse(File.ReadAllText(Application.dataPath + "/../GenerateDatas/json/" + file + ".json", System.Text.Encoding.UTF8));
    }
}
