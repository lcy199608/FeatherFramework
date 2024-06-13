using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TextOfEnhance), true)]
[CanEditMultipleObjects]
public class TextOfEnhanceEditor : Editor
{
#if UNITY_EDITOR
    [MenuItem("GameObject/UI/Text &1", false, 10)]
    public static void CreateText(MenuCommand menuCommand)
    {
        // Create a custom game object
        GameObject go = new GameObject("Text");
        // Ensure it gets reparented if this was a context click (otherwise does nothing)
        GameObject parent = menuCommand.context as GameObject;
        if (null == parent)
            parent = Selection.activeGameObject;
        GameObjectUtility.SetParentAndAlign(go, parent);
        // Register the creation in the undo system
        Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
        Selection.activeObject = go;
        TextOfEnhance com = go.AddComponent<TextOfEnhance>();
        com.alignment = TextAnchor.MiddleCenter;
        com.horizontalOverflow = HorizontalWrapMode.Wrap;
        com.verticalOverflow = VerticalWrapMode.Truncate;
        com.resizeTextForBestFit = true;
        com.fontSize = 36;
        com.text = "Text";
        com.raycastTarget = false;
    }
#endif
}
