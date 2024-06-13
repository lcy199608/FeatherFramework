using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivationRecorderController : MonoBehaviour
{
    public float delayTime = 0;
    public List<ActivationRecorder> activeRecorders;
    public List<ActivationRecorder> inactiveRecorders;

    public void ChangeState()
    {
        Invoke("SetState",delayTime);
    }

    void SetState()
    {
        if (!(activeRecorders == null))
        {
            activeRecorders.ForEach(_ => { SaveDataMgr.SetValue(_.ID, true,true);_.gameObject.SetActive(true); }) ;
        }

        if (!(inactiveRecorders == null))
        {
            inactiveRecorders.ForEach(_ => { SaveDataMgr.SetValue(_.ID, false,true);_.gameObject.SetActive(false); }) ;
        }
    }
}
