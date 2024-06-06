using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SceneMgr : Singleton<SceneMgr>
{
    /// <summary>
    /// 加载场景
    /// </summary>
    /// <param name="name"></param>
    public void LoadScene(int sceneIndex, UnityAction action)
    {
        SceneManager.LoadScene(sceneIndex);
        action();
    }

    public void LoadSceneAsync(int sceneIndex, UnityAction action)
    {
        MonoMgr.Instance.StartCoroutine(ReallyLoadSceneAsync(sceneIndex, action));
    }

    private IEnumerator ReallyLoadSceneAsync(int sceneIndex, UnityAction action)
    {
        AsyncOperation ao = SceneManager.LoadSceneAsync(sceneIndex);
        ao.allowSceneActivation = false;
        while (!ao.isDone)
        {
            EventCenter.Instance.EventTrigger("进度条更新", ao); //如果需要同步进度条
            yield return null;
        }
        action();
    }
}
