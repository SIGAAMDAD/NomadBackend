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

#if !UNITY_EDITOR
using System;
using NUnit.Framework;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.Exceptions;

namespace Nomad.Events.Tests
{
    /// <summary>
    /// Tests for GameEventRegistry functionality
    /// </summary>
    [TestFixture]
    [Category("Nomad.Events")]
    [Category("Registry")]
    [Category("Unit")]
    public class GameEventRegistryTests
    {
        private IGameEventRegistryService _registry;
        private ILoggerService _logger;

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

        #region Event Creation Tests

        [Test]
        public void GetEvent_WithValidParameters_CreatesNewEvent()
        {
            // Arrange
            string nameSpace = "Test.Namespace";
            string eventName = "TestEvent";

            // Act
            var gameEvent = _registry.GetEvent<EmptyEventArgs>(eventName, nameSpace);

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(gameEvent, Is.Not.Null);
                Assert.That(gameEvent.DebugName, Is.EqualTo(eventName));
                Assert.That(gameEvent.NameSpace, Is.EqualTo(nameSpace));
            }
        }

        [Test]
        public void GetEvent_CalledTwiceWithSameParameters_ReturnsSameInstance()
        {
            // Arrange
            string nameSpace = "Test.Namespace";
            string eventName = "TestEvent";

            // Act
            var event1 = _registry.GetEvent<EmptyEventArgs>(eventName, nameSpace);
            var event2 = _registry.GetEvent<EmptyEventArgs>(eventName, nameSpace);

            // Assert
            Assert.That(event1, Is.SameAs(event2));
        }

        [Test]
        public void GetEvent_WithDifferentNamespaces_CreatesDifferentEvents()
        {
            // Arrange
            string eventName = "TestEvent";

            // Act
            var event1 = _registry.GetEvent<EmptyEventArgs>("Namespace1", eventName);
            var event2 = _registry.GetEvent<EmptyEventArgs>("Namespace2", eventName);

            // Assert
            Assert.That(event1, Is.Not.SameAs(event2));
            Assert.That(event1.Id, Is.Not.EqualTo(event2.Id));
        }

        [Test]
        public void GetEvent_WithDifferentEventNames_CreatesDifferentEvents()
        {
            // Arrange
            string nameSpace = "Test.Namespace";

            // Act
            var event1 = _registry.GetEvent<EmptyEventArgs>(nameSpace, "Event1");
            var event2 = _registry.GetEvent<EmptyEventArgs>(nameSpace, "Event2");

            // Assert
            Assert.That(event1, Is.Not.SameAs(event2));
            Assert.That(event1.Id, Is.Not.EqualTo(event2.Id));
        }

        #endregion

        #region Type Mismatch Tests

        [Test]
        public void GetEvent_WithDifferentArgTypeForSameName_DoesNotThrow()
        {
            // Arrange
            string nameSpace = "Test.Namespace";
            string eventName = "TestEvent";
            _registry.GetEvent<EmptyEventArgs>(eventName, nameSpace); // Register with EmptyEventArgs

            // Act & Assert
            Assert.DoesNotThrow(() => _registry.GetEvent<TestEventArgs>(eventName, nameSpace));
        }

        [Test]
        public void GetEvent_WithSameArgTypeForSameName_ReturnsSameEvent()
        {
            // Arrange
            string nameSpace = "Test.Namespace";
            string eventName = "TestEvent";

            // Act
            var event1 = _registry.GetEvent<TestEventArgs>(eventName, nameSpace);
            var event2 = _registry.GetEvent<TestEventArgs>(eventName, nameSpace);

            // Assert
            Assert.That(event1, Is.SameAs(event2));
        }

