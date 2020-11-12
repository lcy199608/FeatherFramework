using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
public class T : MonoBehaviour
{
//auto
   public void Start()
	{
		Img_Img = gameObject.transform.Find("/T/Img_Img").GetComponent<Image>();
		
	}
	public Image Img_Img = null;
	
}
