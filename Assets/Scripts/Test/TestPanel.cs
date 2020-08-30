using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TestPanel : BasePanel
{
    public override void HidePanel()
    {
        Debug.Log("Hide");
        gameObject.SetActive(false);
        //Destroy(gameObject);
    }

    public override void ShowPanel()
    {
        Debug.Log("Show");
        transform.localScale = Vector3.zero;
        GetComponent<DOTweenAnimation>().DORestart();
    }
}