        [Test]
        public void GetEvents_ReturnsAllCreatedEvents()
        {
            // Arrange
            string nameSpace = "Test.Namespace";
            var event1 = _registry.GetEvent<EmptyEventArgs>("Event1", nameSpace);
            var event2 = _registry.GetEvent<EmptyEventArgs>("Event2", nameSpace);
            var event3 = _registry.GetEvent<EmptyEventArgs>("Event3", nameSpace);

            // Act
            var events = _registry.GetAllEvents();

			using (Assert.EnterMultipleScope())
			{
				// Assert
				Assert.That(events, Does.Contain(event1));
                Assert.That(events, Does.Contain(event2));
                Assert.That(events, Does.Contain(event3));
			}
        }

        #endregion

        #region Event Removal Tests

        [Test]
        public void TryRemoveEvent_WithExistingEvent_RemovesAndReturnsTrue()
        {
            // Arrange
            string nameSpace = "Test.Namespace";
            string eventName = "TestEvent";
            _registry.GetEvent<EmptyEventArgs>(eventName, nameSpace);

            // Act
            bool removed = _registry.TryRemoveEvent<EmptyEventArgs>(eventName, nameSpace);

            // Assert
            Assert.That(removed, Is.True);
        }

        [Test]
        public void TryRemoveEvent_WithNonExistentEvent_ReturnsFalse()
        {
            // Arrange
            string nameSpace = "Test.Namespace";
            string eventName = "NonExistentEvent";

            // Act
            bool removed = _registry.TryRemoveEvent<EmptyEventArgs>(eventName, nameSpace);

            // Assert
            Assert.That(removed, Is.False);
        }

        [Test]
        public void TryRemoveEvent_AfterRemoval_CanCreateNewInstanceWithSameName()
        {
            // Arrange
            string nameSpace = "Test.Namespace";
            string eventName = "TestEvent";
            var event1 = _registry.GetEvent<EmptyEventArgs>(eventName, nameSpace);
            _registry.TryRemoveEvent<EmptyEventArgs>(eventName, nameSpace);

            // Act
            var event2 = _registry.GetEvent<EmptyEventArgs>(eventName, nameSpace);

            // Assert
            Assert.That(event1, Is.Not.SameAs(event2));
        }

        [Test]
        public void ClearAllEvents_RemovesAllEvents()
        {
            // Arrange
            string nameSpace = "Test.Namespace";
            _registry.GetEvent<EmptyEventArgs>("Event1", nameSpace);
            _registry.GetEvent<EmptyEventArgs>("Event2", nameSpace);
            _registry.GetEvent<EmptyEventArgs>("Event3", nameSpace);

            // Act
            _registry.ClearAllEvents();

			using (Assert.EnterMultipleScope())
			{
				// Assert
				Assert.That(_registry.TryGetEvent<EmptyEventArgs>("Event1", nameSpace, out _), Is.False);
				Assert.That(_registry.TryGetEvent<EmptyEventArgs>("Event2", nameSpace, out _), Is.False);
				Assert.That(_registry.TryGetEvent<EmptyEventArgs>("Event3", nameSpace, out _), Is.False);
			}
		}

        #endregion

        #region Namespace Clearing Tests

        [Test]
        public void ClearEventsInNamespace_WithMultipleEventsInNamespace_RemovesAll()
        {
            // Arrange
            string nameSpace = "Test.Namespace";
            _registry.GetEvent<EmptyEventArgs>(nameSpace, "Event1");
            _registry.GetEvent<EmptyEventArgs>(nameSpace, "Event2");
            _registry.GetEvent<EmptyEventArgs>(nameSpace, "Event3");

            // Act
            _registry.ClearEventsInNamespace(nameSpace);

            // Assert
            // Can create new instances with same names, which only works if old ones were removed
            var newEvent1 = _registry.GetEvent<EmptyEventArgs>(nameSpace, "Event1");
            Assert.That(newEvent1, Is.Not.Null);
        }

