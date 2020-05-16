using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Reflection;

public class Test : SingletonMono<Test>
{
    bool switchFile;
    //public List<int> a = new List<int> { 1,2,3,4,5};
    //public List<int> b = new List<int> { 4, 5 ,6};
    private void Start()
    {
        SaveHandler.Initialize();
        SaveHandler.SetTempData("test", "1");
    }

    public void DoTest()
    {
        //a.RemoveAll(_ => b.Contains(_));
        //a = b.Concat(a).ToList();

        //a.ForEach(_ => Debug.Log("a:" + _)); //输出 456123

        //string aa = "Test1";
        //Type t = Type.GetType(aa);

        //var obj = t.Assembly.CreateInstance(aa);

        //MethodInfo method = t.GetMethod("Abc");
        //method.Invoke(obj, null);

        if (switchFile)
        {
            //Debug.Log();
        }
    }
}
