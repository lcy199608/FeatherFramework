using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

public class ActivationRecorder : MonoBehaviourWithID
{
    public bool defaultState;

    void Awake()
    {
        CheckActivation();
    }

    public void CheckActivation()
    {
        gameObject.SetActive(SaveHandler.GetValue(ID, defaultState));
    }
}
