using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Nomad.Core.Events;
using Nomad.Core.Exceptions;
using Nomad.Events.Globals;
using Nomad.Core.Util;
using Nomad.Events.Private;
using NUnit.Framework.Internal;

namespace Nomad.Events.Tests
{
    [TestFixture]
    [Category("Nomad.Events")]
    [Category("Registry")]
    [Category("Integration")]
    public class GameEventRegistryWrapperTests
    {
        private Mock<IGameEventRegistryService> _mockService;

        [SetUp]
        public void SetUp()
        {
            _mockService = new Mock<IGameEventRegistryService>();
            Globals.GameEventRegistry.Initialize(_mockService.Object);
        }

        [TearDown]
        public void TearDown()
        {
            // Reset the static instance to null for isolation
            var field = typeof(GameEventRegistry).GetField("_instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            field?.SetValue(null, null);
        }

        #region Initialization Tests

        [Test]
        public void Initialize_WithNullInstance_ThrowsArgumentNullException()
        {
            Assert.That(() => Globals.GameEventRegistry.Initialize(null), Throws.ArgumentNullException);
        }

        [Test]
        public void WhenNotInitialized_AnyStaticMethod_ThrowsSubsystemNotInitializedException()
        {
            // Reset instance to null
            var field = typeof(Globals.GameEventRegistry).GetField("_instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            field?.SetValue(null, null);

            // Use a representative method
            Assert.Throws<SubsystemNotInitializedException>(() => Globals.GameEventRegistry.GetAllEvents());
        }

        #endregion

        #region GetEvent Tests

        [Test]
        public void GetEvent_ForwardsArguments_ReturnsInstanceResult()
        {
            // Arrange
            var name = "TestEvent";
            var ns = "TestNamespace";
            var flags = EventFlags.Default;
            var expectedEvent = Mock.Of<IGameEvent<TestEventArgs>>();

            _mockService
                .Setup(s => s.GetEvent<TestEventArgs>(It.IsAny<string>(), It.IsAny<string>(), flags))
                .Returns(expectedEvent);

            // Act
            var result = Globals.GameEventRegistry.GetEvent<TestEventArgs>(name, ns, flags);

            // Assert
            Assert.That(result, Is.SameAs(expectedEvent));
            _mockService.Verify(s => s.GetEvent<TestEventArgs>(name, ns, flags), Times.Once);
        }

        // Example struct for event args
        public struct TestEventArgs { }

        #endregion

        #region TryGetEvent Tests

        [Test]
        public void TryGetEvent_WhenEventExists_ReturnsTrueAndOutEvent()
        {
            // Arrange
            var name = "TestEvent";
            var ns = "TestNamespace";
			var logger = new MockLogger();
            var expectedEvent = Mock.Of<IGameEvent<TestEventArgs>>();

            _mockService
                .Setup(s => s.TryGetEvent(It.IsAny<string>(), It.IsAny<string>(), out expectedEvent))
                .Returns(true);

            // Act
            var success = Globals.GameEventRegistry.TryGetEvent<TestEventArgs>(name, ns, out var result);

			using (Assert.EnterMultipleScope())
			{
				// Assert
				Assert.That(success, Is.True);
				Assert.That(result, Is.SameAs(expectedEvent));
			}
			_mockService.Verify(s => s.TryGetEvent(name, ns, out expectedEvent), Times.Once);
        }

        [Test]
        public void TryGetEvent_WhenEventDoesNotExist_ReturnsFalseAndNull()
        {
            // Arrange
            var name = "TestEvent";
            var ns = "TestNamespace";
            IGameEvent<TestEventArgs>? nullEvent = null;

            _mockService
                .Setup(s => s.TryGetEvent(name, ns, out nullEvent))
                .Returns(false);

            // Act
            var success = Globals.GameEventRegistry.TryGetEvent<TestEventArgs>(name, ns, out var result);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(result, Is.Null);
            _mockService.Verify(s => s.TryGetEvent(name, ns, out nullEvent), Times.Once);
        }

        #endregion

        #region TryRemoveEvent Tests

        [Test]
        public void TryRemoveEvent_ReturnsInstanceResult()
        {
            // Arrange
            var ns = "TestNamespace";
            var name = "TestEvent";

            _mockService
                .Setup(s => s.TryRemoveEvent<TestEventArgs>(ns, name))
                .Returns(true);

            // Act
            var result = Globals.GameEventRegistry.TryRemoveEvent<TestEventArgs>(ns, name);

            // Assert
            Assert.That(result, Is.True);
            _mockService.Verify(s => s.TryRemoveEvent<TestEventArgs>(ns, name), Times.Once);
        }

        [Test]
        public void TryRemoveEvent_WhenEventNotFound_ReturnsFalse()
        {
            // Arrange
            var ns = "TestNamespace";
            var name = "TestEvent";

            _mockService
                .Setup(s => s.TryRemoveEvent<TestEventArgs>(ns, name))
                .Returns(false);

            // Act
            var result = Globals.GameEventRegistry.TryRemoveEvent<TestEventArgs>(ns, name);

            // Assert
            Assert.That(result, Is.False);
            _mockService.Verify(s => s.TryRemoveEvent<TestEventArgs>(ns, name), Times.Once);
        }

        #endregion

        #region ClearEventsInNamespace Tests

        [Test]
        public void ClearEventsInNamespace_ForwardsArgument()
        {
            // Arrange
            var ns = "TestNamespace";

            // Act
            Globals.GameEventRegistry.ClearEventsInNamespace(ns);

            // Assert
            _mockService.Verify(s => s.ClearEventsInNamespace(ns), Times.Once);
        }

        #endregion

        #region ClearAllEvents Tests

        [Test]
        public void ClearAllEvents_CallsInstanceMethod()
        {
            // Act
            Globals.GameEventRegistry.ClearAllEvents();

            // Assert
            _mockService.Verify(s => s.ClearAllEvents(), Times.Once);
        }

        #endregion

        #region GetAllEvents Tests

        [Test]
        public void GetAllEvents_ReturnsInstanceResult()
        {
            // Arrange
            var expectedEvents = new List<IGameEvent> { Mock.Of<IGameEvent>(), Mock.Of<IGameEvent>() };
            _mockService.Setup(s => s.GetAllEvents()).Returns(expectedEvents);

            // Act
            var result = Globals.GameEventRegistry.GetAllEvents();

            // Assert
            Assert.That(result, Is.SameAs(expectedEvents));
            _mockService.Verify(s => s.GetAllEvents(), Times.Once);
        }

        #endregion
    }
}
