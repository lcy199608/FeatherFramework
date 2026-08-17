using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class MonoBehaviourWithID : MonoBehaviour
{
    public SQLIdHolder ID;
    private const string path = "Assets/Data/IDHolder/";

#if UNITY_EDITOR
    [CustomEditor(typeof(MonoBehaviourWithID), true)]
    [CanEditMultipleObjects]
    public class MonoBehaviourWithIDEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var monoID = target as MonoBehaviourWithID;
            if (GUILayout.Button("生成ID"))
            {
                monoID.ID = CreateInstance<SQLIdHolder>();
                monoID.ID.name = monoID.name + DateTime.Now.ToString("u").Trim();
                AssetDatabase.CreateAsset(monoID.ID, AssetDatabase.GenerateUniqueAssetPath(path + monoID.ID.name.Trim() + ".asset"));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
#endif
}
