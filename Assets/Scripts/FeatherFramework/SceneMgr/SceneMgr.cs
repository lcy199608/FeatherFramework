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
    public void LoadScene(string name,UnityAction action)
    {
        SceneManager.LoadScene(name);
        action();
    }

    public void LoadSceneAsync(string name,UnityAction action)
    {
        MonoController.Instance.StartCoroutine(ReallyLoadSceneAsync(name, action));
    }

    private IEnumerator ReallyLoadSceneAsync(string name,UnityAction action)
    {
        AsyncOperation ao = SceneManager.LoadSceneAsync(name);
        while (!ao.isDone)
        {
            EventCenter.Instance.EventTrigger("进度条更新", ao.progress);
            yield return ao.progress;
        }
        action();
    }
}
