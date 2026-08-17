using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class IdHolder : ScriptableObject
{
    public string ID;

#if UNITY_EDITOR
    private void Awake()
    {
        if (ID == string.Empty || ID == null)
        {
            Guid g = Guid.NewGuid();
            string GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");
            GuidString = GuidString.Replace("/", "");
            ID = GuidString;
            EditorUtility.SetDirty(this);
        }
    }

    [CustomEditor(typeof(IdHolder), true)]
    [CanEditMultipleObjects]
    public class IDRecorderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var monoID = target as IdHolder;
            if (GUILayout.Button("重新生成ID"))
            {
                Guid g = Guid.NewGuid();
                string GuidString = Convert.ToBase64String(g.ToByteArray());
                GuidString = GuidString.Replace("=", "");
                GuidString = GuidString.Replace("+", "");
                GuidString = GuidString.Replace("/", "");
                monoID.ID = GuidString;
                EditorUtility.SetDirty(monoID);
            }
        }
    }
#endif
}
