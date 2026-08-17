using System;
using System.Collections;
using System.Collections.Generic;
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
        gameObject.SetActive(SaveDataMgr.GetValue(ID, defaultState));
    }
}
