using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Feather.Tests
{
    public sealed class PoolActivationProbe : MonoBehaviour
    {
        public bool TokenReady;
        private void OnEnable() => TokenReady = GetComponent<PoolToken>()?.Owner != null;
    }

    // Runs actual Unity activation/destruction, while keeping production bootstrap/save data inactive.
    public sealed class UIPlayLifecycleTests
    {
        [UnitySetUp]
        public IEnumerator Enter()
        {
            var sentinel = new GameObject("UITestInactiveBootstrap");
            sentinel.SetActive(false);
            sentinel.AddComponent<FrameworkHost>();
            yield return new EnterPlayMode();
        }

        [UnityTearDown]
        public IEnumerator Exit()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            foreach (var host in Object.FindObjectsOfType<FrameworkHost>(true))
                if (host.name == "UITestInactiveBootstrap") Object.DestroyImmediate(host.gameObject);
        }

        [UnityTest]
        public IEnumerator PoolActivationAndZeroFrameTimersHaveStableIdentity()
        {
            var holder = new GameObject("PoolPlayFixture");
            var template = new GameObject("InactiveTemplate");
            template.SetActive(false);
            template.AddComponent<PoolActivationProbe>();
            var runner = holder.AddComponent<UIMgr>();
            var assets = new ResMgr(runner);
            var pools = new PoolMgr(assets, holder.transform);
            var timers = new TimerMgr(runner);
            GameObject instance = null;
            try
            {
                pools.GetCloneObj(template, value => instance = value);
                Assert.That(instance.activeSelf, Is.True);
                Assert.That(instance.GetComponent<PoolActivationProbe>().TokenReady, Is.True);
                pools.PushObj(instance);
                pools.GetCloneObj(template, value => instance = value);
                Assert.That(instance.GetComponent<PoolActivationProbe>().TokenReady, Is.True);
                int calls = 0;
                TimerHandle nested = default;
                var first = timers.AfterFrames(0, () => { calls++; nested = timers.AfterSeconds(100, () => { }); });
                Assert.That(calls, Is.Zero);
                yield return null;
                yield return null;
                Assert.That(calls, Is.EqualTo(1));
                Assert.That(first.IsValid, Is.False);
                first.Dispose();
                Assert.That(nested.IsValid, Is.True);
            }
            finally
            {
                timers.Shutdown(); pools.Shutdown(); assets.Shutdown();
                if (instance != null) Object.Destroy(instance);
                Object.Destroy(template); Object.Destroy(holder);
            }
        }

        [UnityTest]
        public IEnumerator AudioOwnerDestructionStopsLoopAndRejectsNonFiniteInputs()
        {
            string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FeatherAudio-" + System.Guid.NewGuid().ToString("N") + ".es3");
            var holder = new GameObject("AudioPlayFixture");
            var owner = new GameObject("AudioOwner");
            var clip = AudioClip.Create("TestTone", 44100, 1, 44100, false);
            var save = new SaveDataMgr();
            save.Initialize(new ES3Settings(false) { path = path, location = ES3.Location.File });
            var audio = holder.AddComponent<AudioMgr>();
            audio.Initialize((name, done) => done(clip), save);
            try
            {
                Assert.Throws<System.ArgumentOutOfRangeException>(() => audio.PlayAudio("tone", AudioType.EFFECT, float.NaN));
                Assert.Throws<System.ArgumentOutOfRangeException>(() => audio.SetBGMVolume(float.PositiveInfinity));
                var voice = audio.PlayLoopAudio("tone", AudioType.EFFECT);
                var action = owner.AddComponent<AudioAction>();
                typeof(AudioAction).GetField("currentVoice", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(action, voice);
                Object.Destroy(owner);
                yield return null;
                Assert.That(voice.IsValid, Is.False);
            }
            finally
            {
                audio.Shutdown();
                Object.Destroy(owner); Object.Destroy(holder); Object.Destroy(clip);
                foreach (var file in new[] { path, path + ".tmp", path + ".bak" }) if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
            }
        }

        [UnityTest]
        public IEnumerator ChildActivationAndDeferredDestruction_FollowOwner()
        {
            var holder = new GameObject("UIPlayFixture");
            var prefabs = new Dictionary<string, GameObject>();
            var canvas = Make(holder.transform, "UICanvas", typeof(Canvas));
            foreach (var name in new[] { "BottomLayer", "MiddleLayer", "TopLayer", "SystemLayer" })
            {
                var layer = Make(canvas.transform, name);
                layer.SetActive(true);
            }
            prefabs[UIMgr.uiPath + "UICanvas.prefab"] = canvas;
            prefabs[UIMgr.uiPath + nameof(TestRoot) + ".prefab"] = Make(holder.transform, nameof(TestRoot), typeof(TestRoot));
            prefabs[UIMgr.uiPath + nameof(TestPage) + ".prefab"] = Make(holder.transform, nameof(TestPage), typeof(TestPage));
            prefabs[UIMgr.uiPath + nameof(OtherPage) + ".prefab"] = Make(holder.transform, nameof(OtherPage), typeof(OtherPage));
            prefabs[UIMgr.uiPath + nameof(TestChild) + ".prefab"] = Make(holder.transform, nameof(TestChild), typeof(TestChild));
            var ui = holder.AddComponent<UIMgr>();
            ui.Initialize(path => prefabs[path], (path, done) => done(prefabs[path]));
            try
            {
                var root = ui.OpenRoot<TestRoot>().Result;
                var page = ui.OpenPage<TestPage>().Result;
                var child = ui.OpenChild<TestChild>(page).Result;
                var childPanel = (TypedTestPanel)child.Panel;
                Assert.That(childPanel.gameObject.activeInHierarchy, Is.True);
                Assert.That(childPanel.EnabledParent, Is.EqualTo("MiddleLayer"));
                var next = ui.OpenPage<OtherPage>().Result;
                yield return null;
                Assert.That(childPanel.gameObject.activeInHierarchy, Is.False);
                ui.Close(next);
                Assert.That(childPanel.gameObject.activeInHierarchy, Is.True);
                Assert.That(childPanel.EnableCount, Is.EqualTo(2));
                ui.Close(page);
                Assert.That(root.IsVisible, Is.True);
                Assert.That(childPanel.Closes, Is.EqualTo(1));
                Assert.That(childPanel.Disposes, Is.EqualTo(1));
                Assert.That(childPanel.gameObject.activeSelf, Is.False);
                yield return null; // Unity destroys at the end of the frame.
                Assert.That(childPanel == null, Is.True);
            }
            finally
            {
                ui.Shutdown();
                Object.Destroy(holder);
            }
        }

        private static GameObject Make(Transform parent, string name, params System.Type[] components)
        {
            var instance = new GameObject(name, typeof(RectTransform));
            instance.SetActive(false);
            instance.transform.SetParent(parent, false);
            foreach (var component in components) instance.AddComponent(component);
            return instance;
        }
    }
}
