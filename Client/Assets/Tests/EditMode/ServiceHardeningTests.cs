using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Feather.Tests
{
    public sealed class ServiceHardeningTests
    {
        [Test]
        public void ClosedAssetsAndPoolsRejectNewWorkButAllowCleanup()
        {
            var root = new GameObject("ServiceTest");
            var template = new GameObject("Template");
            try
            {
                var assets = new ResMgr(root.AddComponent<UIMgr>());
                var scope = assets.CreateScope();
                var pools = new PoolMgr(assets, root.transform);
                GameObject instance = null;
                pools.GetCloneObj(template, result => instance = result);
                pools.Shutdown();
                pools.PushObj(instance);
                Assert.That(instance == null, Is.True);
                Assert.Throws<ObjectDisposedException>(() => pools.GetCloneObj(template, _ => { }));
                assets.Shutdown();
                Assert.Throws<ObjectDisposedException>(() => assets.Load<GameObject>("anything"));
                Assert.Throws<ObjectDisposedException>(() => scope.LoadAsync<GameObject>("anything", _ => { }));
                scope.Dispose();
                assets.Shutdown();
            }
            finally { UnityEngine.Object.DestroyImmediate(template); UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void BinaryReadersRejectInvalidLengths()
        {
            foreach (int length in new[] { -1, int.MaxValue, 5 })
            {
                using (var stream = new MemoryStream(BitConverter.GetBytes(length)))
                using (var reader = new BinaryReader(stream))
                    Assert.Throws<InvalidDataException>(() => cfg.Tables.ReadString(reader));
                using (var stream = new MemoryStream(BitConverter.GetBytes(length)))
                using (var reader = new BinaryReader(stream))
                    Assert.Throws<InvalidDataException>(() => cfg.Tables.ReadArray(reader, value => value.ReadInt32()));
            }
        }

        [Test]
        public void RedDotTotalsSaturateAndChildrenAreReadOnly()
        {
            var root = new RedDotNode("Root", "Root", null);
            var one = new RedDotNode("Root/One", "One", root);
            var two = new RedDotNode("Root/Two", "Two", root);
            root.Children.Add("One", one);
            root.Children.Add("Two", two);
            one.SetRedDotNum(int.MaxValue);
            two.SetRedDotNum(int.MaxValue);
            Assert.That(root.redDotNum, Is.EqualTo(int.MaxValue));
            Assert.Throws<NotSupportedException>(() => ((System.Collections.Generic.IDictionary<string, RedDotNode>)root.dicChildren).Clear());
        }
    }
}
