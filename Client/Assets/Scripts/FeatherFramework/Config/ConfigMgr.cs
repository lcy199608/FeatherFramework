using cfg;
using Luban;
using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ConfigMgr : Singleton<ConfigMgr>
{
    public static Tables Config => config;
    private static Tables config;

    public void InitConfig()
    {
        config = new Tables(LoadByteBuf);
        Debug.Log("Load Config Success");
    }
    
    /// <summary>
    /// 读取JSON
    /// </summary>
    private JSONNode LoadJsonBuf(string file)
    {
        return JSON.Parse(File.ReadAllText(Application.dataPath + "/../GenerateDatas/json/" + file + ".json", System.Text.Encoding.UTF8));
    }

    /// <summary>
    /// 读取BIN
    /// </summary>
    private ByteBuf LoadByteBuf(string file)
    {
        return new ByteBuf(File.ReadAllBytes($"{Application.dataPath}/../GenerateDatas/bytes/{file}.bytes"));
    }
}
