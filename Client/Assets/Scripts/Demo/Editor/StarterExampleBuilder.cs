using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class StarterExampleBuilder
{
    public const string MenuPath = "Assets/Scenes/Starter/Menu.unity";
    public const string GamePath = "Assets/Scenes/Starter/Game.unity";

    [MenuItem("FeatherFramework/Examples/Create Starter Example")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Exit Play Mode first.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Directory.CreateDirectory("Assets/Scenes/Starter");
            Directory.CreateDirectory("Assets/Res/Starter");
            Directory.CreateDirectory("Assets/Res/UI");
            if (!File.Exists("Assets/Res/Starter/Level.txt"))
                File.WriteAllText("Assets/Res/Starter/Level.txt", "Starter level: collect coins, return to menu, then continue your saved progress.");
            AssetDatabase.Refresh();
            MakePanel(false);
            MakePanel(true);
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (string path in new[] { MenuPath, GamePath })
            {
                var existing = scenes.Find(scene => scene.path == path);
                if (existing == null) scenes.Add(new EditorBuildSettingsScene(path, true));
                else existing.enabled = true;
            }
            var enabled = scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToList();
            MakeScene(MenuPath, false, enabled.IndexOf(MenuPath), enabled.IndexOf(GamePath));
            MakeScene(GamePath, true, enabled.IndexOf(MenuPath), enabled.IndexOf(GamePath));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }
        finally { if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup); }
        Debug.Log("Starter example ready: open Assets/Scenes/Starter/Menu.unity and press Play.");
    }

    private static void MakeScene(string path, bool gameplay, int menuIndex, int gameIndex)
    {
        // Never overwrite a scene already customized by the user.
        if (File.Exists(path))
        {
            var existing = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in existing.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<StarterEntry>(true))
                { component.menuScene = MenuPath; component.gameScene = GamePath; }
            EditorSceneManager.SaveScene(existing);
            return;
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var entry = new GameObject("StarterEntry").AddComponent<StarterEntry>();
        entry.gameplay = gameplay;
        entry.menuScene = MenuPath;
        entry.gameScene = GamePath;
        var canvas = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Res/UI/UICanvas.prefab");
        if (canvas == null || canvas.GetComponentInChildren<EventSystem>(true) == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        EditorSceneManager.SaveScene(scene, path);
    }

    private static void MakePanel(bool game)
    {
        string name = game ? nameof(StarterGamePanel) : nameof(StarterMenuPanel);
        string path = "Assets/Res/UI/" + name + ".prefab";
        if (File.Exists(path))
        {
            var existing = PrefabUtility.LoadPrefabContents(path);
            try { SetUILayer(existing); PrefabUtility.SaveAsPrefabAsset(existing, path); }
            finally { PrefabUtility.UnloadPrefabContents(existing); }
            return;
        }
        var root = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        try
        {
            root.GetComponent<UnityEngine.UI.Image>().color = new Color(0.035f, 0.065f, 0.12f, 1);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Label(root.transform, "Title", game ? "STARTER / PLAY" : "FEATHER / STARTER", 130, 36);
            var status = Label(root.transform, "Status", "Ready", 55, 22);
            var primary = Button(root.transform, game ? "Collect coin & save" : "Start / Continue", -35);
            if (game)
            {
                var panel = root.AddComponent<StarterGamePanel>();
                panel.status = status;
                panel.collectButton = primary;
                panel.backButton = Button(root.transform, "Return to menu", -105);
            }
            else
            {
                var panel = root.AddComponent<StarterMenuPanel>();
                panel.status = status;
                panel.startButton = primary;
            }
            SetUILayer(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static UnityEngine.UI.Text Label(Transform parent, string name, string value, float y, int size)
    {
        var node = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
        node.transform.SetParent(parent, false);
        var rect = (RectTransform)node.transform;
        rect.sizeDelta = new Vector2(760, 65);
        rect.anchoredPosition = new Vector2(0, y);
        var label = node.GetComponent<UnityEngine.UI.Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = value;
        return label;
    }

    private static void SetUILayer(GameObject root)
    {
        int layer = LayerMask.NameToLayer("UI");
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
            if (child.gameObject.layer == 0) child.gameObject.layer = layer;
    }

    private static UnityEngine.UI.Button Button(Transform parent, string title, float y)
    {
        var node = new GameObject("Button", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        node.transform.SetParent(parent, false);
        var rect = (RectTransform)node.transform;
        rect.sizeDelta = new Vector2(350, 54);
        rect.anchoredPosition = new Vector2(0, y);
        node.GetComponent<UnityEngine.UI.Image>().color = new Color(0.08f, 0.37f, 0.60f, 1);
        var label = Label(node.transform, "Label", title, 0, 22);
        label.rectTransform.sizeDelta = rect.sizeDelta;
        return node.GetComponent<UnityEngine.UI.Button>();
    }
}
