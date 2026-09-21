using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Feather.Tests
{
    public sealed class PoolMgrTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private PoolMgr pools;

        private GameObject Create(string name)
        {
            var obj = new GameObject(name);
            objects.Add(obj);
            return obj;
        }

        [SetUp]
        public void SetUp()
        {
            var root = Create("PoolTests");
            pools = new PoolMgr(new ResMgr(root.AddComponent<UIMgr>()), root.transform);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in objects)
                if (obj != null) Object.DestroyImmediate(obj);
            objects.Clear();
        }

        private GameObject Clone(GameObject template)
        {
            GameObject result = null;
            pools.GetCloneObj(template, instance => result = instance);
            if (!objects.Contains(result)) objects.Add(result);
            return result;
        }

        [Test]
        public void DestroyedPooledObjects_AreSkippedAndReplacedWhenEmpty()
        {
            var template = Create("Item");
            var live = Clone(template);
            var dead = Clone(template);
            pools.PushObj(live);
            pools.PushObj(dead);
            Object.DestroyImmediate(dead);
            Assert.That(Clone(template), Is.SameAs(live));
            pools.PushObj(live);
            Object.DestroyImmediate(live);
            Assert.That(Clone(template) != null, Is.True);
        }

        [Test]
        public void SameNamedTemplates_DoNotShareInstances()
        {
            var first = Create("Item");
            first.AddComponent<BoxCollider>();
            var second = Create("Item");
            second.AddComponent<SphereCollider>();
            var firstClone = Clone(first);
            pools.PushObj(firstClone);
            var secondClone = Clone(second);
            Assert.That(secondClone, Is.Not.SameAs(firstClone));
            Assert.That(secondClone.GetComponent<SphereCollider>(), Is.Not.Null);
            Assert.That(secondClone.GetComponent<BoxCollider>(), Is.Null);
            Assert.That(Clone(first), Is.SameAs(firstClone));
        }

        [Test]
        public void RenamingTemplateAndInstance_DoesNotChangeTheirPool()
        {
            var template = Create("Item");
            var clone = Clone(template);
            string key = clone.GetComponent<PoolToken>().Key;
            template.name = "OtherTemplateName";
            clone.name = "OtherInstanceName";
            pools.PushObj(clone);
            Assert.That(Clone(template), Is.SameAs(clone));
            Assert.That(clone.GetComponent<PoolToken>().Key, Is.EqualTo(key));
        }

        [Test]
        public void CloneOfSpawnedInstance_UsesItsOriginalPool()
        {
            var template = Create("Item");
            var first = Clone(template);
            var second = Clone(first);
            pools.PushObj(second);
            Assert.That(Clone(template), Is.SameAs(second));
        }
    }
}
