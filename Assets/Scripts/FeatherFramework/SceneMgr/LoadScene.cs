using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class LoadScene : MonoBehaviour
{
    private void Start()
    {
        EventCenter.Instance.AddEventListener<AsyncOperation>("进度条更新", _ => 
        {
                _.allowSceneActivation = true;
        });

        SceneMgr.Instance.LoadSceneAsync(GameManager.nextSceneId,() =>{});
    }
}
