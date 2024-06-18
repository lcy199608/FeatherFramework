using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ExcelTool : MonoBehaviour
{
    [MenuItem("Tools/Excel/读取配置表")]
    private static void RunMyBatFile()
    {
        // 设置.bat文件的路径
        string batFilePath = Path.Combine(Path.Combine(Application.dataPath, "../../Config/MiniTemplate/"), "gen.bat");

        // 创建并运行进程
        Process process = new Process();
        process.StartInfo.FileName = batFilePath;
        //process.StartInfo.CreateNoWindow = true; // 不显示窗口
        //process.StartInfo.UseShellExecute = false; // 不使用系统外壳启动进程
        // 注册进程退出事件
        process.Exited += ProcessExited;
        process.Start(); // 启动进程
        process.WaitForExit(); // 等待进程结束
    }

    private static void ProcessExited(object sender, EventArgs e)
    {
        // 在进程完成后执行资源刷新
        UnityEditor.AssetDatabase.Refresh();
        UnityEngine.Debug.Log("Read Config Over");
    }
}
