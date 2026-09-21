using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class AutoGenerateUIScript
{
    private const string OutputDirectory = "Assets/Scripts/Game/UI";

    private static readonly IReadOnlyDictionary<string, Type> BindingTypes = new Dictionary<string, Type>
    {
        { "Img", typeof(Image) },
        { "Btn", typeof(Button) },
        { "Txt", typeof(Text) },
        { "Tran", typeof(Transform) }
    };

    [MenuItem("FeatherFramework/生成或刷新UI脚本 %g")]
    public static void BuildUIScript()
    {
        if (!EditorUtility.DisplayDialog("提示", "确定生成或刷新UI绑定脚本吗？", "确定", "取消"))
        {
            return;
        }

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scripts/Game/UI"));
        var selected = Selection.gameObjects;
        if (selected.Select(value => SanitizeIdentifier(value.name)).Distinct().Count() != selected.Length)
            throw new InvalidOperationException("Selected panels produce conflicting class names.");
        foreach (GameObject selectedObject in selected)
        {
            GenerateFor(selectedObject);
        }
        AssetDatabase.Refresh();
    }

    private static void GenerateFor(GameObject root)
    {
        string className = SanitizeIdentifier(root.name);
        List<Binding> bindings = CollectBindings(root.transform);
        if (bindings == null)
        {
            return;
        }

        string businessPath = $"{OutputDirectory}/{className}.cs";
        string generatedPath = $"{OutputDirectory}/{className}.Bindings.g.cs";
        string sourcePath = AssetDatabase.GetAssetPath(root);
        string guid = string.IsNullOrEmpty(sourcePath) ? "" : AssetDatabase.AssetPathToGUID(sourcePath);
        string marker = "// Source asset GUID: " + guid;
        if (File.Exists(generatedPath) && guid.Length > 0)
        {
            var previous = Regex.Match(File.ReadAllText(generatedPath), @"// Source asset GUID: (\w+)");
            if (previous.Success && previous.Groups[1].Value != guid)
                throw new InvalidOperationException($"{className} is already generated for another prefab.");
        }
        if (TypeCache.GetTypesDerivedFrom<PanelBase>().Any(type => type.Name == className && type.FullName != className))
            throw new InvalidOperationException($"Panel class name conflicts with an existing namespaced type: {className}");
        if (!File.Exists(businessPath))
        {
            File.WriteAllText(businessPath, CreateBusinessScript(className), new UTF8Encoding(false));
        }
        File.WriteAllText(generatedPath, CreateBindingScript(className, bindings) + marker + "\n", new UTF8Encoding(false));
        Debug.Log($"Generated UI bindings: {generatedPath}", root);
    }

    private static List<Binding> CollectBindings(Transform root)
    {
        var result = new List<Binding>();
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Contains("/")) throw new InvalidOperationException("UI node names cannot contain '/'.");
            var siblingNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (Transform sibling in child)
                if (!siblingNames.Add(sibling.name)) throw new InvalidOperationException($"Duplicate child name under {child.name}: {sibling.name}");
            if (child == root)
            {
                continue;
            }

            var match = BindingTypes.FirstOrDefault(pair => child.name.StartsWith(pair.Key, StringComparison.Ordinal));
            if (string.IsNullOrEmpty(match.Key))
            {
                continue;
            }
            if (child.GetComponent(match.Value) == null)
            {
                Debug.LogError($"UI binding {GetPath(root, child)} requires component {match.Value.Name}.", child);
                return null;
            }

            result.Add(new Binding(CreateUniqueMemberName(child.name, usedNames), match.Value.Name, GetPath(root, child)));
        }
        return result;
    }

    private static string CreateBusinessScript(string className)
    {
        return $@"using UnityEngine;

public partial class {className} : PanelBase
{{
    public override UIType Type => UIType.Page;

    public override void OnInit()
    {{
        BindGeneratedReferences();
    }}

    public override void OnShow()
    {{
    }}

    public override void OnHide()
    {{
    }}

    public override void OnClose()
    {{
        // 释放本次打开持有的订阅、计时器等；OnInit 中建立的实例资源在 OnDispose 清理。
    }}
}}
";
    }

    private static string CreateBindingScript(string className, IEnumerable<Binding> bindings)
    {
        var members = new StringBuilder();
        var assignments = new StringBuilder();
        foreach (Binding binding in bindings)
        {
            members.AppendLine($"    private {binding.TypeName} {binding.MemberName};");
            string escaped = binding.Path.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
            assignments.AppendLine($"        {binding.MemberName} = transform.Find(\"{escaped}\").GetComponent<{binding.TypeName}>();");
        }

        return $@"// <auto-generated />
using UnityEngine;
using UnityEngine.UI;

public partial class {className}
{{
{members}    private void BindGeneratedReferences()
    {{
{assignments}    }}
}}
";
    }

    private static string GetPath(Transform root, Transform child)
    {
        var parts = new Stack<string>();
        Transform current = child;
        while (current != null && current != root)
        {
            parts.Push(current.name);
            current = current.parent;
        }
        return string.Join("/", parts);
    }

    private static string CreateUniqueMemberName(string objectName, ISet<string> usedNames)
    {
        string baseName = "_" + SanitizeIdentifier(objectName);
        string name = baseName;
        int suffix = 2;
        while (!usedNames.Add(name))
        {
            name = baseName + suffix;
            suffix++;
        }
        return name;
    }

    private static string SanitizeIdentifier(string value)
    {
        using (var provider = new Microsoft.CSharp.CSharpCodeProvider())
            if (provider.IsValidIdentifier(value)) return value;
        string identifier = Regex.Replace(value ?? string.Empty, "[^a-zA-Z0-9_]", "_");
        if (string.IsNullOrEmpty(identifier))
        {
            throw new InvalidOperationException("A UI object name cannot be converted to a C# identifier.");
        }
        if (char.IsDigit(identifier[0]))
        {
            identifier = "_" + identifier;
        }
        using (var provider = new Microsoft.CSharp.CSharpCodeProvider())
            if (!provider.IsValidIdentifier(identifier)) identifier = "_" + identifier;
        return identifier;
    }

    private sealed class Binding
    {
        public string MemberName { get; }
        public string TypeName { get; }
        public string Path { get; }

        public Binding(string memberName, string typeName, string path)
        {
            MemberName = memberName;
            TypeName = typeName;
            Path = path;
        }
    }
}
