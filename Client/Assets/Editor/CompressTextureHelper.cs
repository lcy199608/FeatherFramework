using System.Collections;
using UnityEngine;
using UnityEditor;
using System.IO;
public class CompressTextureHelper : EditorWindow
{
    private enum Platform
    {
        Android,
        iPhone,
        Standalone,
        WebGL
    }

    private enum CompressMode
    {
        普通压缩, //最保守的压缩，选择比最大边大一号的size压缩，效果可能会不明显
        智能压缩, //选择最大边接近的size，比普通压缩压缩力度大
        根据面积压缩, //根据面积压缩，选择比自己面积大一号的，比较保守
        根据面积强力压缩 //根据面积压缩，选择与自己最接近的
    }

    private CompressMode mode = CompressMode.普通压缩;

    private bool isReset;

    private bool isAndroid;
    private bool isIOS;
    private bool isStandalone;
    private bool isWebGL;

    private int quality;
    private TextureImporterFormat format = TextureImporterFormat.ASTC_6x6;

    private TextureImporterPlatformSettings settings;

    //存储路径
    static ArrayList ImagePathlist;

    [MenuItem("LcyFramework/图片压缩/打开工具")]
    static void Init()
    {
        GetWindow(typeof(CompressTextureHelper));
    }

    private void Awake()
    {
        settings = new TextureImporterPlatformSettings();
        settings.overridden = true;
        //settings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
        //settings.textureCompression = TextureImporterCompression.Uncompressed;
        //settings.compressionQuality = 100;
        //settings.crunchedCompression = true;
        //settings.allowsAlphaSplitting = true;
        //settings.androidETC2FallbackOverride = AndroidETC2FallbackOverride.UseBuildSettings;
    }

    //界面
    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUI.DropShadowLabel(new Rect(0, -5, position.width, 20), "一键压缩工具v1.0");
        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(isReset); //与end呼应，中间的都灰色禁用
        //GUI.enabled = isReset; //和上面功能类似，结束位置用GUI.enabled = true;

        mode = (CompressMode)EditorGUILayout.EnumPopup("压缩模式", mode);
        EditorGUILayout.Space();

        format = (TextureImporterFormat)EditorGUILayout.EnumPopup("压图格式", format);
        EditorGUILayout.Space();

        EditorGUI.EndDisabledGroup();
        //GUI.enabled = true;

        EditorGUILayout.LabelField("压缩平台：");
        //EditorGUILayout.BeginHorizontal(); //开始水平布局
        isAndroid = EditorGUILayout.Toggle("Android", isAndroid);
        isIOS = EditorGUILayout.Toggle("IOS", isIOS);
        isStandalone = EditorGUILayout.Toggle("PC,Mac&Linux", isStandalone);
        isWebGL = EditorGUILayout.Toggle("WebGL", isWebGL);
        //EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        isReset = EditorGUILayout.Toggle("还原为导入时默认格式", isReset);
        EditorGUILayout.Space();

        if (GUILayout.Button("一键压缩"))
        {
            CompressorIma();
        }
        EditorGUILayout.Space();

        if (GUILayout.Button("单个文件夹压缩"))
        {
            CompressorIma1();
        }
        EditorGUILayout.Space();

