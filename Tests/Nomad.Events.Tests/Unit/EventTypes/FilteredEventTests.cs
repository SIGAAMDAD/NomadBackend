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
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.Util;
using Nomad.Events.Extensions;
using Nomad.Events.Private.EventTypes;
using Nomad.Events.Private;
using NUnit.Framework;
using System.Threading;

namespace Nomad.Events.Tests {
    [TestFixture]
    [Category( "Nomad.Events" )]
    [Category( "EventTypes" )]
    [Category( "Unit" )]
    public class FilteredEventTests {
        private ILoggerService _logger;
        private const string TestNamespace = "TestNamespace";
        private const string TestEventName = "TestEvent";

        [SetUp]
        public void Setup() {
            _logger = new MockLogger();
        }

        [TearDown]
        public void TearDown() {
            _logger?.Dispose();
        }

        // Helper to create a GameEvent instance with given flags
        private IGameEvent<T> CreateEvent<T>( EventFlags flags = EventFlags.Default ) where T : struct {
            // InternString is implicitly convertible from string (if not, use constructor)
            return new GameEvent<T>(
                new InternString( TestNamespace ),
                new InternString( TestEventName ),
                _logger,
                flags
            );
        }

        [Test]
        public void CreateFilteredEvent_IsFilteredEvent() {
            // Arrange
            var evt = CreateEvent<int>().Where( args => args > 10 );

            // Assert
            Assert.That( evt, Is.InstanceOf<FilteredGameEvent<int>>() );
        }

        [Test]
        public void CreateFilteredEvent_HasCorrectData() {
            // Arrange
            var evt = CreateEvent<int>().Where( args => args > 10 );

            // Assert
            Assert.That( evt, Is.InstanceOf<FilteredGameEvent<int>>() );
            using ( Assert.EnterMultipleScope() ) {
                Assert.That( evt.DebugName, Is.EqualTo( TestEventName ) );
                Assert.That( evt.NameSpace, Is.EqualTo( TestNamespace ) );
            }
        }

        [Test]
        public void CreateFilteredEvent_PublishAsync_ThrowsNotSupportedException() {
            // Arrange
            var evt = CreateEvent<int>().Where( args => args > 10 );

            // Assert
            Assert.ThrowsAsync<NotSupportedException>( async () => await evt.PublishAsync( default ) );
        }

        [Test]
        public void CreateFilteredEvent_SubscribeAsync_ThrowsNotSupportedException() {
            // Arrange
            var evt = CreateEvent<int>().Where( args => args > 10 );

            // Assert
            Assert.Throws<NotSupportedException>( () => evt.SubscribeAsync( null ) );
        }

        [Test]
        public void CreateFilteredEvent_UnsubscribeAsync_ThrowsNotSupportedException() {
            // Arrange
            var evt = CreateEvent<int>().Where( args => args > 10 );

            // Assert
            Assert.Throws<NotSupportedException>( () => evt.UnsubscribeAsync( null ) );
        }

        [Test]
        public void CreateFilteredEvent_OnPublishedAdd_AddsSubscription() {
            // Arrange
            var evt = CreateEvent<int>().Where( args => args > 10 );

            // Act
            evt.OnPublished += ( in int args ) => { };

            // Assert
            Assert.That( evt.SubscriberCount, Is.EqualTo( 1 ) );
        }

        [Test]
        public void CreateFilteredEvent_OnPublishedRemoveAfterAdd_RemovesSubscription() {
            // Arrange
            var evt = CreateEvent<int>().Where( args => args > 10 );
            EventCallback<int> callback = ( in int args ) => { };

            // Act & Assert
            evt.OnPublished += callback;
            Assert.That( evt.SubscriberCount, Is.EqualTo( 1 ) );
            evt.OnPublished -= callback;
            Assert.That( evt.SubscriberCount, Is.Zero );
        }

        [Test]
        public void CreateFilteredEvent_OnPublishedAsyncAdd_ThrowsNotSupportedException() {
            // Arrange
            var evt = CreateEvent<int>().Where( args => args > 10 );

            // Act & Assert
            Assert.Throws<NotSupportedException>(() => evt.OnPublishedAsync += async ( int args, CancellationToken ct ) => { });
        }

        [Test]
        public void CreateFilteredEvent_DisposeTwice_DoesNotThrow() {
            // Arrange
            var evt = CreateEvent<int>().Where( args => args > 10 );

            // Assert
            evt.Dispose();
            Assert.DoesNotThrow( () => evt.Dispose() );
        }

        [Test]
        public void PublishFilteredEvent_MultipleTimesVariousArgs_PredicateFiltersCorrectly() {
            // Arrange
            var evt = CreateEvent<int>().Where( args => args > 10 );
            int callCount = 0;
            evt.Subscribe( ( in int args ) => callCount++ );

            // Act
            evt.Publish( 9 );
            evt.Publish( 2 );
            evt.Publish( 14 );

            // Assert
            Assert.That( callCount, Is.EqualTo( 1 ) );
        }

        [Test]
        public void PublishFilteredEvent_PublishCountIsAccurate() {
            // Arrange
            var evt = CreateEvent<int>().Where( args => args > 10 );
            evt.Subscribe( ( in int args ) => {} );

            // Act
            evt.Publish( 9 );
            evt.Publish( 2 );
            evt.Publish( 14 );

            // Assert
            Assert.That( evt.PublishCount, Is.EqualTo( 3 ) );
        }
    }
}