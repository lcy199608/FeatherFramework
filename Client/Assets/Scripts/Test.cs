using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using cfg;

public class Test : MonoBehaviour
{
    int id;
    void Start()
    {
        float num = 12345f;
        Debug.Log(Utils.FormatNumber(num));
    }

    void TestFunc()
    {
        Debug.LogWarning("Complete");
    }
}
