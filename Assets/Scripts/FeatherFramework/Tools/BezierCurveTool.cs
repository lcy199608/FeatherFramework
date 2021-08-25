using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum BezierType
{
    None,
    Two,
    Three,
    Four
}

public class BezierCurveTool : MonoBehaviour
{
    [Tooltip("选择使用几阶贝塞尔")]
    public BezierType type = BezierType.Two
        ;
    [Tooltip("生成点放在该物体下")]
    public Transform parent;

    [Tooltip("控制点的父对象")]
    [SerializeField] private Transform points;

    [Tooltip("控制点列表")]
    private List<Vector3> pointList = new List<Vector3>();

    [Tooltip("需要生成的点的个数")]
    [SerializeField] private int pointCount = 100;

    [Tooltip("生成的目标点列表")]
    [HideInInspector]
    public List<Vector3> line_pointList = new List<Vector3>();

    public void Init()
    {
        if (null != points)
        {
            foreach (Transform item in points)
            {
                pointList.Add(item.position);
            }
        }
        BezierCurve(type, pointList, pointCount);
    }

    void BezierCurve(BezierType type,List<Vector3> pointList,int pointCount)
    {
        line_pointList.Clear();

        switch (type)
        {
            case BezierType.Two:
                for (int i = 0; pointList.Count != 0 && i < pointCount; i++)
                {
                    Vector3 pos1 = Vector3.Lerp(pointList[0], pointList[1], i / (float)pointCount);
                    Vector3 pos2 = Vector3.Lerp(pointList[1], pointList[2], i / (float)pointCount);
                    Vector3 find = Vector3.Lerp(pos1, pos2, i / (float)pointCount);

                    line_pointList.Add(find);
                }
                break;
            case BezierType.Three:
                for (int i = 0; pointList.Count != 0 && i < pointCount; i++)
                {
                    Vector3 pos1 = Vector3.Lerp(pointList[0], pointList[1], i / (float)pointCount);
                    Vector3 pos2 = Vector3.Lerp(pointList[1], pointList[2], i / (float)pointCount);
                    Vector3 pos3 = Vector3.Lerp(pointList[2], pointList[3], i / (float)pointCount);

                    var pos1_0 = Vector3.Lerp(pos1, pos2, i / (float)pointCount);
                    var pos1_1 = Vector3.Lerp(pos2, pos3, i / (float)pointCount);
                    Vector3 find = Vector3.Lerp(pos1_0, pos1_1, i / (float)pointCount);

                    line_pointList.Add(find);
                }
                break;
            case BezierType.Four:
                for (int i = 0; pointList.Count != 0 && i < pointCount; i++)
                {
                    Vector3 pos1 = Vector3.Lerp(pointList[0], pointList[1], i / (float)pointCount);
                    Vector3 pos2 = Vector3.Lerp(pointList[1], pointList[2], i / (float)pointCount);
                    Vector3 pos3 = Vector3.Lerp(pointList[2], pointList[3], i / (float)pointCount);
                    Vector3 pos4 = Vector3.Lerp(pointList[3], pointList[4], i / (float)pointCount);

                    var pos1_0 = Vector3.Lerp(pos1, pos2, i / (float)pointCount);
                    var pos1_1 = Vector3.Lerp(pos2, pos3, i / (float)pointCount);
                    var pos1_2 = Vector3.Lerp(pos3, pos4, i / (float)pointCount);

                    var pos2_0 = Vector3.Lerp(pos1_0, pos1_1, i / (float)pointCount);
                    var pos2_1 = Vector3.Lerp(pos1_1, pos1_2, i / (float)pointCount);

                    Vector3 find = Vector3.Lerp(pos2_0, pos2_1, i / (float)pointCount);

                    line_pointList.Add(find);
                }
                break;
        }

        this.pointList.Clear();
    }

    //获取贝塞尔算法生成点
    //不传默认取配置值
    public List<Vector3> GetPoints(int pointCount = 0,BezierType type = BezierType.None, List<Vector3> controlPoints = null)
    {
        pointCount = pointCount == 0 ? this.pointCount : pointCount;
        type = type == BezierType.None ? this.type : type;
        if(controlPoints == null)
        {
            if (null != points)
            {
                foreach (Transform item in points)
                {
                    pointList.Add(item.position);
                }
                controlPoints = pointList;
            }
        }
        BezierCurve(type, controlPoints, pointCount);
        return line_pointList;
    }

#if UNITY_EDITOR
    //在scene视图显示
    void OnDrawGizmos()
    {
        Init();
        Gizmos.color = Color.yellow;
        for (int i = 0; i < line_pointList.Count - 1; i++)
        {
            Gizmos.DrawLine(line_pointList[i], line_pointList[i + 1]);
        }
    }
#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(BezierCurveTool))]
public class CreatCurvePointsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var ccp = target as BezierCurveTool;

        Color bc = GUI.backgroundColor;
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("CreateCurve", GUILayout.Height(30)))
        {
            ccp.Init();

            ccp.line_pointList.ForEach(_ =>
            {
                GameObject obj = new GameObject();
                obj.transform.SetParent(ccp.parent);
                obj.transform.position = _;
                obj.transform.rotation = Quaternion.Euler(Vector3.zero);
                obj.transform.localScale = Vector3.zero;
            });

            
        }
        GUI.backgroundColor = bc;

        bc = GUI.backgroundColor;
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Clear", GUILayout.Height(30)))
        {
            Transform parent = ccp.parent;
            for (int i = parent.childCount; i >= 1; i--)
            {
                DestroyImmediate(parent.GetChild(i - 1).gameObject, true);
            }
        }
        GUI.backgroundColor = bc;
    }
}
#endif
