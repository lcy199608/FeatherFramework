using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ExcelTool : MonoBehaviour
{
    [MenuItem("Tools/读取配置表")]
    private static void RunMyBatFile()
    {
        // 设置.bat文件的路径
        string batFilePath = Path.Combine(Path.Combine(Application.dataPath, "../../Config/MiniTemplate/"), "gen.bat");

        // 创建并运行进程
        Process process = new Process();
        process.StartInfo.FileName = batFilePath;
        //process.StartInfo.CreateNoWindow = true; // 不显示窗口
        //process.StartInfo.UseShellExecute = false; // 不使用系统外壳启动进程
        process.Start(); // 启动进程
        process.WaitForExit(); // 等待进程结束
        UnityEngine.Debug.Log("Read Config Over");
    }
}
