using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using System;

[CreateAssetMenu(fileName = "GameSettings", menuName = "CreatSysFile/GameSettings")]
public class GameSettings : ScriptableObject
{
    public List<LevelInfos> levels;

    public List<LevelInfo> specialLevels;
}