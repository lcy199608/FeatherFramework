using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;


public class AutoBuildTemplate
{
    public static string UIClass =
 @"using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
public class #类名# : BasePanel
{
    public override void ShowPanel()
    {
        
    }

    public override void HidePanel()
    {

    }

//auto
    #成员#

    public override void Init()
    {
        base.Init();
        #查找#
    }
}
";
}


public class AutoBuild
{

    [MenuItem("LcyFramework/生成或刷新UI脚本")]
    public static void BuildUIScript()
    {

        var dicUIType = new Dictionary<string, string>();

        dicUIType.Add("Img", "Image");
        dicUIType.Add("Btn", "Button");
        dicUIType.Add("Txt", "Text");
        dicUIType.Add("Tran", "Transform");

        //获取选中的prefab
        GameObject[] selectobjs = Selection.gameObjects;

        foreach (GameObject go in selectobjs)
        {
            //选择的物体
            GameObject selectobj = go.transform.root.gameObject;

            //物体的子物体
            Transform[] _transforms = selectobj.GetComponentsInChildren<Transform>(true);

            //转换为list
            List<Transform> childList = new List<Transform>(_transforms);

            //UI需要查询的物体，根据规则筛选
            var mainNode = from trans in childList where dicUIType.Keys.ToList().Any(_ => trans.name.Contains(_)) select trans;

            //存储每个目标路径
            var nodePathList = new Dictionary<string, string>();

            //循环得到物体路径
            foreach (Transform node in mainNode)
            {
                Transform tempNode = node;
                string nodePath = "/" + tempNode.name;

                //遍历到顶，求得路径
                while (tempNode != tempNode.root)
                {
                    //取得上级
                    tempNode = tempNode.parent;

                    if (tempNode == tempNode.root)
                        continue;

                    //求出/在哪
                    int index = nodePath.IndexOf('/');

                    //把得到的路径插入
                    nodePath = nodePath.Insert(index, "/" + tempNode.name);
                }

                nodePath = nodePath.Remove(0, 1);// 去掉/
                //将最终路径存入
                nodePathList.Add(node.name, nodePath);
            }

            //成员变量字符串
            string memberstring = "";
            //查询代码字符串
            string loadedcontant = "";

            foreach (Transform itemtran in mainNode)
            {
                //识别变量类型
                string typeStr = dicUIType[dicUIType.Keys.First(_ => itemtran.name.Contains(_))];

                //变量声明
                memberstring += "private " + typeStr + " " + itemtran.name + " = null;\r\n\t";

                //查找语句
                loadedcontant += itemtran.name + " = " + "gameObject.transform.Find(\"" + nodePathList[itemtran.name] + "\").GetComponent<" + typeStr + ">();\r\n\t\t";
            }

            //创建脚本的路径
            string scriptPath = Application.dataPath + "/Scripts/Game/UI/" + selectobj.name + ".cs";


            string classStr = "";

            //如果已经存在了脚本，则只替换//auto下方的字符串
            //方便刷新
            if (File.Exists(scriptPath))
            {
                FileStream classfile = new FileStream(scriptPath, FileMode.Open);
                StreamReader read = new StreamReader(classfile);
                classStr = read.ReadToEnd();
                read.Close();
                classfile.Close();
                File.Delete(scriptPath);

                //分割的位置
                string splitStr = "//auto";
                //auto 上面的部分
                string unchangeStr = Regex.Split(classStr, splitStr, RegexOptions.IgnoreCase)[0];
                //auto 下面的部分
                string changeStr = Regex.Split(AutoBuildTemplate.UIClass, splitStr, RegexOptions.IgnoreCase)[1];

                StringBuilder build = new StringBuilder();
                build.Append(unchangeStr);
                build.Append(splitStr);
                build.Append(changeStr);
                classStr = build.ToString();
            }
            else
            {
                classStr = AutoBuildTemplate.UIClass;
            }

            classStr = classStr.Replace("#类名#", selectobj.name);
            classStr = classStr.Replace("#查找#", loadedcontant);
            classStr = classStr.Replace("#成员#", memberstring);

            FileStream file = new FileStream(scriptPath, FileMode.CreateNew);
            StreamWriter fileW = new StreamWriter(file, System.Text.Encoding.UTF8);
            fileW.Write(classStr);
            fileW.Flush();
            fileW.Close();
            file.Close();

            Debug.Log("创建脚本 " + Application.dataPath + "/Scripts/Game/UI/" + selectobj.name + ".cs 成功!");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

}