        [Test]
        public void ClearEventsInNamespace_OnlyClearsSpecificNamespace()
        {
            // Arrange
            string nameSpace1 = "Namespace1";
            string nameSpace2 = "Namespace2";
            var event1 = _registry.GetEvent<EmptyEventArgs>(nameSpace1, "Event1");
            var event2 = _registry.GetEvent<EmptyEventArgs>(nameSpace2, "Event1");

            // Act
            _registry.ClearEventsInNamespace(nameSpace1);
            var event2After = _registry.GetEvent<EmptyEventArgs>(nameSpace2, "Event1");

            // Assert - event from nameSpace2 should still be the same instance
            Assert.That(event2After, Is.SameAs(event2));
        }

        [Test]
        public void ClearEventsInNamespace_WithNonExistentNamespace_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                _registry.ClearEventsInNamespace("NonExistent.Namespace");
            });
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void Dispose_DisposesAllEvents()
        {
            // Arrange
            _registry.GetEvent<EmptyEventArgs>("Namespace1", "Event1");
            _registry.GetEvent<EmptyEventArgs>("Namespace2", "Event2");

            // Act
            _registry.Dispose();

            // After disposal, we can't get events anymore, but we verify no exception is thrown
            Assert.DoesNotThrow(() => _registry.Dispose());
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            _registry.GetEvent<EmptyEventArgs>("Test.Namespace", "Event");

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                _registry.Dispose();
                _registry.Dispose();
                _registry.Dispose();
            });
        }

        #endregion

        #region Event Equality Tests

        [Test]
        public void GameEvent_Equals_ReturnsTrueForSameInstance()
        {
            // Arrange
            var event1 = _registry.GetEvent<EmptyEventArgs>("Test.Namespace", "Event");

            // Act
            bool equals = event1.Equals(event1);

            // Assert
            Assert.That(equals, Is.True);
        }

        [Test]
        public void GameEvent_Equals_ReturnsTrueForEventWithSameId()
        {
            // Arrange
            var event1 = _registry.GetEvent<EmptyEventArgs>("Test.Namespace", "Event");
            var event2 = _registry.GetEvent<EmptyEventArgs>("Test.Namespace", "Event");

            // Act
            bool equals = event1.Equals(event2);

            // Assert
            Assert.That(equals, Is.True);
        }

        [Test]
        public void GameEvent_Equals_ReturnsFalseForDifferentEvents()
        {
            // Arrange
            var event1 = _registry.GetEvent<EmptyEventArgs>("Test.Namespace", "Event1");
            var event2 = _registry.GetEvent<EmptyEventArgs>("Test.Namespace", "Event2");

            // Act
            bool equals = event1.Equals(event2);

            // Assert
            Assert.That(equals, Is.False);
        }

        #endregion

        #region Multiple Registries Tests

        [Test]
        public void MultipleRegistries_MaintainIndependentCaches()
        {
            // Arrange
            var registry1 = new GameEventRegistry(_logger);
            var registry2 = new GameEventRegistry(_logger);

            // Act
            var event1 = registry1.GetEvent<EmptyEventArgs>("Test", "Event");
            var event2 = registry2.GetEvent<EmptyEventArgs>("Test", "Event");

            // Assert
            Assert.That(event1, Is.Not.SameAs(event2));

            // Cleanup
            registry1.Dispose();
            registry2.Dispose();
        }

        #endregion

        #region Edge Cases

        [Test]
        public void GetEvent_WithEmptyStringNamespace_ThrowsArgumentExceptin()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _registry.GetEvent<EmptyEventArgs>("", "Event"));
        }

        [Test]
        public void GetEvent_WithSpecialCharactersInNames_CreatesEvent()
        {
            // Act
            var gameEvent = _registry.GetEvent<EmptyEventArgs>("Event!@#$%^", "Test.Namespace@#$");

            // Assert
            Assert.That(gameEvent, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(gameEvent.NameSpace, Contains.Substring("@#$"));
                Assert.That(gameEvent.DebugName, Contains.Substring("!@#$"));
            }
        }

        #endregion
    }

    /// <summary>
    /// Test event args types
    /// </summary>
    public struct TestEventArgs
    {
        public int Value { get; set; }
        public string Message { get; set; }
    }
}
#endif
