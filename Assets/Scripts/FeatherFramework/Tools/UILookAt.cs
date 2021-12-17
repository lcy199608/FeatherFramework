using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILookAt : Singleton<UILookAt>
{
    //参数分别为：1.UI修改目标的Transform		2.朝向向量		3.起始向量
    public void LookAt(Transform transform, Vector3 dir, Vector3 lookAxis)
    {
        Quaternion q = Quaternion.identity;
        q.SetFromToRotation(lookAxis, dir);
        transform.rotation = q;
    }
}
