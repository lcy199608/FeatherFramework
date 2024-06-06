using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "1", menuName = "Level/LevelInfo")]

[Serializable]
public class LevelInfo : ScriptableObject
{
    const string folderPath = "Levels/";
    public string levelName;
    public string levelPath;
    public MultiLanguageText tips;

#if UNITY_EDITOR
    public GameObject level;

    private void OnValidate()
    {
        levelName = level.gameObject.name;
        levelPath = folderPath + levelName;
    }
#endif
}

[Serializable]
public class LevelInfos
{
    public List<LevelInfo> levels;
}
