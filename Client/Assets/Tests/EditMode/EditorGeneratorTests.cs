using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Feather.Tests
{
    public sealed class EditorGeneratorTests
    {
        [Test]
        public void UiGeneratorRejectsAmbiguousPathsAndEscapesLiterals()
        {
            var type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType("AutoGenerateUIScript")).First(value => value != null);
            var collect = type.GetMethod("CollectBindings", BindingFlags.Static | BindingFlags.NonPublic);
            var generate = type.GetMethod("CreateBindingScript", BindingFlags.Static | BindingFlags.NonPublic);
            var root = new GameObject("TestPanel");
            try
            {
                var child = new GameObject("TranQuote\"\\");
                child.transform.SetParent(root.transform);
                var bindings = collect.Invoke(null, new object[] { root.transform });
                string text = (string)generate.Invoke(null, new[] { (object)"TestPanel", bindings });
                Assert.That(text, Does.Contain("TranQuote\\\"\\\\"));
                new GameObject(child.name).transform.SetParent(root.transform);
                var error = Assert.Throws<TargetInvocationException>(() => collect.Invoke(null, new object[] { root.transform }));
                Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
                var sanitize = type.GetMethod("SanitizeIdentifier", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(sanitize.Invoke(null, new object[] { "class" }), Is.EqualTo("_class"));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BinaryReaderAcceptsLegacyAndVersionOne(bool versioned)
        {
            byte[] data;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new byte[] { 0x46, 0x43, 0x46, 0x47 });
                if (versioned) writer.Write(-1);
                writer.Write(1); writer.Write(123);
                data = stream.ToArray();
            }
            var method = typeof(cfg.Tables).GetMethod("LoadBinaryRows", BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(typeof(int));
            var rows = (System.Collections.Generic.List<int>)method.Invoke(null, new object[] { data, new Func<BinaryReader, int>(reader => reader.ReadInt32()) });
            CollectionAssert.AreEqual(new[] { 123 }, rows);
        }
    }
}
