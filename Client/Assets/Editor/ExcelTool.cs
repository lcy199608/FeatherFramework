using System.IO;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using UnityEditor;
using UnityEngine;

public class ExcelTool : MonoBehaviour
{
    private static readonly string ToolDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Config/SheetTool"));
    private const string SettingsAssetPath = "Assets/Data/ConfigImportSettings.asset";

    [MenuItem("FeatherFramework/Config/Validate Excel Config")]
    private static void ValidateExcelConfig()
    {
        RunSheetTool("validate");
    }

    [MenuItem("FeatherFramework/Config/Sync Excel Config")]
    private static void SyncExcelConfig()
    {
        ConfigImportSettings settings = GetOrCreateSettings();
        string format = settings.importFormat == ConfigImportFormat.Bin ? "bin" : "json";
        RunSheetTool("sync", $"-- --format {format}");
    }

    [MenuItem("FeatherFramework/Config/Select Import Settings")]
    private static void SelectImportSettings()
    {
        Selection.activeObject = GetOrCreateSettings();
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    private static void RunSheetTool(string command, string extraArguments = "")
    {
        if (!Directory.Exists(ToolDirectory))
        {
            Debug.LogError($"SheetTool directory was not found: {ToolDirectory}");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = Application.platform == RuntimePlatform.WindowsEditor ? "npm.cmd" : "npm",
            Arguments = $"run {command}{extraArguments}",
            WorkingDirectory = ToolDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using Process process = Process.Start(startInfo);
        if (process == null)
        {
            Debug.LogError("Failed to start SheetTool process.");
            return;
        }

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            Debug.Log(stdout.Trim());
        }

        if (process.ExitCode != 0)
        {
            Debug.LogError(string.IsNullOrWhiteSpace(stderr) ? $"SheetTool exited with code {process.ExitCode}" : stderr.Trim());
            return;
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            Debug.LogWarning(stderr.Trim());
        }

        AssetDatabase.Refresh();
        Debug.Log($"Excel config {command} finished.");
    }

    private static ConfigImportSettings GetOrCreateSettings()
    {
        ConfigImportSettings settings = AssetDatabase.LoadAssetAtPath<ConfigImportSettings>(SettingsAssetPath);
        if (settings != null)
        {
            return settings;
        }

        string directory = Path.GetDirectoryName(SettingsAssetPath);
        if (!AssetDatabase.IsValidFolder(directory))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }

        settings = ScriptableObject.CreateInstance<ConfigImportSettings>();
        AssetDatabase.CreateAsset(settings, SettingsAssetPath);
        AssetDatabase.SaveAssets();
        return settings;
    }
}
