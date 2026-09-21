using NUnit.Framework;
using UnityEngine;

namespace Feather.Tests
{
    public sealed class TimerMgrTests
    {
        private GameObject root;
        private TimerMgr timers;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("TimerTests");
            timers = new TimerMgr(root.AddComponent<UIMgr>());
        }

        [TearDown]
        public void TearDown()
        {
            timers.RemoveAllTimer();
            Object.DestroyImmediate(root);
        }

        [Test]
        public void HandleValidityTracksCompletionAndShutdown()
        {
            var handle = timers.AfterSeconds(0, () => { });
            Assert.That(handle.IsValid, Is.True);
            timers.Tick();
            Assert.That(handle.IsValid, Is.False);
            var pending = timers.AfterSeconds(10, () => { });
            timers.Shutdown();
            Assert.That(pending.IsValid, Is.False);
            Assert.Throws<System.ObjectDisposedException>(() => timers.AfterSeconds(1, () => { }));
        }

        [Test]
        public void DueTimers_CanCancelEachOtherBeforeInvocation()
        {
            TimerHandle first = default, second = default;
            int calls = 0;
            first = timers.AfterSeconds(0, () => { calls++; second.Dispose(); });
            second = timers.AfterSeconds(0, () => { calls++; first.Dispose(); });
            timers.Tick();
            timers.Tick();
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Callback_CanCancelAllRemainingSnapshotEntries()
        {
            int calls = 0;
            for (int i = 0; i < 3; i++)
                timers.AfterSeconds(0, () => { calls++; timers.RemoveAllTimer(); });
            timers.Tick();
            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void Callback_CreatedTimerWaitsForNextTick()
        {
            int calls = 0;
            TimerHandle original = default;
            original = timers.AfterSeconds(0, () =>
            {
                original.Dispose();
                timers.AfterSeconds(0, () => calls++);
            });
            timers.Tick();
            Assert.That(calls, Is.Zero);
            timers.Tick();
            Assert.That(calls, Is.EqualTo(1));
        }
    }
}
