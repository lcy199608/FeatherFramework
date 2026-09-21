using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Feather.Tests
{
    public sealed class EventCenterTests
    {
        [Test]
        public void OldSubscriptionCannotRemoveNewRegistrationAfterClear()
        {
            var events = new EventCenter();
            var id = new EventId("clear");
            int calls = 0;
            UnityEngine.Events.UnityAction action = () => calls++;
            var old = events.Subscribe(id, action);
            events.Clear();
            using (events.Subscribe(id, action))
            {
                old.Dispose();
                events.Publish(id);
                Assert.That(calls, Is.EqualTo(1));
            }
            events.Shutdown();
            Assert.Throws<ObjectDisposedException>(() => events.Subscribe(id, action));
        }

        [Test]
        public void Publish_InvokesMatchingPayloadOnly()
        {
            var events = new EventCenter();
            var number = 0;
            var text = string.Empty;
            var numberChanged = new EventId<int>("changed");
            var textChanged = new EventId<string>("changed");

            events.Subscribe(numberChanged, value => number = value);
            events.Subscribe(textChanged, value => text = value);

            events.Publish(numberChanged, 42);

            Assert.That(number, Is.EqualTo(42));
            Assert.That(text, Is.Empty);
        }

        [Test]
        public void Dispose_RemovesOnlyItsSubscription()
        {
            var events = new EventCenter();
            var firstCalls = 0;
            var secondCalls = 0;
            var changed = new EventId("changed");
            IDisposable first = events.Subscribe(changed, () => firstCalls++);
            events.Subscribe(changed, () => secondCalls++);

            first.Dispose();
            events.Publish(changed);

            Assert.That(firstCalls, Is.Zero);
            Assert.That(secondCalls, Is.EqualTo(1));
        }

        [Test]
        public void Subscribe_RejectsInvalidInputs()
        {
            var events = new EventCenter();

            Assert.Throws<ArgumentException>(() => new EventId(" "));
            Assert.Throws<ArgumentNullException>(() => events.Subscribe(new EventId("changed"), null));
        }

        [Test]
        public void Publish_ContinuesAfterListenerThrows()
        {
            var events = new EventCenter();
            var calls = 0;
            var changed = new EventId("changed");
            events.Subscribe(changed, () =>
            {
                calls += 1;
                throw new InvalidOperationException("listener failure");
            });
            events.Subscribe(changed, () => calls += 1);

            LogAssert.Expect(LogType.Exception, new Regex("listener failure"));
            Assert.DoesNotThrow(() => events.Publish(changed));
            Assert.That(calls, Is.EqualTo(2));
        }

        [Test]
        public void TypedEventId_UsesTheExpectedPayloadType()
        {
            var events = new EventCenter();
            var changed = 0;
            var eventId = new EventId<int>("changed");

            events.Subscribe(eventId, value => changed = value);
            events.Publish(eventId, 7);

            Assert.That(changed, Is.EqualTo(7));
        }
    }
}
