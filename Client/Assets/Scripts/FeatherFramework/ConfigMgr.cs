using cfg;
using UnityEngine;

public sealed class ConfigMgr
{
    public Tables Tables { get; private set; }

    internal ConfigMgr() { }

    internal void InitConfig(bool enableLocalization = true, bool enableRedDots = true)
    {
        Tables = cfg.Tables.Load(name => (name != "Language" || enableLocalization) &&
                                        (name != "RedDot" || enableRedDots));
        Debug.Log("Load Config Success");
    }
}
