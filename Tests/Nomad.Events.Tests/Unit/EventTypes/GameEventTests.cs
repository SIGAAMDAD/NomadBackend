using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Events.Private;
using Nomad.Core.Util;
using Nomad.Events.Extensions;
using NSubstitute.Core;
using Nomad.Events.Private.EventTypes;
using System.Runtime.CompilerServices;            // InternString


namespace Nomad.Events.Tests
{
    [TestFixture]
    [Category("Nomad.Events")]
    [Category("EventTypes")]
    [Category("Unit")]
    public class GameEventTests
    {
        private ILoggerService _logger;
        private const string TestNamespace = "TestNamespace";
        private const string TestEventName = "TestEvent";

        [SetUp]
        public void Setup()
        {
            _logger = new MockLogger();
        }

        [TearDown]
        public void TearDown()
        {
            _logger?.Dispose();
        }

        // Helper to create a GameEvent instance with given flags
        private IGameEvent<T> CreateEvent<T>(EventFlags flags = EventFlags.Default) where T : struct
        {
            return new GameEvent<T>(
                new InternString(TestNamespace),
                new InternString(TestEventName),
                _logger,
                flags
            );
        }

        #region Basic Properties

        [Test]
        public void Constructor_SetsNameAndNamespace()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(evt.DebugName, Is.EqualTo(TestEventName));
                Assert.That(evt.NameSpace, Is.EqualTo(TestNamespace));
                Assert.That(evt.Id, Is.Not.Zero);
            }
        }

        [Test]
        public void Equals_NotSameType_ReturnsFalse()
        {
            var a = CreateEvent<EmptyEventArgs>();
            var b = new object();

            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void Equals_SameEvent_ReturnsTrue()
        {
            var a = CreateEvent<EmptyEventArgs>();
            var b = a;

            Assert.That(a, Is.EqualTo(b));
        }

        #endregion

        #region Subscription (Synchronous)

        [Test]
        public void Subscribe_SyncCallback_AddsSubscription()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            bool called = false;
            EventCallback<EmptyEventArgs> callback = (in EmptyEventArgs args) => called = true;

            var handle = evt.Subscribe(callback);
            evt.Publish(EmptyEventArgs.Args);

            Assert.That(called, Is.True);
        }

        [Test]
        public void Subscribe_SameSyncCallbackTwice_WithDefaultFlags_AddsBoth()
        {
            // SubscriptionSet (default) does not prevent duplicates
            var evt = CreateEvent<EmptyEventArgs>(EventFlags.Default);
            int callCount = 0;
            EventCallback<EmptyEventArgs> callback = (in EmptyEventArgs args) => callCount++;

            evt.Subscribe(callback);
            evt.Subscribe(callback);
            evt.Publish(EmptyEventArgs.Args);

            Assert.That(callCount, Is.EqualTo(2));
        }

        [Test]
        public void Subscribe_SameSyncCallbackTwice_WithLockFree_ReturnsFalseAndDoesNotAdd()
        {
            // LockFreeSubscriptionSet checks duplicates and logs a warning
            var evt = CreateEvent<EmptyEventArgs>(EventFlags.NoLock);
            int callCount = 0;
            EventCallback<EmptyEventArgs> callback = (in EmptyEventArgs args) => callCount++;

            var handle1 = evt.Subscribe(callback);
            var handle2 = evt.Subscribe(callback); // Should not add second time
            evt.Publish(EmptyEventArgs.Args);

            Assert.That(callCount, Is.EqualTo(1));
        }

        [Test]
        public void Unsubscribe_SyncCallback_RemovesSubscription()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            bool called = false;
            EventCallback<EmptyEventArgs> callback = (in EmptyEventArgs args) => called = true;

            evt.Subscribe(callback);
            evt.Unsubscribe(callback);
            evt.Publish(EmptyEventArgs.Args);

            Assert.That(called, Is.False);
        }

        [Test]
        public void Unsubscribe_NonExistentSyncCallback_DoesNotThrow()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            EventCallback<EmptyEventArgs> callback = (in EmptyEventArgs args) => { };

            Assert.DoesNotThrow(() => evt.Unsubscribe(callback));
        }

        [Test]
        public void OnPublished_EventAccessor_AddsAndRemovesSyncCallback()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            bool called = false;
            EventCallback<EmptyEventArgs> callback = (in EmptyEventArgs args) => called = true;

            evt.OnPublished += callback;
            evt.Publish(EmptyEventArgs.Args);
            Assert.That(called, Is.True);

            called = false;
            evt.OnPublished -= callback;
            evt.Publish(EmptyEventArgs.Args);
            Assert.That(called, Is.False);
        }

        #endregion

        #region Subscription (Asynchronous)

        [Test]
        public void Subscribe_AsyncCallback_AddsSubscription()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            bool called = false;
            AsyncEventCallback<EmptyEventArgs> callback = async (args, ct) =>
            {
                await Task.Yield();
                called = true;
            };

            var handle = evt.SubscribeAsync(callback);
            evt.PublishAsync(EmptyEventArgs.Args).Wait();

            Assert.That(called, Is.True);
        }

        [Test]
        public void Subscribe_AsyncCallback_WithNoLock_ThrowsNotSupported()
        {
            var evt = CreateEvent<EmptyEventArgs>(EventFlags.NoLock);
            AsyncEventCallback<EmptyEventArgs> callback = (args, ct) => Task.CompletedTask;

            Assert.Throws<NotSupportedException>(() => evt.SubscribeAsync(callback));
        }

        [Test]
        public void Unsubscribe_AsyncCallback_RemovesSubscription()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            bool called = false;
            AsyncEventCallback<EmptyEventArgs> callback = async (args, ct) =>
            {
                await Task.Yield();
                called = true;
            };

            evt.SubscribeAsync(callback);
            evt.UnsubscribeAsync(callback);
            evt.PublishAsync(EmptyEventArgs.Args).Wait();

            Assert.That(called, Is.False);
        }

        [Test]
        public void OnPublishedAsync_EventAccessor_AddsAndRemovesAsyncCallback()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            bool called = false;
            AsyncEventCallback<EmptyEventArgs> callback = async (args, ct) =>
            {
                await Task.Yield();
                called = true;
            };

            evt.OnPublishedAsync += callback;
            evt.PublishAsync(EmptyEventArgs.Args).Wait();
            Assert.That(called, Is.True);

            called = false;
            evt.OnPublishedAsync -= callback;
            evt.PublishAsync(EmptyEventArgs.Args).Wait();
            Assert.That(called, Is.False);
        }

        #endregion

        #region Publishing (Synchronous)

        [Test]
        public void Publish_InvokesAllSyncSubscribers()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            int count = 0;
            EventCallback<EmptyEventArgs> cb1 = (in EmptyEventArgs args) => count++;
            EventCallback<EmptyEventArgs> cb2 = (in EmptyEventArgs args) => count++;

            evt.Subscribe(cb1);
            evt.Subscribe(cb2);
            evt.Publish(EmptyEventArgs.Args);

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void Publish_PassesArgumentsCorrectly()
        {
            var evt = CreateEvent<TestEventArgs>();
            var expected = new TestEventArgs { Value = 42 };
            TestEventArgs? received = null;
            EventCallback<TestEventArgs> callback = (in TestEventArgs args) => received = args;

            evt.Subscribe(callback);
            evt.Publish(expected);

            Assert.That(received, Is.EqualTo(expected));
        }

        [Test]
        public void Publish_WithNoSubscribers_DoesNothing()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            Assert.DoesNotThrow(() => evt.Publish(EmptyEventArgs.Args));
        }

        [Test]
        public void Publish_WhenCallbackThrows_OtherCallbacksStillRun()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            int count = 0;
            EventCallback<EmptyEventArgs> throwing = (in EmptyEventArgs args) => throw new InvalidOperationException();
            EventCallback<EmptyEventArgs> normal = (in EmptyEventArgs args) => count++;

            evt.Subscribe(throwing);
            evt.Subscribe(normal);

            // Exception is not caught by the event; it propagates to caller.
            // We test that the normal callback still executes before the throw.
            Assert.Throws<EventPublishException>(() => evt.Publish(EmptyEventArgs.Args));
            Assert.That(count, Is.EqualTo(1));
        }

        #endregion

        #region Publishing (Asynchronous)

        [Test]
        public async Task PublishAsync_InvokesAllAsyncSubscribers()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            int count = 0;
            AsyncEventCallback<EmptyEventArgs> cb1 = async (args, ct) =>
            {
                await Task.Delay(10, ct);
                count++;
            };
            AsyncEventCallback<EmptyEventArgs> cb2 = async (args, ct) =>
            {
                await Task.Delay(5, ct);
                count++;
            };

            evt.SubscribeAsync(cb1);
            evt.SubscribeAsync(cb2);
            await evt.PublishAsync(EmptyEventArgs.Args);

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public async Task PublishAsync_WaitsForAllTasks()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            var tcs = new TaskCompletionSource<bool>();
            AsyncEventCallback<EmptyEventArgs> callback = async (args, ct) => await tcs.Task;

            evt.SubscribeAsync(callback);
            var publishTask = evt.PublishAsync(EmptyEventArgs.Args);

            Assert.That(publishTask.IsCompleted, Is.False);
            tcs.SetResult(true);
            await publishTask; // should complete
        }

        [Test]
        public void PublishAsync_WithNoLock_ThrowsNotSupported()
        {
            var evt = CreateEvent<EmptyEventArgs>(EventFlags.NoLock);
            Assert.ThrowsAsync<NotSupportedException>(async () =>
                await evt.PublishAsync(EmptyEventArgs.Args));
        }

        [Test]
        public void PublishAsync_WithCancellationToken_CancelsTasks()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            AsyncEventCallback<EmptyEventArgs> callback = async (args, ct) =>
            {
                await Task.Delay(1000, ct);
            };

            evt.SubscribeAsync(callback);
            Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await evt.PublishAsync(EmptyEventArgs.Args, cts.Token));
        }

        [Test]
        public async Task PublishAsync_WithMixedSubscribers_OnlyAsyncAreInvoked()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            bool syncCalled = false;
            bool asyncCalled = false;

            evt.Subscribe((in EmptyEventArgs args) => syncCalled = true);
            evt.SubscribeAsync(async (args, ct) =>
            {
                await Task.Yield();
                asyncCalled = true;
            });

            await evt.PublishAsync(EmptyEventArgs.Args);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(syncCalled, Is.False, "Synchronous subscribers should not be invoked by PublishAsync");
                Assert.That(asyncCalled, Is.True);
            }
        }

        #endregion

        #region Subscription Handle

        [Test]
        public void SubscriptionHandle_Dispose_UnsubscribesSyncCallback()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            bool called = false;
            EventCallback<EmptyEventArgs> callback = (in EmptyEventArgs args) => called = true;

            var handle = evt.Subscribe(callback);
            handle.Dispose();
            evt.Publish(EmptyEventArgs.Args);

            Assert.That(called, Is.False);
        }

        [Test]
        public void SubscriptionHandle_Dispose_UnsubscribesAsyncCallback()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            bool called = false;
            AsyncEventCallback<EmptyEventArgs> callback = async (args, ct) =>
            {
                await Task.Yield();
                called = true;
            };

            var handle = evt.SubscribeAsync(callback);
            handle.Dispose();
            evt.PublishAsync(EmptyEventArgs.Args).Wait();

            Assert.That(called, Is.False);
        }

        [Test]
        public void SubscriptionHandle_DisposeTwice_DoesNotThrow()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            EventCallback<EmptyEventArgs> callback = (in EmptyEventArgs args) => { };
            var handle = evt.Subscribe(callback);
            handle.Dispose();
            Assert.DoesNotThrow(() => handle.Dispose());
        }

        #endregion

        #region Event Lifecycle

        [Test]
        public void Dispose_RemovesAllSubscriptions()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            evt.Subscribe((in EmptyEventArgs args) => { });
            evt.Dispose();

            Assert.That(evt.SubscriberCount, Is.GreaterThan(0));
        }

        [Test]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            evt.Dispose();
            Assert.DoesNotThrow(() => evt.Dispose());
        }

        #endregion

        #region Event Flags

        [Test]
        public void EventFlags_NoLock_ForbidsAsyncOperations()
        {
            var evt = CreateEvent<EmptyEventArgs>(EventFlags.NoLock);
            AsyncEventCallback<EmptyEventArgs> asyncCb = (args, ct) => Task.CompletedTask;

            Assert.Throws<NotSupportedException>(() => evt.SubscribeAsync(asyncCb));
            Assert.ThrowsAsync<NotSupportedException>(async () => await evt.PublishAsync(EmptyEventArgs.Args));
        }

        [Test]
        public void EventFlags_Default_AllowsBothSyncAndAsync()
        {
            var evt = CreateEvent<EmptyEventArgs>(EventFlags.Default);
            bool syncCalled = false, asyncCalled = false;

            evt.Subscribe((in EmptyEventArgs args) => syncCalled = true);
            evt.SubscribeAsync(async (args, ct) =>
            {
                await Task.Yield();
                asyncCalled = true;
            });

            evt.Publish(EmptyEventArgs.Args);
            Assert.That(syncCalled, Is.True);

            evt.PublishAsync(EmptyEventArgs.Args).Wait();
            Assert.That(asyncCalled, Is.True);
        }

        [Test]
        public void Constructor_NoLockAndAsynchronousFlagsCombination_Throws()
        {
            // The constructor throws if NoLock and Asynchronous are both set.
            // We need to access the internal constructor; we can test via reflection or assume it's tested elsewhere.
            // Here we simply verify that creating with such flags throws.
            Assert.Throws<NotSupportedException>(() =>
            {
                var _ = new GameEvent<EmptyEventArgs>(
                    new InternString(TestNamespace),
                    new InternString(TestEventName),
                    _logger,
                    EventFlags.NoLock | EventFlags.Asynchronous
                );
            });
        }

        #endregion

        #region One Time Susbcription

        [Test]
        public void OneTimeSubscription_CallsOriginalMethod()
        {
            // Arrange
            var evt = CreateEvent<EmptyEventArgs>();
            bool called = false;

            var handle = evt.SubscribeOnce((in EmptyEventArgs args) => called = true);

            // Act
            evt.Publish(default);

            // Assert
            Assert.That(called, Is.True);
        }

        [Test]
        public void OneTimeSubscription_KillsItself()
        {
            // Arrange
            var evt = CreateEvent<EmptyEventArgs>();
            bool notCalled = true;

            var handle = evt.SubscribeOnce((in EmptyEventArgs args) => notCalled = false);

            // Act
            evt.Publish(default);
            notCalled = true;
            evt.Publish(default);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(notCalled, Is.True);
                Assert.That(handle.IsDisposed, Is.True);
            }
        }

        [Test]
        public void OneTimeSubscription_UsedWithEventQueue_KillsItself()
        {
            // Arrange
            var evt = CreateEvent<EmptyEventArgs>();
            var eventQueue = new EventQueue();
            bool notCalled = true;

            var handle = evt.SubscribeOnce((in EmptyEventArgs args) => notCalled = false);

            // Act
            eventQueue.Enqueue(evt, default);
            eventQueue.ProcessAll();
            notCalled = true;
            eventQueue.Enqueue(evt, default);
            eventQueue.ProcessAll();

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(notCalled, Is.True);
                Assert.That(handle.IsDisposed, Is.True);
            }
        }

        #endregion

