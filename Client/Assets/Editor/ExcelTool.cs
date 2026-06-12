using System;
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
        RunSheetTool("sync", $"--format {format}");
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

        ProcessStartInfo startInfo = CreateSheetToolStartInfo(command, extraArguments);

        Process process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to start SheetTool process.\nCommand: {startInfo.FileName} {startInfo.Arguments}\n{exception.Message}");
            return;
        }

        if (process == null)
        {
            Debug.LogError("Failed to start SheetTool process.");
            return;
        }

        using (process)
        {
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
        }

        AssetDatabase.Refresh();
        Debug.Log($"Excel config {command} finished.");
    }

    private static ProcessStartInfo CreateSheetToolStartInfo(string command, string extraArguments)
    {
        string normalizedExtraArguments = NormalizeExtraArguments(extraArguments);
        string nodeExecutable = ResolveNodeExecutable();
        if (string.IsNullOrEmpty(nodeExecutable))
        {
            throw new InvalidOperationException(
                "Unable to locate node. Install Node.js and ensure node is available at a standard path such as /opt/homebrew/bin/node or /usr/local/bin/node.");
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = nodeExecutable,
            Arguments = $"./cli.js {command}{normalizedExtraArguments}",
            WorkingDirectory = ToolDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        string nodeDirectory = Path.GetDirectoryName(nodeExecutable);
        if (!string.IsNullOrEmpty(nodeDirectory))
        {
            string currentPath = startInfo.EnvironmentVariables["PATH"] ?? string.Empty;
            if (!currentPath.Contains(nodeDirectory))
            {
                startInfo.EnvironmentVariables["PATH"] = string.IsNullOrEmpty(currentPath)
                    ? nodeDirectory
                    : $"{nodeDirectory}{Path.PathSeparator}{currentPath}";
            }
        }

        return startInfo;
    }

    private static string NormalizeExtraArguments(string extraArguments)
    {
        if (string.IsNullOrWhiteSpace(extraArguments))
        {
            return string.Empty;
        }

        string normalizedArguments = extraArguments.Trim();
        if (normalizedArguments.StartsWith("-- "))
        {
            normalizedArguments = normalizedArguments.Substring(3).TrimStart();
        }

        return string.IsNullOrEmpty(normalizedArguments)
            ? string.Empty
            : $" {normalizedArguments}";
    }

    private static string ResolveNodeExecutable()
    {
        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            return "node.exe";
        }

        string[] candidatePaths =
        {
            "/opt/homebrew/bin/node",
            "/usr/local/bin/node",
            "/usr/bin/node"
        };

        foreach (string candidatePath in candidatePaths)
        {
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        return null;
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
