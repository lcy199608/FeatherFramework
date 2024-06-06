using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BasePanel : MonoBehaviour
{
    public virtual void Init() { }
    public abstract void ShowPanel();
    public abstract void HidePanel();
}