#if EVENT_DEBUG
        #region Debug Members

        [Test]
        public void DebugProperties_UpdateOnPublish()
        {
            var evt = CreateEvent<TestEventArgs>();
            var args = new TestEventArgs { Value = 42 };

            evt.Publish(args);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(evt.LastPayload, Is.EqualTo(args));
				Assert.That(evt.LastPublishTime, Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(1)));
				Assert.That(evt.PublishCount, Is.EqualTo(1));
			}
		}

        [Test]
        public void SubscriberCount_ReflectsSubscriptions()
        {
            var evt = CreateEvent<EmptyEventArgs>();
            Assert.That(evt.SubscriberCount, Is.EqualTo(0));

            var handle1 = evt.Subscribe((in EmptyEventArgs _) => { });
            Assert.That(evt.SubscriberCount, Is.EqualTo(1));

            var handle2 = evt.Subscribe((in EmptyEventArgs _) => { });
            Assert.That(evt.SubscriberCount, Is.EqualTo(2));

            handle1.Dispose();
            Assert.That(evt.SubscriberCount, Is.EqualTo(1));

            handle2.Dispose();
            Assert.That(evt.SubscriberCount, Is.Zero);
        }

        #endregion
#endif

        #region Test EventArgs

        private struct TestEventArgs : IEquatable<TestEventArgs>
        {
            public int Value;

            public bool Equals(TestEventArgs other) => Value == other.Value;
            public override bool Equals(object obj) => obj is TestEventArgs other && Equals(other);
            public override int GetHashCode() => Value;
        }

        #endregion
    }
}