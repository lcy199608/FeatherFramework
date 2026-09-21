using NUnit.Framework;
using UnityEngine;

namespace Feather.Tests
{
    public sealed class OptionalModulesTests
    {
        [Test]
        public void DisabledTables_AreEmptyAndOtherTablesStillLoad()
        {
            var config = new ConfigMgr();
            config.InitConfig(false, false);
            Assert.That(config.Tables.Language.DataList, Is.Empty);
            Assert.That(config.Tables.RedDot.DataList, Is.Empty);
            Assert.That(config.Tables.Item, Is.Not.Null);
            config.InitConfig();
            Assert.That(config.Tables.Language.DataList, Is.Not.Empty);
            Assert.That(config.Tables.RedDot.DataList, Is.Not.Empty);
        }

        [Test]
        public void NewGameConfig_PreservesEnabledDefaults()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            try
            {
                Assert.That(config.enableLocalization, Is.True);
                Assert.That(config.enableRedDots, Is.True);
            }
            finally { Object.DestroyImmediate(config); }
        }
    }
}
