using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "ID/SqlID")]
public class SQLIdHolder : IdHolder
{
    public bool IsSystemData;
    public int GroupID;
}
