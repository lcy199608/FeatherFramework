using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldPos2UI : SingletonMono<WorldPos2UI>
{
    Canvas canvas;
    Dictionary<RectTransform, Transform> followsDic = new Dictionary<RectTransform, Transform>(); //key = UI对象，value = 目标对象

    void Update()
    {
        if (followsDic.Count == 0)
            return;

        foreach (var item in followsDic.Keys)
        {
            item.anchoredPosition = World2UIPos(followsDic[item].position);
        }
    }

    public void AddFollow(RectTransform followTrans,Transform targetTrans)
    {
        if (followsDic.ContainsKey(followTrans))
        {
            followsDic[followTrans] = targetTrans;
        }
        else
        {
            followsDic.Add(followTrans, targetTrans);
        }
    }

    public void RemoveFollow(RectTransform followTrans)
    {
        if (followsDic.ContainsKey(followTrans))
        {
            followsDic.Remove(followTrans);
        }
    }

    Vector3 World2UIPos(Vector3 worldPos)
    {
        if (canvas == null)
            canvas = GameObject.Find("UICanvas(Clone)").GetComponent<Canvas>();

        Vector2 pos = Camera.main.WorldToScreenPoint(worldPos);
        Vector2 point;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent as RectTransform, pos, canvas.worldCamera, out point))
        {
            return point;
        }

        Debug.LogError("坐标转换失败！");
        return Vector3.zero;
    }
}
