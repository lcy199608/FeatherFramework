using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Reflection;

public class Test : SingletonMono<Test>
{
    string a1 = "testBtn";
    string a2 = "btn";
    private void Start()
    {
        Debug.Log(a1.Contains(a2));
    }
}
