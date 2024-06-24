using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    private static readonly string[] Units = new string[] { "", "k", "m", "b", "t", "aa", "bb", "cc", "dd", "ee", "ff", "gg", "hh", "ii", "jj", "kk", "ll", "mm", "nn", "oo", "pp", "qq", "rr", "ss", "tt", "uu", "vv", "ww", "xx", "yy", "zz" /* ...继续添加更多单位... */ };

    /// <summary>
    /// 大数转换
    /// </summary>
    /// <param name="number">原始数</param>
    /// <param name="decimalPlaces">保留几位小数</param>
    /// <returns></returns>
    public static string FormatNumber(double number, int decimalPlaces = 3)
    {
        if (number == 0)
            return "0";

        int unitIndex = 0;

        while (number >= 1000)
        {
            number /= 1000;
            unitIndex++;
        }

        unitIndex = Math.Min(unitIndex, Units.Length - 1);

        // 使用指定的小数位数格式化数字，并添加适当的单位
        string format = "{0:0." + new string('#', decimalPlaces) + "}{1}";
        return String.Format(format, number, Units[unitIndex]);
    }

    static Canvas canvas;
    /// <summary>
    /// 世界坐标转换为UI坐标
    /// </summary>
    /// <param name="worldPos"></param>
    /// <param name="canvas"></param>
    /// <returns></returns>
    public static Vector3 WorldPos2UIPos(Vector3 worldPos,Canvas canvas)
    {
        if (canvas == null)
        {
            canvas = GameObject.Find("UICanvas(Clone)").GetComponent<Canvas>();
        }

        Vector2 pos = Camera.main.WorldToScreenPoint(worldPos);
        Vector2 point;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, pos, canvas.worldCamera, out point))
        {
            return point;
        }

        Debug.LogError("坐标转换失败！");
        return Vector3.zero;
    }

    /// <summary>
    /// UI朝向
    /// </summary>
    /// <param name="transform">UI修改目标的Transform</param>
    /// <param name="dir">朝向向量</param>
    /// <param name="lookAxis">起始向量</param>
    public static void LookAt(Transform transform, Vector3 dir, Vector3 lookAxis)
    {
        Quaternion q = Quaternion.identity;
        q.SetFromToRotation(lookAxis, dir);
        transform.rotation = q;
    }
}
