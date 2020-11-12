using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.UI;
using Unity.Linq;
using UnityEditor.SceneManagement;

[CreateAssetMenu(fileName = "FontHelper", menuName = "FontHelper")]
public class MyFontHelper : ScriptableObject
{
    [Space(10)]
    public Font font;
    [Space(10)]
    public bool isDebug;
    [Space(10)]
    bool isNeedSave;

    [CustomEditor(typeof(MyFontHelper))]
    public class MyHelper : Editor
    {
        public override void OnInspectorGUI()
        {
            var myHelper = target as MyFontHelper;
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            if (GUILayout.Button("查看所有预制体使用到的字体"))
            {
                string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", "GameObject"));

                for (int i = 0; i < guids.Length; i++)
                {
                    //转化路径
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                    //加载对象
                    GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                    foreach (var go in root.DescendantsAndSelf()
                       .Where(n => n.GetComponent<Text>()))
                    {
                        if (myHelper.isDebug)
                        {
                            Debug.Log("<color=yellow>Font:</color>" + go.GetComponent<Text>().font.name + "   <color=green>Path:</color>" + assetPath + "  <color=orange>Name:</color>" + root + "/" + go.name);
                        }
                        else
                        {
                            Debug.Log(go.GetComponent<Text>().font.name);
                        }
                    }
                }
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("预制体一键修改"))
            {
                int temp = 0; //修改总数量

                //找到所有Project下GameObject，Prefab就是GameObject
                string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", "GameObject"));

                //对每个路径下的GameObject进行操作
                for (int i = 0; i < guids.Length; i++)
                {
                    //转化路径
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                    //加载对象
                    GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                    foreach (var go in root.DescendantsAndSelf()
                       .Where(n => n.GetComponent<Text>()))
                    {
                        ++temp;
                        if (myHelper.isDebug)
                            Debug.Log(root.name + "下的" + go.name + "物体上的组件已被修改。");
                        go.GetComponent<Text>().font = myHelper.font;
                        myHelper.isNeedSave = true;
                        EditorUtility.SetDirty(go);
                    }
                    Debug.Log(string.Format("预制体修改完成！共修改{0}个Text组件。", temp.ToString()));

                    if (myHelper.isNeedSave)
                    {
                        PrefabUtility.SavePrefabAsset(root);
                        SaveAndRefresh();
                        myHelper.isNeedSave = false;
                    }
                }
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("修改BuildSetting中的所有scene"))
            {
                // 遍历build setting中的场景
                foreach (UnityEditor.EditorBuildSettingsScene S in UnityEditor.EditorBuildSettings.scenes)
                {
                    //在built setting中被勾选
                    if (S.enabled)
                    {
                        //得到场景的名称
                        string name = S.path;

                        //打开这个场景
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(name);

                        // 遍历场景中的GameObject
                        // 注意FindObjectsOfTypeAll虽然会找到所有对象，但是也会找到场景中没有的东西，而且会出现重复查找的情况，最好是进行筛选一下
                        foreach (Text obj in Resources.FindObjectsOfTypeAll(typeof(Text)))
                        {
                            if (myHelper.isDebug)
                                Debug.Log("scene name:" + name + "    object name:" + obj.name);
                            obj.font = myHelper.font;
                            EditorUtility.SetDirty(obj);
                        }
                        EditorSceneManager.SaveOpenScenes();
                        SaveAndRefresh();
                    }
                };
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("修改选中目标及其子物体"))
            {
                var os = Selection.objects;
                foreach (var item in os)
                {
                    GameObject go = item as GameObject;
                    foreach (var g in go.DescendantsAndSelf().Where(_ => _.GetComponent<Text>()))
                    {
                        if (myHelper.isDebug)
                            Debug.Log(g.name + "物体上的组件已被修改。");
                        g.GetComponent<Text>().font = myHelper.font;
                        EditorUtility.SetDirty(g);
                    }
                }
                SaveAndRefresh();
                EditorSceneManager.SaveOpenScenes();
            }

            void SaveAndRefresh()
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}