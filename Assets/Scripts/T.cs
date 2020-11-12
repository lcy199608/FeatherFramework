using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
public class T : BasePanel
{
    public override void Init()
    {
        base.Init();

    }

    public override void ShowPanel()
    {
        
    }

    public override void HidePanel()
    {

    }

//auto
    private Button testBtn = null;
	

   public void Start()
	{
		testBtn = gameObject.transform.Find("/T/testBtn").GetComponent<Button>();
		
	}
}
