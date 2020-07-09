using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class EnableRecorderGos : MonoBehaviour
{
    //ActivationRecorder必须在物体启用状态才能触发

    [MenuItem("Tools/启用所有挂载ActivationRecorder的对象")]
    static void Activating()
    {
        bool isNeedSave = false;
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
               .Where(n => n.GetComponent<ActivationRecorder>()))
            {
                go.SetActive(true);
                Debug.Log(go.name);
                isNeedSave = true;
                EditorUtility.SetDirty(go);
            }

            if (isNeedSave)
            {
                PrefabUtility.SavePrefabAsset(root);
                SaveAndRefresh();
                isNeedSave = false;
            }
        }

        // 修改场景
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
                foreach (ActivationRecorder obj in Resources.FindObjectsOfTypeAll(typeof(ActivationRecorder)))
                {
                    obj.gameObject.SetActive(true);
                    EditorUtility.SetDirty(obj);
                    Debug.Log(obj.gameObject.name);
                }
                EditorSceneManager.SaveOpenScenes();
                SaveAndRefresh();
            }
        };
    }

    static void SaveAndRefresh()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
