using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldPos2UI : SingletonMono<WorldPos2UI>
{
    Canvas canvas;
    Dictionary<RectTransform, TransInfo> followsDic = new Dictionary<RectTransform, TransInfo>(); //key = UI对象，value = 目标对象

    struct TransInfo
    {
        public Transform target;
        public Vector3 offset;

        public TransInfo(Transform target, Vector3 offset)
        {
            this.target = target;
            this.offset = offset;
        }
    }

    void Update()
    {
        if (followsDic.Count == 0)
            return;

        foreach (var item in followsDic.Keys)
        {
            item.anchoredPosition = World2UIPos(followsDic[item].target.position + followsDic[item].offset);
        }
    }

    public void AddFollow(RectTransform followTrans, Transform targetTrans, Vector3 offset = default)
    {
        if (followsDic.ContainsKey(followTrans))
        {
            followsDic[followTrans] = new TransInfo(targetTrans, offset);
        }
        else
        {
            followsDic.Add(followTrans, new TransInfo(targetTrans, offset));
        }
    }

    public void RemoveFollow(RectTransform followTrans)
    {
        if (followsDic.ContainsKey(followTrans))
        {
            followsDic.Remove(followTrans);
        }
    }

    public Vector3 World2UIPos(Vector3 worldPos)
    {
        if (canvas == null)
            canvas = GameObject.Find("UICanvas(Clone)").GetComponent<Canvas>();

        Vector2 pos = Camera.main.WorldToScreenPoint(worldPos);
        Vector2 point;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, pos, canvas.worldCamera, out point))
        {
            return point;
        }

        Debug.LogError("坐标转换失败！");
        return Vector3.zero;
    }
}