        if (GUILayout.Button("选中图片压缩"))
        {
            CompressorIma2();
        }
    }

    //获取所有图片路径并保存
    void GetDirs(string path1)
    {
        foreach (string path in Directory.GetFiles(path1))
        {
            //获取所有文件夹中后缀符合要求对象的路径  
            if (System.IO.Path.GetExtension(path) == ".jpg" || System.IO.Path.GetExtension(path) == ".png" || System.IO.Path.GetExtension(path) == ".JPG" || System.IO.Path.GetExtension(path) == ".PNG")
            {
                ImagePathlist.Add(path.Substring(path.IndexOf(path1)));
            }
        }
        if (Directory.GetDirectories(path1).Length > 0)  //遍历所有文件夹  
        {
            foreach (string path11 in Directory.GetDirectories(path1))
            {
                GetDirs(path11);
            }
        }
    }

    //处理资源
    void opImage(string path)
    {
        try
        {
            TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath(path);
            Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(path);

            int size = 1024;
            GetSize(texture, ref size);

            ChoosePlatformAndHandleIma(ti, size);
        }
        catch (System.Exception)
        {
            //因为某些原因未被成功压缩的图片，请手动查看路径
            Debug.LogError(path);
        }
    }

    //一键压缩
    void CompressorIma()
    {
        if (!JudgeChoose())
            return;

        if (ImagePathlist == null)
        {
            ImagePathlist = new ArrayList();
        }
        else
        {
            ImagePathlist.Clear();
        }

        GetDirs("Assets");

        bool temp = EditorUtility.DisplayDialog("提示", "一共需要处理" + ImagePathlist.Count + "张图片", "确定", "取消");

        if (temp)
        {
            EditorUtility.DisplayProgressBar("进度", "0/" + ImagePathlist.Count, 0);
            for (int i = 0; i < ImagePathlist.Count; i++)
            {
                opImage((string)ImagePathlist[i]);
                EditorUtility.DisplayProgressBar("进度", (i + 1) + "/" + ImagePathlist.Count, ((i + 1) / (float)ImagePathlist.Count));
            }
            EditorUtility.ClearProgressBar();
        }
        else
        {
            return;
        }
    }

    //单个文件夹压缩
    void CompressorIma1()
    {
        if (!JudgeChoose())
            return;

        object[] selection = (object[])Selection.objects;
        //判断是否有对象被选中 并且是一个对象  不带后缀的
        if (selection.Length != 1)
        {
            EditorUtility.DisplayDialog("提示", "请选择要压缩的文件夹！(要在Project视窗的右侧选取）", "确定");
            return;
        }


        string objPath = AssetDatabase.GetAssetPath((Object)selection[0]);
        if (objPath.Contains("."))
        {
            EditorUtility.DisplayDialog("提示", "不是文件夹，请重现选取！（要在Project视窗的右侧选取）", "确定");
            return;
        }

        string fullPath = objPath;

        if (ImagePathlist == null)
        {
            ImagePathlist = new ArrayList();
        }
        else
        {
            ImagePathlist.Clear();
        }

        GetDirs(fullPath);

        bool temp = EditorUtility.DisplayDialog("提示", "一共需要处理" + ImagePathlist.Count + "张图片", "确定", "取消");

        if (temp)
        {
            EditorUtility.DisplayProgressBar("进度", "0/" + ImagePathlist.Count, 0);
            for (int i = 0; i < ImagePathlist.Count; i++)
            {
                opImage((string)ImagePathlist[i]);
                EditorUtility.DisplayProgressBar("进度", (i + 1) + "/" + ImagePathlist.Count, ((i + 1) / (float)ImagePathlist.Count));
            }
            EditorUtility.ClearProgressBar();
        }
        else
        {
            return;
        }
    }

    //压缩选中图片
    void CompressorIma2()
    {
        if (!JudgeChoose())
            return;

        if (ImagePathlist == null)
        {
            ImagePathlist = new ArrayList();
        }
        else
        {
            ImagePathlist.Clear();
        }

        object[] selection = (object[])Selection.objects;

        if (selection.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "请选中要压缩的图片！", "确定");
            return;
        }

        foreach (Object obj in selection)
        {
            string objPath = AssetDatabase.GetAssetPath(obj);

            if (objPath.EndsWith(".jpg") || objPath.EndsWith(".png") || objPath.EndsWith(".JPG") || objPath.EndsWith(".PNG"))
            {
                ImagePathlist.Add(objPath);
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "请选择正确的图片格式！", "确定");
                return;
            }
        }
        if (ImagePathlist.Count > 0)
        {
            bool temp = EditorUtility.DisplayDialog("提示", "一共需要处理" + ImagePathlist.Count + "张图片", "确定", "取消");

            if (temp)
            {
                EditorUtility.DisplayProgressBar("进度", "0/" + ImagePathlist.Count, 0);
                for (int i = 0; i < ImagePathlist.Count; i++)
                {
                    opImage((string)ImagePathlist[i]);
                    EditorUtility.DisplayProgressBar("进度", (i + 1) + "/" + ImagePathlist.Count, ((i + 1) / (float)ImagePathlist.Count));
                }
                EditorUtility.ClearProgressBar();
            }
            else
            {
                return;
            }
        }
    }

    void GetSize(Texture texture, ref int size)
    {
        if (isReset)
        {
            size = 2048;
            return;
        }

        if (mode != CompressMode.根据面积压缩 && mode != CompressMode.根据面积强力压缩)
        {
            float judge = texture.width >= texture.height ? texture.width : texture.height;

            if (judge <= 32)
            {
                size = 32;
            }
            else if (judge <= 64)
            {
                size = 64;
            }
            else if (judge <= 128)
            {
                size = 128;
            }
            else if (judge <= 256)
            {
                size = 256;
            }
            else if (judge <= 512)
            {
                size = 512;
            }
            else if (judge <= 1024)
            {
                size = 1024;
            }
            else if (judge <= 2048)
            {
                size = 2048;
            }
            else if (judge <= 4096)
            {
                size = 4096;
            }
            else if (judge <= 8192)
            {
                size = 8192;
            }
            else
            {
                size = 1024;
            }

            if (mode == CompressMode.智能压缩)
            {
                if (size - judge > judge - size / 2)
                    size /= 2;
            }
            return;
        }

        int texArea = texture.width * texture.height;

        if (texArea <= 32 * 32)
        {
            size = 32;
        }
        else if (texArea <= 64 * 64)
        {
            size = 64;
        }
        else if (texArea <= 128 * 128)
        {
            size = 128;
        }
        else if (texArea <= 256 * 256)
        {
            size = 256;
        }
        else if (texArea <= 512 * 512)
        {
            size = 512;
        }
        else if (texArea <= 1024 * 1024)
        {
            size = 1024;
        }
        else if (texArea <= 2048 * 2048)
        {
            size = 2048;
        }
        else if (texArea <= 4096 * 4096)
        {
            size = 4096;
        }
        else if (texArea <= 8192 * 8192)
        {
            size = 8192;
        }
        else
        {
            size = 1024;
        }

        if (mode == CompressMode.根据面积强力压缩)
        {
            if (size * size - texArea > texArea - size * size / 2)
                size /= 2;
            Debug.Log(size);
        }
    }

    //根据选择的平台处理图片
    void ChoosePlatformAndHandleIma(TextureImporter ti, int size)
    {
        settings.maxTextureSize = size;
        if (isReset)
        {
            settings.overridden = false;
            format = TextureImporterFormat.Automatic;
        }
        settings.format = format;
        if (isAndroid)
        {
            settings.name = Platform.Android.ToString();
            HandleIma(ti);
        }

        if (isIOS)
        {
            settings.name = Platform.iPhone.ToString();
            HandleIma(ti);
        }

        if (isStandalone)
        {
            settings.name = Platform.Standalone.ToString();
            HandleIma(ti);
        }

        //木鸡推荐的压缩方法 默认DXT1Crunched
        if (isWebGL)
        {
            settings.name = Platform.WebGL.ToString();

            if (ti.DoesSourceTextureHaveAlpha())
            {
                settings.format = TextureImporterFormat.DXT5Crunched;
            }

            settings.compressionQuality = 50;

            HandleIma(ti);
        }
    }

    //处理图片
    void HandleIma(TextureImporter ti)
    {
        ti.SetPlatformTextureSettings(settings);
        ti.SaveAndReimport();
    }

    //不选择输出平台提醒
    bool JudgeChoose()
    {
        if (!isAndroid && !isIOS && !isStandalone && !isWebGL)
        {
            EditorUtility.DisplayDialog("提示", "请勾选要压缩的平台！", "确定");
            return false;
        }
        return true;
    }
}


