using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Feather.Tests
{
    public sealed class LocalizationTests
    {
        private string file;
        private SaveDataMgr save;
        private GameConfig gameConfig;
        private ConfigMgr config;
        private EventCenter events;

        [SetUp]
        public void SetUp()
        {
            file = Path.Combine(Path.GetTempPath(), "FeatherLocalization-" + Guid.NewGuid().ToString("N") + ".es3");
            save = new SaveDataMgr();
            save.Initialize(new ES3Settings(false) { path = file, location = ES3.Location.File, encryptionType = ES3.EncryptionType.None });
            gameConfig = ScriptableObject.CreateInstance<GameConfig>();
            gameConfig.followSystemLanguage = false;
            gameConfig.defaultLanguage = SystemLanguage.English;
            config = new ConfigMgr();
            config.InitConfig();
            events = new EventCenter();
        }

        [TearDown]
        public void TearDown()
        {
            save.Shutdown();
            UnityEngine.Object.DestroyImmediate(gameConfig);
            if (File.Exists(file)) File.Delete(file);
            if (File.Exists(file + ".tmp")) File.Delete(file + ".tmp");
        }

        [TestCase(1, SystemLanguage.ChineseSimplified)]
        [TestCase(2, SystemLanguage.ChineseTraditional)]
        [TestCase(3, SystemLanguage.English)]
        [TestCase(4, SystemLanguage.Japanese)]
        [TestCase(5, SystemLanguage.Korean)]
        public void LegacySavedNumbersMigrateWithoutEnumCollisions(int old, SystemLanguage expected)
        {
            save.SetSystemData("LanguageSaveData", old);
            var languages = new LanguageMgr(save, config, events, gameConfig);
            Assert.That(languages.CurrentLanguage, Is.EqualTo(expected));
            var record = save.GetSystemData<LanguageMgr.Preference>(LanguageMgr.PreferenceSaveKey, null);
            Assert.That(record.Version, Is.EqualTo(1));
            Assert.That(record.Language, Is.EqualTo(expected.ToString()));
            languages.CurrentLanguage = SystemLanguage.Arabic;
            Assert.That(new LanguageMgr(save, config, events, gameConfig).CurrentLanguage, Is.EqualTo(SystemLanguage.Arabic));
            Assert.That(save.GetSystemData("LanguageSaveData", -1), Is.EqualTo(old));
        }

        [TestCase(0, true, SystemLanguage.English)]
        [TestCase(1, false, SystemLanguage.ChineseSimplified)]
        [TestCase(2, false, SystemLanguage.ChineseTraditional)]
        [TestCase(3, false, SystemLanguage.English)]
        [TestCase(4, false, SystemLanguage.Japanese)]
        [TestCase(5, false, SystemLanguage.Korean)]
        public void LegacyInspectorLanguageMigrates(int old, bool follow, SystemLanguage expected)
        {
            // EditorJsonUtility.FromJsonOverwrite does not run this asset's deserialization callback.
            // Import a real old-format asset, using this project's existing GameConfig script GUID.
            string path = "Assets/__LocalizationMigration_" + Guid.NewGuid().ToString("N") + ".asset";
            string scriptPath = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromScriptableObject(gameConfig));
            string guid = UnityEditor.AssetDatabase.AssetPathToGUID(scriptPath);
            try
            {
                File.WriteAllText(path, "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n" +
                    "  m_ObjectHideFlags: 0\n  m_Script: {fileID: 11500000, guid: " + guid + ", type: 3}\n  m_Name: OldLanguage\n  language: " + old + "\n");
                UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceSynchronousImport);
                var migrated = UnityEditor.AssetDatabase.LoadAssetAtPath<GameConfig>(path);
                Assert.That(migrated.followSystemLanguage, Is.EqualTo(follow));
                Assert.That(migrated.defaultLanguage, Is.EqualTo(expected));
                migrated.defaultLanguage = SystemLanguage.Arabic;
                migrated.OnAfterDeserialize();
                Assert.That(migrated.defaultLanguage, Is.EqualTo(SystemLanguage.Arabic), "Migration must not run twice");
            }
            finally { UnityEditor.AssetDatabase.DeleteAsset(path); }
        }

        [Test]
        public void EveryUnityLanguageHasAStronglyTypedTableColumn()
        {
            var row = new cfg.LanguageInfo();
            var languages = Enum.GetValues(typeof(SystemLanguage)).Cast<SystemLanguage>().Distinct();
            foreach (var language in languages)
            {
                var normalized = LanguageMgr.Normalize(language);
                // Use the canonical Hungarian spelling, not Unity's obsolete Hugarian alias.
                string name = normalized == SystemLanguage.Hungarian ? "Hungarian" : normalized.ToString();
                var property = typeof(cfg.LanguageInfo).GetProperty(name);
                Assert.That(property, Is.Not.Null, name);
                property.SetValue(row, name);
                Assert.That(LanguageTableAccess.Get(row, language), Is.EqualTo(name), name);
                property.SetValue(row, null);
            }
        }

        [Test]
        public void MissingTranslationReturnsEnglishAndItsDirectionThenId()
        {
            var language = new LanguageMgr(save, config, events, gameConfig);
            language.CurrentLanguage = SystemLanguage.Arabic;
            var row = config.Tables.Language.DataMap[1];
            row.Arabic = "";
            row.English = "Start Game";
            Assert.That(language.GetLanguageById(1, out var resolved), Is.EqualTo("Start Game"));
            Assert.That(resolved, Is.EqualTo(SystemLanguage.English));
            row.English = "";
            Assert.That(language.GetLanguageById(1), Is.EqualTo("1"));
            row.Arabic = "مرحبا";
            Assert.That(language.GetLanguageById(1, out resolved), Is.EqualTo("مرحبا"));
            Assert.That(resolved, Is.EqualTo(SystemLanguage.Arabic));
        }

        [Test]
        public void LanguageEventsOnlyPublishWhenResolvedLanguageChanges()
        {
            var language = new LanguageMgr(save, config, events, gameConfig);
            int calls = 0;
            using (events.Subscribe(FrameworkEvents.LanguageChangedId, () => calls++))
            {
                language.CurrentLanguage = SystemLanguage.Arabic;
                language.CurrentLanguage = SystemLanguage.Arabic;
                var unused = language.CurrentLanguage;
                Assert.That(calls, Is.EqualTo(1));
                language.CurrentLanguage = SystemLanguage.Unknown;
                Assert.That(language.CurrentLanguage, Is.EqualTo(SystemLanguage.English));
                Assert.Throws<ArgumentOutOfRangeException>(() => language.CurrentLanguage = (SystemLanguage)999);
                language.Shutdown();
                Assert.Throws<ObjectDisposedException>(() => language.GetLanguageById(1));
            }
        }

        [Test]
        public void FollowSystemChoiceIsPersistedSeparately()
        {
            var language = new LanguageMgr(save, config, events, gameConfig);
            language.FollowSystemLanguage();
            Assert.That(language.FollowsSystemLanguage, Is.True);
            Assert.That(new LanguageMgr(save, config, events, gameConfig).FollowsSystemLanguage, Is.True);
            language.CurrentLanguage = language.CurrentLanguage;
            Assert.That(language.FollowsSystemLanguage, Is.False);
        }
    }
}
