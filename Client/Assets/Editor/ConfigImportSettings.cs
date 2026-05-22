using UnityEngine;

public enum ConfigImportFormat
{
    Json,
    Bin
}

[CreateAssetMenu(fileName = "ConfigImportSettings", menuName = "FeatherFramework/Config Import Settings")]
public class ConfigImportSettings : ScriptableObject
{
    public ConfigImportFormat importFormat = ConfigImportFormat.Json;
}
