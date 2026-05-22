/*
===========================================================================
The Nomad Framework
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;
using NUnit.Framework;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Events.Private;
using Nomad.Events.Private.SubscriptionSets;

namespace Nomad.Events.Tests
{
    /// <summary>
    /// Tests for EventFlags and the event implementation selected by each mode.
    /// </summary>
    [TestFixture]
    [Category("Nomad.Events")]
    [Category("Configuration")]
    [Category("Unit")]
    public class EventFlagsTests
    {
        private ILoggerService _logger;
        private IGameEventRegistryService _registry;

        [SetUp]
        public void Setup()
        {
            _logger = new MockLogger();
            _registry = new GameEventRegistry(_logger);
        }

        [TearDown]
        public void TearDown()
        {
            _registry?.Dispose();
            _logger?.Dispose();
        }

        [TestCase(EventFlags.Synchronous, 1u)]
        [TestCase(EventFlags.Asynchronous, 2u)]
        [TestCase(EventFlags.NoLock, 4u)]
        [TestCase(EventFlags.AtomicSafe, 8u)]
        [TestCase(EventFlags.Default, 3u)]
        public void EventFlags_ValuesMatchCurrentApi(EventFlags flag, uint expected)
        {
            Assert.That((uint)flag, Is.EqualTo(expected));
        }

        [Test]
        public void EventFlags_Default_EnablesSyncAndAsyncOnly()
        {
            EventFlags flags = EventFlags.Default;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(flags.HasFlag(EventFlags.Synchronous), Is.True);
                Assert.That(flags.HasFlag(EventFlags.Asynchronous), Is.True);
                Assert.That(flags.HasFlag(EventFlags.NoLock), Is.False);
                Assert.That(flags.HasFlag(EventFlags.AtomicSafe), Is.False);
            }
        }

        [Test]
        public void EventFlags_CanCombineIndependentModes()
        {
            EventFlags flags = EventFlags.Synchronous | EventFlags.AtomicSafe;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(flags.HasFlag(EventFlags.Synchronous), Is.True);
                Assert.That(flags.HasFlag(EventFlags.AtomicSafe), Is.True);
                Assert.That(flags.HasFlag(EventFlags.Asynchronous), Is.False);
                Assert.That(flags.HasFlag(EventFlags.NoLock), Is.False);
            }
        }

        [Test]
        public void GetEvent_Default_UsesStandardSubscriptionSet()
        {
            GameEvent<EmptyEventArgs> gameEvent = GetConcreteEvent(EventFlags.Default);

            Assert.That(gameEvent.SubscriptionSet, Is.TypeOf<SubscriptionSet<EmptyEventArgs>>());
        }

        [Test]
        public void GetEvent_NoLock_UsesLockFreeSubscriptionSet()
        {
            GameEvent<EmptyEventArgs> gameEvent = GetConcreteEvent(EventFlags.NoLock);

            Assert.That(gameEvent.SubscriptionSet, Is.TypeOf<LockFreeSubscriptionSet<EmptyEventArgs>>());
        }

        [Test]
        public void GetEvent_AtomicSafe_UsesAtomicSubscriptionSet()
        {
            GameEvent<EmptyEventArgs> gameEvent = GetConcreteEvent(EventFlags.AtomicSafe);

            Assert.That(gameEvent.SubscriptionSet, Is.TypeOf<AtomicSubscriptionSet<EmptyEventArgs>>());
        }

        [Test]
        public void GetEvent_NoLockWithAsynchronous_ThrowsNotSupported()
        {
            Assert.Throws<NotSupportedException>(() =>
                _registry.GetEvent<EmptyEventArgs>(
                    name: "InvalidFlags",
                    nameSpace: "EventFlagsTests",
                    flags: EventFlags.NoLock | EventFlags.Asynchronous));
        }

        [Test]
        public void NoLockEvent_ForbidsAsyncOperations()
        {
            IGameEvent<EmptyEventArgs> gameEvent = _registry.GetEvent<EmptyEventArgs>(
                "NoLockAsync",
                "EventFlagsTests",
                EventFlags.NoLock);

            Assert.Throws<NotSupportedException>(() =>
                gameEvent.SubscribeAsync((args, ct) => System.Threading.Tasks.Task.CompletedTask));
            Assert.ThrowsAsync<NotSupportedException>(async () =>
                await gameEvent.PublishAsync(EmptyEventArgs.Args));
        }

        private GameEvent<EmptyEventArgs> GetConcreteEvent(EventFlags flags)
        {
            IGameEvent<EmptyEventArgs> gameEvent = _registry.GetEvent<EmptyEventArgs>(
                name: $"{nameof(EventFlagsTests)}_{flags}",
                nameSpace: "EventFlagsTests",
                flags: flags);

            Assert.That(gameEvent, Is.InstanceOf<GameEvent<EmptyEventArgs>>());
            return (GameEvent<EmptyEventArgs>)gameEvent;
        }
    }
}
