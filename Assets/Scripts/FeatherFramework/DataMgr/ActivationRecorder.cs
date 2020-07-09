using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class ActivationRecorder : MonoBehaviour
{
    public bool defaultState;
    public SQLIdHolder id;

    void Awake()
    {
        CheckActivation();
    }

    public void CheckActivation()
    {
        gameObject.SetActive(SaveHandler.GetValue(id, defaultState));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ActivationRecorder))]
[CanEditMultipleObjects]
public class ActivationRecorderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var act = target as ActivationRecorder;

        EditorGUILayout.Space(20);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("生成ID"))
        {
            act.id = CreateInstance<SQLIdHolder>();
            act.id.name = act.name + DateTime.Now.ToString("u").Trim();
            AssetDatabase.CreateAsset(act.id, AssetDatabase.GenerateUniqueAssetPath("Assets/Data/IDs/" + act.id.name.Trim() + ".asset"));
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.Refresh();
        }
    }
}
#endif
