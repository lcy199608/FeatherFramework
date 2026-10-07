using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Feather.Tests
{
    public sealed class LanguageActivationProbe : MonoBehaviour
    {
        public int Enables;
        private void OnEnable() => Enables++;
    }

    public sealed class LocalizationPlayLifecycleTests
    {
        [UnitySetUp]
        public IEnumerator Enter()
        {
            var sentinel = new GameObject("LocalizationInactiveBootstrap");
            sentinel.SetActive(false);
            sentinel.AddComponent<FrameworkHost>();
            yield return new EnterPlayMode();
        }

        [UnityTearDown]
        public IEnumerator Exit()
        {
            Framework.ClearServices();
            if (Application.isPlaying) yield return new ExitPlayMode();
            foreach (var host in UnityEngine.Object.FindObjectsOfType<FrameworkHost>(true))
                if (host.name == "LocalizationInactiveBootstrap") UnityEngine.Object.DestroyImmediate(host.gameObject);
        }

        [UnityTest]
        public IEnumerator LocalizedComponentsRefreshInactiveTextAndReleaseSubscriptions()
        {
            string file = Path.Combine(Path.GetTempPath(), "FeatherLanguageLifecycle-" + Guid.NewGuid().ToString("N") + ".es3");
            var root = new GameObject("LanguageLifecycleOwner");
            var gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            gameConfig.followSystemLanguage = false;
            gameConfig.defaultLanguage = SystemLanguage.English;
            var events = new EventCenter();
            var save = new SaveDataMgr();
            save.Initialize(new ES3Settings(false) { path = file, location = ES3.Location.File, encryptionType = ES3.EncryptionType.None });
            var config = new ConfigMgr();
            config.InitConfig();
            config.Tables.Language.DataMap[1].English = "Start Game";
            config.Tables.Language.DataMap[1].Arabic = "سلام";
            var language = new LanguageMgr(save, config, events, gameConfig);
            var ui = root.AddComponent<UIMgr>();
            var audio = root.AddComponent<AudioMgr>();
            var assets = new ResMgr(ui);
            var timers = new TimerMgr(ui);
            var pools = new PoolMgr(assets, root.transform);
            var scenes = new SceneMgr(ui, events);
            Framework.SetServices(new FrameworkServices(gameConfig, events, assets, save, config, language, null, timers, scenes, pools, audio, ui));
            var englishSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            var arabicSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            try
            {
                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
                labelObject.transform.SetParent(root.transform);
                var label = labelObject.AddComponent<TextOfEnhance>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.rectTransform.sizeDelta = new Vector2(500, 100);
                label.languageId = 1;
                var spriteObject = new GameObject("Sprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                spriteObject.transform.SetParent(root.transform);
                var sprite = spriteObject.AddComponent<LanguageSpriteSwitch>();
                sprite.EN = englishSprite;
                sprite.variants = new[] { new LanguageSpriteSwitch.Variant { language = SystemLanguage.Arabic, sprite = arabicSprite } };
                var variantObject = new GameObject("VariantController");
                variantObject.transform.SetParent(root.transform);
                var english = new GameObject("English");
                english.transform.SetParent(variantObject.transform);
                var arabic = new GameObject("Arabic");
                arabic.transform.SetParent(variantObject.transform);
                var probe = arabic.AddComponent<LanguageActivationProbe>();
                var objects = variantObject.AddComponent<LanguageGameObjectSwitch>();
                objects.EN = english;
                objects.variants = new[] { new LanguageGameObjectSwitch.Variant { language = SystemLanguage.Arabic, gameObject = arabic } };
                yield return null;
                Assert.That(label.text, Is.EqualTo("Start Game"));
                Assert.That(english.activeSelf, Is.True);
                labelObject.SetActive(false);
                language.CurrentLanguage = SystemLanguage.Arabic;
                Assert.That(label.text, Is.EqualTo("سلام"), "Inactive text must still receive the language event");
                labelObject.SetActive(true);
                yield return null;
                Assert.That(label.DisplayText, Is.EqualTo("\uFEE1\uFEFC\uFEB3"));
                Assert.That(spriteObject.GetComponent<UnityEngine.UI.Image>().sprite, Is.SameAs(arabicSprite));
                Assert.That(english.activeSelf, Is.False);
                Assert.That(arabic.activeInHierarchy, Is.True);
                int enabled = probe.Enables;
                objects.RefreshLanguage();
                Assert.That(probe.Enables, Is.EqualTo(enabled), "Refreshing must not toggle the selected object");
                language.CurrentLanguage = SystemLanguage.French;
                Assert.That(label.text, Is.EqualTo("Start Game"));
                Assert.That(spriteObject.GetComponent<UnityEngine.UI.Image>().sprite, Is.SameAs(englishSprite));
                Assert.That(english.activeSelf, Is.True);
                UnityEngine.Object.Destroy(labelObject);
                UnityEngine.Object.Destroy(spriteObject);
                UnityEngine.Object.Destroy(variantObject);
                yield return null;
                var listeners = (IDictionary)typeof(EventCenter).GetField("eventDic", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(events);
                Assert.That(listeners.Count, Is.Zero, "All three language subscriptions must be disposed");
                language.CurrentLanguage = SystemLanguage.Hebrew;
            }
            finally
            {
                Framework.ClearServices();
                language.Shutdown(); timers.Shutdown(); pools.Shutdown(); scenes.Shutdown(); assets.Shutdown(); save.Shutdown(); events.Shutdown();
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(gameConfig);
                UnityEngine.Object.DestroyImmediate(englishSprite);
                UnityEngine.Object.DestroyImmediate(arabicSprite);
                foreach (string path in new[] { file, file + ".tmp", file + ".bak" }) if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
