using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Feather.Tests
{
    public sealed class StarterFlowPlayTests
    {
        [UnitySetUp]
        public IEnumerator Enter()
        {
            var sentinel = new GameObject("StarterTestInactiveBootstrap");
            sentinel.SetActive(false);
            sentinel.AddComponent<FrameworkHost>();
            yield return new EnterPlayMode();
        }

        [UnityTearDown]
        public IEnumerator Exit()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            foreach (var host in Object.FindObjectsOfType<FrameworkHost>(true))
                if (host.name == "StarterTestInactiveBootstrap") Object.DestroyImmediate(host.gameObject);
        }

        private static IEnumerator Until(Func<bool> condition)
        {
            float deadline = Time.realtimeSinceStartup + 15;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(condition(), Is.True, "Starter operation timed out.");
        }

        [UnityTest]
        public IEnumerator MenuGameSaveReturnAndScopedAssets_WorkTogether()
        {
            var holder = new GameObject("StarterTestServices");
            Object.DontDestroyOnLoad(holder);
            var ui = holder.AddComponent<UIMgr>();
            var assets = new ResMgr(ui);
            var events = new EventCenter();
            var scenes = new SceneMgr(ui, events);
            var settings = new ES3Settings(false) { path = Path.Combine(Path.GetTempPath(), "FeatherStarter-" + Guid.NewGuid().ToString("N") + ".es3"), location = ES3.Location.File };
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var save = new SaveDataMgr();
            var audio = holder.AddComponent<AudioMgr>();
            var tables = new ConfigMgr();
            try
            {
                save.Initialize(settings);
                save.LoadData(0);
                tables.InitConfig(false, false);
                audio.Initialize(assets, save);
                ui.Initialize(assets);
                ui.CreateUICanvas(false);
                var services = new FrameworkServices(config, events, assets, save, tables, null, null,
                    new TimerMgr(ui), scenes, new PoolMgr(assets, holder.transform), audio, ui);
                Framework.SetServices(services);
                Assert.That(services.HasLocalization || services.HasRedDots, Is.False);
                Assert.Throws<InvalidOperationException>(() => { var unused = services.Localization; });
                Assert.Throws<InvalidOperationException>(() => { var unused = services.RedDots; });
                ui.ActivateUICanvas();

                // Real Addressables load; scopes reject sharing and suppress callbacks after disposal.
                const string address = "Res/Starter/Level.txt";
                var scope = assets.CreateScope();
                TextAsset loaded = null;
                scope.LoadAsync<TextAsset>(address, value => loaded = value);
                Assert.Throws<InvalidOperationException>(() => assets.Load<TextAsset>(address));
                using (var other = assets.CreateScope())
                    Assert.Throws<InvalidOperationException>(() => other.LoadAsync<TextAsset>(address, _ => { }));
                yield return Until(() => loaded != null);
                Assert.That(loaded.text, Does.Contain("Starter level"));
                scope.Dispose();
                scope.Dispose();
                bool cancelledCallback = false;
                using (var cancelled = assets.CreateScope())
                    cancelled.LoadAsync<TextAsset>(address, _ => cancelledCallback = true);
                yield return null;
                Assert.That(cancelledCallback, Is.False);

                int menu = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(StarterExampleBuilder.MenuPath);
                int game = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(StarterExampleBuilder.GamePath);
                var first = scenes.SwitchAsync(menu);
                Assert.That(scenes.SwitchAsync(menu), Is.SameAs(first));
                Assert.That(scenes.SwitchAsync(game).IsFaulted, Is.True);
                Assert.That(scenes.SwitchAsync(-1).IsFaulted, Is.True);
                Assert.That(scenes.IsLoading, Is.True);
                yield return Until(() => first.IsCompleted);
                Assert.That(first.Status, Is.EqualTo(TaskStatus.RanToCompletion));
                yield return Until(() => ui.TryGetUI<StarterMenuPanel>(out var panel) && panel.Handle.IsVisible);
                ui.TryGetUI<StarterMenuPanel>(out var menuPanel);
                menuPanel.startButton.onClick.Invoke();
                yield return Until(() => ui.TryGetUI<StarterGamePanel>(out var panel) && panel.Handle.IsVisible);
                ui.TryGetUI<StarterGamePanel>(out var gamePanel);
                Assert.That(gamePanel.status.text, Is.EqualTo("Saved coins: 0"));
                Assert.That(gamePanel.collectButton.GetComponent<RectTransform>().rect.width, Is.GreaterThan(0));
                gamePanel.collectButton.onClick.Invoke();
                Assert.That(gamePanel.status.text, Is.EqualTo("Saved coins: 1"));
                gamePanel.backButton.onClick.Invoke();
                yield return Until(() => menuPanel.Handle != null && menuPanel.Handle.IsVisible);
                menuPanel.startButton.onClick.Invoke();
                yield return Until(() => gamePanel.Handle != null && gamePanel.Handle.IsVisible);
                Assert.That(gamePanel.status.text, Is.EqualTo("Saved coins: 1"));
                var reopened = new SaveDataMgr();
                reopened.Initialize(settings);
                reopened.LoadData(0);
                Assert.That(reopened.GetData("FeatherStarter.Progress", new StarterGamePanel.Progress()).Coins, Is.EqualTo(1));
                gamePanel.backButton.onClick.Invoke();
                yield return Until(() => menuPanel.Handle != null && menuPanel.Handle.IsVisible);
            }
            finally
            {
                scenes.Shutdown();
                ui.Shutdown();
                audio.Shutdown();
                assets.ReleaseAll();
                Framework.ClearServices();
                Object.Destroy(holder);
                Object.Destroy(config);
                foreach (var path in new[] { settings.path, settings.path + ".tmp", settings.path + ".bak" })
                    if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
