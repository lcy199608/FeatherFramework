using System;
using NUnit.Framework;
using UnityEngine;

namespace Feather.Tests
{
    public sealed class SceneMgrTests
    {
        [Test]
        public void InvalidScene_FaultsAndDoesNotBlockNextRequest()
        {
            var scenes = new SceneMgr(null, new EventCenter());
            var result = scenes.SwitchAsync(-1);
            Assert.That(result.IsFaulted, Is.True);
            Assert.That(result.Exception.InnerException, Is.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(scenes.IsLoading, Is.False);
            scenes.Shutdown();
            Assert.That(scenes.SwitchAsync(0).Exception.InnerException, Is.TypeOf<ObjectDisposedException>());
        }
    }
}
