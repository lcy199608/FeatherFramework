using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BasePanel : MonoBehaviour
{
    public object uiData;
    public virtual void OnInit() { }
    public abstract void OnShow();
    public abstract void OnHide();
}
