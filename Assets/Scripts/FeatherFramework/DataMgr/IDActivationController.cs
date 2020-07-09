using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IDActivationController : MonoBehaviour
{
    public float delayTime;
    public List<SQLIdHolder> activeIds;
    public List<SQLIdHolder> inactiveIds;

    public void ChangeState()
    {
        Invoke("Change", delayTime);
    }

    void Change()
    {
        if (!(activeIds == null))
        {
            activeIds.ForEach(_ => SaveHandler.SetValue(_, true,true));
        }

        if (!(inactiveIds == null))
        {
            inactiveIds.ForEach(_ => SaveHandler.SetValue(_, false,true));
        }
    }
}
