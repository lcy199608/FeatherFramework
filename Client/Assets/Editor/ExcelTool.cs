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

    private static Process running;
    private static void StopRunning()
    {
        try { if (running != null && !running.HasExited) running.Kill(); }
        catch (Exception exception) { Debug.LogWarning(exception.Message); }
    }

    private static async void RunSheetTool(string command, string extraArguments = "")
    {
        if (running != null) { Debug.LogWarning("SheetTool is already running."); return; }
        Process process = null;
        bool cancelled = false;
        try
        {
            if (!Directory.Exists(ToolDirectory)) throw new DirectoryNotFoundException(ToolDirectory);
            process = Process.Start(CreateSheetToolStartInfo(command, extraArguments));
            if (process == null) throw new InvalidOperationException("Failed to start SheetTool.");
            running = process;
            AssemblyReloadEvents.beforeAssemblyReload += StopRunning;
            EditorApplication.quitting += StopRunning;
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            var deadline = DateTime.UtcNow.AddMinutes(2);
            while (!process.HasExited)
            {
                if (DateTime.UtcNow >= deadline || EditorUtility.DisplayCancelableProgressBar("SheetTool", command + " in progress", 0.5f))
                {
                    cancelled = true;
                    StopRunning();
                    break;
                }
                await System.Threading.Tasks.Task.Delay(100);
            }
            string stdout = await stdoutTask;
            string stderr = await stderrTask;
            if (!string.IsNullOrWhiteSpace(stdout)) Debug.Log(stdout.Trim());
            if (cancelled) { Debug.LogWarning("SheetTool cancelled or timed out. Check outputs before continuing."); return; }
            if (process.ExitCode != 0) throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? "SheetTool failed." : stderr.Trim());
            if (!string.IsNullOrWhiteSpace(stderr)) Debug.LogWarning(stderr.Trim());
            AssetDatabase.Refresh();
            Debug.Log("Excel config " + command + " finished.");
        }
        catch (Exception exception) { Debug.LogError("SheetTool: " + exception.Message); }
        finally
        {
            AssemblyReloadEvents.beforeAssemblyReload -= StopRunning;
            EditorApplication.quitting -= StopRunning;
            EditorUtility.ClearProgressBar();
            running = null;
            process?.Dispose();
        }
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
        string configuredPath = Environment.GetEnvironmentVariable("FEATHER_NODE_PATH");
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] windowsCandidates =
            {
                Path.Combine(userProfile, @".cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"nodejs\node.exe")
            };
            foreach (string candidatePath in windowsCandidates)
            {
                if (File.Exists(candidatePath))
                {
                    return candidatePath;
                }
            }
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
