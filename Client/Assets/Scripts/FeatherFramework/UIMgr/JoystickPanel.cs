using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEditor;

public class JoystickPanel : BasePanel
{
    public enum JoystickType
    {
        Normal, //固定摇杆
        AutoHide, //不点击自动隐藏摇杆
        Move, //摇杆停留在上次位置
    }

    //摇杆检测区域
    public enum JoystickArea
    {
        Top,
        Bottom,
        Left,
        Right,
        LeftTop,
        LeftBottom,
        RightTop,
        RightBottom,
    }

    public override void ShowPanel()
    {
        if (type == JoystickType.AutoHide)
        {
            imgBG.gameObject.Hide();
        }
    }

    public override void HidePanel()
    {
    }

    public void DragEvent(PointerEventData data)
    {
        if (!isInArea)
            return;

        controlPos = Screen2LocalPos(imgBG.rectTransform, data.position);
        imgControl.transform.localPosition = controlPos;

        if(controlPos.magnitude > maxRange)
        {
            imgControl.transform.localPosition = controlPos.normalized * maxRange;
        }

        EventCenter.Instance.EventTrigger("JoystickDir", controlPos.normalized);
        EventCenter.Instance.EventTrigger("JoystickValue", imgControl.transform.localPosition.magnitude / maxRange);
    }

    public void EndDragEvent(PointerEventData data)
    {
        imgControl.transform.localPosition = Vector3.zero;
        isInArea = false;

        if (type == JoystickType.AutoHide)
        {
            imgBG.gameObject.Hide();
        }
    }

    public void BeginDragEvent(PointerEventData data)
    {
        switch (type)
        {
            case JoystickType.Normal:
                //判断是否在摇杆内拖拽
                var pos = Screen2LocalPos(imgBG.rectTransform, data.position);
                if (pos.magnitude > maxRange)
                    return;
                break;
            case JoystickType.AutoHide:
                break;
            case JoystickType.Move:
                break;
        }

        if(type == JoystickType.Normal)
        {
            //判断是否在摇杆内拖拽
            var pos = Screen2LocalPos(imgBG.rectTransform, data.position);
            if (pos.magnitude > maxRange)
                return;
        }

        //判断区域是否符合
        switch (area)
        {
            case JoystickArea.Top:
                if (data.position.y > height / 2)
                    isInArea = true;
                else
                    return;
                break;
            case JoystickArea.Bottom:
                if (data.position.y < height / 2)
                    isInArea = true;
                else
                    return;
                break;
            case JoystickArea.Left:
                if (data.position.x < width / 2)
                    isInArea = true;
                else
                    return;
                break;
            case JoystickArea.Right:
                if (data.position.x > width / 2)
                    isInArea = true;
                else
                    return;
                break;
            case JoystickArea.LeftTop:
                if (data.position.x < width / 2 && data.position.y > height / 2)
                    isInArea = true;
                else
                    return;
                break;
            case JoystickArea.LeftBottom:
                if (data.position.x < width / 2 && data.position.y < height / 2)
                    isInArea = true;
                else
                    return;
                break;
            case JoystickArea.RightTop:
                if (data.position.x > width / 2 && data.position.y > height / 2)
                    isInArea = true;
                else
                    return;
                break;
            case JoystickArea.RightBottom:
                if (data.position.x > width / 2 && data.position.y < height / 2)
                    isInArea = true;
                else
                    return;
                break;
        }

        if (type == JoystickType.AutoHide)
        {
            //初始化位置为点击位置
            var pos = Screen2LocalPos(imgTouchRect.rectTransform, data.position);
            imgBG.transform.localPosition = pos;

            imgBG.gameObject.Show();
        }

        if (type == JoystickType.Move)
        {
            var pos = Screen2LocalPos(imgTouchRect.rectTransform, data.position);
            imgBG.transform.localPosition = pos;
        }
    }

    Vector2 Screen2LocalPos(RectTransform rect,Vector2 screenPos)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, Camera.main, out pos);
        return pos;
    }

    public JoystickType type;
    public JoystickArea area;
    public float maxRange = 0;
    private float width;
    private float height;
    private bool isInArea;
    Vector2 controlPos;

    //auto
    private Image imgTouchRect = null;
	private Image imgBG = null;
	private Image imgControl = null;
	

    public override void Init()
    {
        base.Init();
        imgTouchRect = transform.Find("ImgTouchRect").GetComponent<Image>();
		imgBG = transform.Find("ImgTouchRect/ImgBG").GetComponent<Image>();
		imgControl = transform.Find("ImgTouchRect/ImgBG/ImgControl").GetComponent<Image>();
        
        UIEventListenerMgr.Instance.AddSafeListener(imgTouchRect.gameObject, UIEventListenerMgr.EventType.Up, EndDragEvent);
        UIEventListenerMgr.Instance.AddSafeListener(imgTouchRect.gameObject, UIEventListenerMgr.EventType.Down, BeginDragEvent);
        UIEventListenerMgr.Instance.AddSafeListener(imgTouchRect.gameObject, UIEventListenerMgr.EventType.Drag, DragEvent);

        width = Screen.width;
        height = Screen.height;
    }
}