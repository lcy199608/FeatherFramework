using cfg;
using UnityEngine;

public class ConfigMgr : Singleton<ConfigMgr>
{
    public static Tables Config => config;
    private static Tables config;

    public void InitConfig()
    {
        config = Tables.Load();
        Debug.Log("Load Config Success");
    }
}
