using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class UIEventListener : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Action<PointerEventData> dragEvent = null;
    public Action<PointerEventData> beginDragEvent = null;
    public Action<PointerEventData> endDragEvent = null;
    public Action<PointerEventData> clickEvent = null;
    public Action<PointerEventData> downEvent = null;
    public Action<PointerEventData> upEvent = null;

    public void OnBeginDrag(PointerEventData eventData)
    {
        beginDragEvent?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        dragEvent?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        endDragEvent?.Invoke(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        clickEvent?.Invoke(eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        downEvent?.Invoke(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        upEvent?.Invoke(eventData);
    }
}
