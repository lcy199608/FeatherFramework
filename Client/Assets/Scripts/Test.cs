using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using cfg;

public class Test : MonoBehaviour
{
    void Start()
    {
        UnityEngine.Debug.LogFormat("item[1].name:{0}", ConfigSystem.Config.TbItem[10000].Name);
    }
}
