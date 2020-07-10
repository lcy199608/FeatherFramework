using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class ScriptableObjectWithID : ScriptableObject
{
    public SQLIdHolder ID;
    private const string path = "Assets/Data/IDHolder/";

#if UNITY_EDITOR
    [CustomEditor(typeof(ScriptableObjectWithID), true)]
    [CanEditMultipleObjects]
    public class MonoBehaviourWithIDEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var monoID = target as ScriptableObjectWithID;
            GUILayout.Space(20);
            if (GUILayout.Button("生成ID"))
            {
                monoID.ID = CreateInstance<SQLIdHolder>();
                monoID.ID.name = monoID.name + DateTime.Now.ToString("u").Trim();
                AssetDatabase.CreateAsset(monoID.ID, AssetDatabase.GenerateUniqueAssetPath(path + monoID.ID.name.Trim() + ".asset"));
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(target);
                AssetDatabase.Refresh();
            }
        }
    }
#endif
}
