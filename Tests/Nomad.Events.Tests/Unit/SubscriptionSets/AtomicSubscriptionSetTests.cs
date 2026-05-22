using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nomad.Core.Events;
using Nomad.Events.Private.SubscriptionSets;

namespace Nomad.Events.Tests
{
    [TestFixture]
	[Category("Nomad.Events")]
	[Category("Unit")]
    public sealed class AtomicSubscriptionSetTests
    {
        private MockLogger _logger = null!;
        private TestGameEventMetadata<TestArgs> _eventData = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = new MockLogger();
            _eventData = new TestGameEventMetadata<TestArgs>("Nomad.Tests", "AtomicSubscriptionSetTests");
        }

        [TearDown]
        public void TearDown()
        {
            _logger.Dispose();
			_eventData.Dispose();
        }

        private AtomicSubscriptionSet<TestArgs> CreateSet(
            EventExceptionPolicy exceptionPolicy = EventExceptionPolicy.ReportAndContinue
        )
        {
            return new AtomicSubscriptionSet<TestArgs>(_eventData, _logger, exceptionPolicy);
        }

        #region Construction / Initial State

        [Test]
        public void Constructor_InitializesEmptySet()
        {
            using var set = CreateSet();

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( set.SubscriberCount, Is.Zero );
				Assert.That( set.PublishCount, Is.Zero );
			}
		}

        [Test]
        public void Constructor_NullEventData_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AtomicSubscriptionSet<TestArgs>(null!, _logger, EventExceptionPolicy.ReportAndContinue));
        }

        [Test]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new AtomicSubscriptionSet<TestArgs>(_eventData, null!, EventExceptionPolicy.ReportAndContinue));
        }

        #endregion

        #region Synchronous Subscription Management

        [Test]
        public void AddSubscription_AddsCallbackAndIncrementsSubscriberCount()
        {
            using var set = CreateSet();
            EventCallback<TestArgs> callback = static (in TestArgs _) => { };

            bool added = set.AddSubscription(callback);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( added, Is.True );
				Assert.That( set.SubscriberCount, Is.EqualTo( 1 ) );
				Assert.That( set.ContainsCallback( callback, out int index ), Is.True );
				Assert.That( index, Is.EqualTo( 0 ) );
			}
		}

        [Test]
        public void AddSubscription_NullCallback_ThrowsArgumentNullException()
        {
            using var set = CreateSet();

            Assert.Throws<ArgumentNullException>(() => set.AddSubscription(null!));
        }

        [Test]
        public void AddSubscription_DuplicateCallback_ReturnsFalseAndDoesNotIncrementSubscriberCount()
        {
            using var set = CreateSet();
            EventCallback<TestArgs> callback = static (in TestArgs _) => { };

            bool first = set.AddSubscription(callback);
            bool second = set.AddSubscription(callback);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( first, Is.True );
				Assert.That( second, Is.False );
				Assert.That( set.SubscriberCount, Is.EqualTo( 1 ) );
			}
		}

        [Test]
        public void RemoveSubscription_RemovesExistingCallbackAndDecrementsSubscriberCount()
        {
            using var set = CreateSet();
            EventCallback<TestArgs> callback = static (in TestArgs _) => { };

            set.AddSubscription(callback);

            bool removed = set.RemoveSubscription(callback);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( removed, Is.True );
				Assert.That( set.SubscriberCount, Is.Zero );
				Assert.That( set.ContainsCallback( callback, out int index ), Is.False );
				Assert.That( index, Is.EqualTo( -1 ) );
			}
		}

        [Test]
        public void RemoveSubscription_NullCallback_ThrowsArgumentNullException()
        {
            using var set = CreateSet();

            Assert.Throws<ArgumentNullException>(() => set.RemoveSubscription(null!));
        }

        [Test]
        public void RemoveSubscription_MissingCallback_ReturnsFalseAndLeavesCountUnchanged()
        {
            using var set = CreateSet();
            EventCallback<TestArgs> callback = static (in TestArgs _) => { };

            bool removed = set.RemoveSubscription(callback);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( removed, Is.False );
				Assert.That( set.SubscriberCount, Is.Zero );
			}
		}

        [Test]
        public void RemoveSubscription_RemovesMiddleCallbackAndPreservesRemainingOrder()
        {
            using var set = CreateSet();
            var order = new List<int>();

            EventCallback<TestArgs> first = (in args) => order.Add(1);
            EventCallback<TestArgs> second = (in args) => order.Add(2);
            EventCallback<TestArgs> third = (in args) => order.Add(3);

            set.AddSubscription(first);
            set.AddSubscription(second);
            set.AddSubscription(third);

            bool removed = set.RemoveSubscription(second);

            set.Pump(new TestArgs { Value = 1 });

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( removed, Is.True );
				Assert.That( set.SubscriberCount, Is.EqualTo( 2 ) );
				Assert.That( order, Is.EqualTo( new[] { 1, 3 } ) );

				Assert.That( set.ContainsCallback( first, out int firstIndex ), Is.True );
				Assert.That( firstIndex, Is.EqualTo( 0 ) );

				Assert.That( set.ContainsCallback( third, out int thirdIndex ), Is.True );
				Assert.That( thirdIndex, Is.EqualTo( 1 ) );

				Assert.That( set.ContainsCallback( second, out int removedIndex ), Is.False );
				Assert.That( removedIndex, Is.EqualTo( -1 ) );
			}
		}

        [Test]
        public void ContainsCallback_NullCallback_ReturnsFalse()
        {
            using var set = CreateSet();

            bool found = set.ContainsCallback(null!, out int index);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( found, Is.False );
				Assert.That( index, Is.EqualTo( -1 ) );
			}
		}

        [Test]
        public void ContainsCallback_MissingCallback_ReturnsFalseAndMinusOneIndex()
        {
            using var set = CreateSet();
            EventCallback<TestArgs> callback = static (in TestArgs _) => { };

            bool found = set.ContainsCallback(callback, out int index);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( found, Is.False );
				Assert.That( index, Is.EqualTo( -1 ) );
			}
		}

        #endregion

        #region Synchronous Pumping

        [Test]
        public void Pump_WithNoSubscribers_DoesNotThrowAndIncrementsPublishCount()
        {
            using var set = CreateSet();
            var args = new TestArgs { Value = 5 };

            Assert.DoesNotThrow(() => set.Pump(in args));

            Assert.That(set.PublishCount, Is.EqualTo(1));
        }

        [Test]
        public void Pump_InvokesAllSynchronousSubscribersInInsertionOrder()
        {
            using var set = CreateSet();
            var order = new List<int>();

            set.AddSubscription((in args) => order.Add(1));
            set.AddSubscription((in args) => order.Add(2));
            set.AddSubscription((in args) => order.Add(3));

            set.Pump(new TestArgs { Value = 10 });

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( order, Is.EqualTo( new[] { 1, 2, 3 } ) );
				Assert.That( set.PublishCount, Is.EqualTo( 1 ) );
			}
		}

        [Test]
        public void Pump_PassesPayloadToEverySubscriber()
        {
            using var set = CreateSet();
            var received = new List<int>();

            set.AddSubscription((in TestArgs args) => received.Add(args.Value));
            set.AddSubscription((in TestArgs args) => received.Add(args.Value * 2));

            set.Pump(new TestArgs { Value = 21 });

            Assert.That(received, Is.EqualTo(new[] { 21, 42 }));
        }

        [Test]
        public void Pump_DoesNotInvokeAsyncSubscribers()
        {
            using var set = CreateSet();
            int asyncCalls = 0;

            set.AddSubscriptionAsync((args, ct) =>
            {
                Interlocked.Increment(ref asyncCalls);
                return Task.CompletedTask;
            });

            set.Pump(new TestArgs { Value = 1 });

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( asyncCalls, Is.Zero );
				Assert.That( set.PublishCount, Is.EqualTo( 1 ) );
			}
		}

        [Test]
        public void Pump_AddSubscriptionDuringDispatch_DoesNotInvokeNewSubscriberUntilNextPump()
        {
            using var set = CreateSet();
            var calls = new List<string>();

            EventCallback<TestArgs> lateSubscriber = (in args) => calls.Add("late");

            EventCallback<TestArgs> first = (in args) =>
            {
                calls.Add("first");
                set.AddSubscription(lateSubscriber);
            };

            set.AddSubscription(first);

            set.Pump(new TestArgs { Value = 1 });

            Assert.That(calls, Is.EqualTo(new[] { "first" }));

            calls.Clear();
            set.Pump(new TestArgs { Value = 1 });

            Assert.That(calls, Is.EqualTo(new[] { "first", "late" }));
        }

        [Test]
        public void Pump_RemoveSubscriptionDuringDispatch_UsesSnapshotForCurrentPumpAndRemovesForNextPump()
        {
            using var set = CreateSet();
            var calls = new List<string>();

            EventCallback<TestArgs> second = (in args) => calls.Add("second");

            EventCallback<TestArgs> first = (in args) =>
            {
                calls.Add("first");
                set.RemoveSubscription(second);
            };

            set.AddSubscription(first);
            set.AddSubscription(second);

            set.Pump(new TestArgs { Value = 1 });

            Assert.That(calls, Is.EqualTo(new[] { "first", "second" }));

            calls.Clear();
            set.Pump(new TestArgs { Value = 1 });

            Assert.That(calls, Is.EqualTo(new[] { "first" }));
        }

        #endregion

        #region Synchronous Exception Policy

        [Test]
        public void Pump_ReportAndContinue_WhenSubscriberThrows_InvokesRemainingSubscribersAndDoesNotThrow()
        {
            using var set = CreateSet(EventExceptionPolicy.ReportAndContinue);
            int afterThrowingSubscriberCalls = 0;

            set.AddSubscription((in args) => throw new InvalidOperationException("boom"));
            set.AddSubscription((in args) => afterThrowingSubscriberCalls++);

            Assert.DoesNotThrow(() => set.Pump(new TestArgs { Value = 1 }));

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( afterThrowingSubscriberCalls, Is.EqualTo( 1 ) );
				Assert.That( set.PublishCount, Is.EqualTo( 1 ) );
			}
		}

        [Test]
        public void Pump_Propagate_WhenSubscriberThrows_ThrowsEventHandlerExceptionAndStopsDispatch()
        {
            using var set = CreateSet(EventExceptionPolicy.Propagate);
            int afterThrowingSubscriberCalls = 0;

            set.AddSubscription((in args) => throw new InvalidOperationException("boom"));
            set.AddSubscription((in args) => afterThrowingSubscriberCalls++);

            Assert.Throws<EventHandlerException>(() => set.Pump(new TestArgs { Value = 1 }));

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( afterThrowingSubscriberCalls, Is.Zero );
				Assert.That( set.PublishCount, Is.Zero );
			}
		}

        [Test]
        public void Pump_AggregateAfterDispatch_WhenSubscriberThrows_InvokesRemainingSubscribersThenThrowsEventPublishException()
        {
            using var set = CreateSet(EventExceptionPolicy.AggregateAfterDispatch);
            int afterThrowingSubscriberCalls = 0;

            set.AddSubscription((in args) => throw new InvalidOperationException("boom"));
            set.AddSubscription((in args) => afterThrowingSubscriberCalls++);

            Assert.Throws<EventPublishException>(() => set.Pump(new TestArgs { Value = 1 }));

            Assert.That(afterThrowingSubscriberCalls, Is.EqualTo(1));
        }

        #endregion

        #region Asynchronous Subscription Management

        [Test]
        public void AddSubscriptionAsync_AddsCallbackAndIncrementsSubscriberCount()
        {
            using var set = CreateSet();
            AsyncEventCallback<TestArgs> callback = static (_, _) => Task.CompletedTask;

            bool added = set.AddSubscriptionAsync(callback);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( added, Is.True );
				Assert.That( set.SubscriberCount, Is.EqualTo( 1 ) );
				Assert.That( set.ContainsCallbackAsync( callback, out int index ), Is.True );
				Assert.That( index, Is.EqualTo( 0 ) );
			}
		}

        [Test]
        public void AddSubscriptionAsync_NullCallback_ThrowsArgumentNullException()
        {
            using var set = CreateSet();

            Assert.Throws<ArgumentNullException>(() => set.AddSubscriptionAsync(null!));
        }

        [Test]
        public void AddSubscriptionAsync_DuplicateCallback_ReturnsFalseAndDoesNotIncrementSubscriberCount()
        {
            using var set = CreateSet();
            AsyncEventCallback<TestArgs> callback = static (_, _) => Task.CompletedTask;

            bool first = set.AddSubscriptionAsync(callback);
            bool second = set.AddSubscriptionAsync(callback);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( first, Is.True );
				Assert.That( second, Is.False );
				Assert.That( set.SubscriberCount, Is.EqualTo( 1 ) );
			}
		}

        [Test]
        public void RemoveSubscriptionAsync_RemovesExistingCallbackAndDecrementsSubscriberCount()
        {
            using var set = CreateSet();
            AsyncEventCallback<TestArgs> callback = static (_, _) => Task.CompletedTask;

            set.AddSubscriptionAsync(callback);

            bool removed = set.RemoveSubscriptionAsync(callback);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( removed, Is.True );
				Assert.That( set.SubscriberCount, Is.Zero );
				Assert.That( set.ContainsCallbackAsync( callback, out int index ), Is.False );
				Assert.That( index, Is.EqualTo( -1 ) );
			}
		}

        [Test]
        public void RemoveSubscriptionAsync_NullCallback_ThrowsArgumentNullException()
        {
            using var set = CreateSet();

            Assert.Throws<ArgumentNullException>(() => set.RemoveSubscriptionAsync(null!));
        }

        [Test]
        public void RemoveSubscriptionAsync_MissingCallback_ReturnsFalseAndLeavesCountUnchanged()
        {
            using var set = CreateSet();
            AsyncEventCallback<TestArgs> callback = static (_, _) => Task.CompletedTask;

            bool removed = set.RemoveSubscriptionAsync(callback);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( removed, Is.False );
				Assert.That( set.SubscriberCount, Is.Zero );
			}
		}

        [Test]
        public void ContainsCallbackAsync_NullCallback_ReturnsFalse()
        {
            using var set = CreateSet();

            bool found = set.ContainsCallbackAsync(null!, out int index);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( found, Is.False );
				Assert.That( index, Is.EqualTo( -1 ) );
			}
		}

        [Test]
        public void ContainsCallbackAsync_MissingCallback_ReturnsFalseAndMinusOneIndex()
        {
            using var set = CreateSet();
            AsyncEventCallback<TestArgs> callback = static (_, _) => Task.CompletedTask;

            bool found = set.ContainsCallbackAsync(callback, out int index);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( found, Is.False );
				Assert.That( index, Is.EqualTo( -1 ) );
			}
		}

        #endregion

        #region Asynchronous Pumping

        [Test]
        public async Task PumpAsync_WithNoSubscribers_DoesNotThrowAndIncrementsPublishCount()
        {
            using var set = CreateSet();

            await set.PumpAsync(new TestArgs { Value = 1 }, CancellationToken.None);

            Assert.That(set.PublishCount, Is.EqualTo(1));
        }

        [Test]
        public async Task PumpAsync_InvokesAllAsyncSubscribers()
        {
            using var set = CreateSet();
            int callCount = 0;

            set.AddSubscriptionAsync((args, ct) =>
            {
                Interlocked.Add(ref callCount, args.Value);
                return Task.CompletedTask;
            });

            set.AddSubscriptionAsync((args, ct) =>
            {
                Interlocked.Add(ref callCount, args.Value * 2);
                return Task.CompletedTask;
            });

            await set.PumpAsync(new TestArgs { Value = 3 }, CancellationToken.None);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( callCount, Is.EqualTo( 9 ) );
				Assert.That( set.PublishCount, Is.EqualTo( 1 ) );
			}
		}

        [Test]
        public async Task PumpAsync_DoesNotInvokeSynchronousSubscribers()
        {
            using var set = CreateSet();
            int syncCalls = 0;

            set.AddSubscription((in TestArgs _) => syncCalls++);

            await set.PumpAsync(new TestArgs { Value = 1 }, CancellationToken.None);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( syncCalls, Is.Zero );
				Assert.That( set.PublishCount, Is.EqualTo( 1 ) );
			}
		}

        [Test]
        public void PumpAsync_WhenTokenAlreadyCanceled_ThrowsOperationCanceledExceptionAndDoesNotInvokeSubscribers()
        {
            using var set = CreateSet();
            using var cts = new CancellationTokenSource();
            int calls = 0;

            set.AddSubscriptionAsync((args, ct) =>
            {
                Interlocked.Increment(ref calls);
                return Task.CompletedTask;
            });

            cts.Cancel();

            Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await set.PumpAsync(new TestArgs { Value = 1 }, cts.Token));

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( calls, Is.Zero );
				Assert.That( set.PublishCount, Is.Zero );
			}
		}

        [Test]
        public async Task PumpAsync_AfterRemovingAsyncSubscriber_DoesNotInvokeRemovedSubscriber()
        {
            using var set = CreateSet();
            int firstCalls = 0;
            int secondCalls = 0;

            AsyncEventCallback<TestArgs> first = (args, ct) =>
            {
                Interlocked.Increment(ref firstCalls);
                return Task.CompletedTask;
            };

            AsyncEventCallback<TestArgs> second = (args, ct) =>
            {
                Interlocked.Increment(ref secondCalls);
                return Task.CompletedTask;
            };

            set.AddSubscriptionAsync(first);
            set.AddSubscriptionAsync(second);

            bool removed = set.RemoveSubscriptionAsync(second);

            await set.PumpAsync(new TestArgs { Value = 1 }, CancellationToken.None);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( removed, Is.True );
				Assert.That( firstCalls, Is.EqualTo( 1 ) );
				Assert.That( secondCalls, Is.Zero );
				Assert.That( set.SubscriberCount, Is.EqualTo( 1 ) );
			}
		}

        [Test]
        public async Task PumpAsync_AddAsyncSubscriptionDuringDispatch_DoesNotInvokeNewSubscriberUntilNextPump()
        {
            using var set = CreateSet();
            var calls = new List<string>();

            AsyncEventCallback<TestArgs> lateSubscriber = (args, ct) =>
            {
                calls.Add("late");
                return Task.CompletedTask;
            };

            AsyncEventCallback<TestArgs> first = (args, ct) =>
            {
                calls.Add("first");
                set.AddSubscriptionAsync(lateSubscriber);
                return Task.CompletedTask;
            };

            set.AddSubscriptionAsync(first);

            await set.PumpAsync(new TestArgs { Value = 1 }, CancellationToken.None);

            Assert.That(calls, Is.EqualTo(new[] { "first" }));

            calls.Clear();
            await set.PumpAsync(new TestArgs { Value = 1 }, CancellationToken.None);

            Assert.That(calls, Is.EqualTo(new[] { "first", "late" }));
        }

        [Test]
        public async Task PumpAsync_AwaitsAllSubscriberTasksBeforeReturning()
        {
            using var set = CreateSet();
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int completedCalls = 0;

            set.AddSubscriptionAsync(async (args, ct) =>
            {
                await tcs.Task.ConfigureAwait(false);
                Interlocked.Increment(ref completedCalls);
            });

            Task publishTask = set.PumpAsync(new TestArgs { Value = 1 }, CancellationToken.None);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( publishTask.IsCompleted, Is.False );
				Assert.That( completedCalls, Is.Zero );
			}

			tcs.SetResult();

            await publishTask;

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( completedCalls, Is.EqualTo( 1 ) );
				Assert.That( set.PublishCount, Is.EqualTo( 1 ) );
			}
		}

        #endregion

        #region Asynchronous Exception Policy

        [Test]
        public async Task PumpAsync_ReportAndContinue_WhenSubscriberFaults_DoesNotThrowAndInvokesOtherSubscribers()
        {
            using var set = CreateSet(EventExceptionPolicy.ReportAndContinue);
            int successfulCalls = 0;

            set.AddSubscriptionAsync((args, ct) => Task.FromException(new InvalidOperationException("boom")));

            set.AddSubscriptionAsync((args, ct) =>
            {
                Interlocked.Increment(ref successfulCalls);
                return Task.CompletedTask;
            });

            await set.PumpAsync(new TestArgs { Value = 1 }, CancellationToken.None);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( successfulCalls, Is.EqualTo( 1 ) );
				Assert.That( set.PublishCount, Is.EqualTo( 1 ) );
			}
		}

        [Test]
        public void PumpAsync_AggregateAfterDispatch_WhenSubscriberFaults_ThrowsEventPublishExceptionAfterAwaitingAll()
        {
            using var set = CreateSet(EventExceptionPolicy.AggregateAfterDispatch);
            int successfulCalls = 0;

            set.AddSubscriptionAsync((args, ct) => Task.FromException(new InvalidOperationException("boom")));

            set.AddSubscriptionAsync((args, ct) =>
            {
                Interlocked.Increment(ref successfulCalls);
                return Task.CompletedTask;
            });

            Assert.ThrowsAsync<EventPublishException>(async () =>
                await set.PumpAsync(new TestArgs { Value = 1 }, CancellationToken.None));

            Assert.That(successfulCalls, Is.EqualTo(1));
        }

        [Test]
        public void PumpAsync_Propagate_WhenSubscriberFaults_ThrowsEventPublishException()
        {
            using var set = CreateSet(EventExceptionPolicy.Propagate);

            set.AddSubscriptionAsync((args, ct) => Task.FromException(new InvalidOperationException("boom")));

            Assert.ThrowsAsync<EventPublishException>(async () =>
                await set.PumpAsync(new TestArgs { Value = 1 }, CancellationToken.None));
        }

        #endregion

        #region Mixed Sync / Async Count Semantics

        [Test]
        public void SubscriberCount_IncludesBothSyncAndAsyncSubscriptions()
        {
            using var set = CreateSet();

            EventCallback<TestArgs> sync = static (in TestArgs _) => { };
            AsyncEventCallback<TestArgs> async = static (_, _) => Task.CompletedTask;

            set.AddSubscription(sync);
            set.AddSubscriptionAsync(async);

            Assert.That(set.SubscriberCount, Is.EqualTo(2));

            set.RemoveSubscription(sync);
            Assert.That(set.SubscriberCount, Is.EqualTo(1));

            set.RemoveSubscriptionAsync(async);
            Assert.That(set.SubscriberCount, Is.Zero);
        }

        [Test]
        public async Task RemovingSyncSubscriber_DoesNotCorruptAsyncTaskCache()
        {
            using var set = CreateSet();

            int asyncCalls = 0;

            EventCallback<TestArgs> sync = static (in TestArgs _) => { };
            AsyncEventCallback<TestArgs> async = (args, ct) =>
            {
                Interlocked.Increment(ref asyncCalls);
                return Task.CompletedTask;
            };

            set.AddSubscription(sync);
            set.AddSubscriptionAsync(async);

            bool removed = set.RemoveSubscription(sync);

            await set.PumpAsync(new TestArgs { Value = 1 }, CancellationToken.None);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( removed, Is.True );
				Assert.That( asyncCalls, Is.EqualTo( 1 ) );
				Assert.That( set.SubscriberCount, Is.EqualTo( 1 ) );
			}
		}

        [Test]
        public async Task RemovingAsyncSubscriber_DoesNotLeaveNullOrStaleTaskCacheEntries()
        {
            using var set = CreateSet();

            int calls = 0;

            AsyncEventCallback<TestArgs> first = (args, ct) =>
            {
                Interlocked.Increment(ref calls);
                return Task.CompletedTask;
            };

            AsyncEventCallback<TestArgs> second = (args, ct) =>
            {
                Interlocked.Increment(ref calls);
                return Task.CompletedTask;
            };

            set.AddSubscriptionAsync(first);
            set.AddSubscriptionAsync(second);

            bool removed = set.RemoveSubscriptionAsync(second);

            Assert.That(removed, Is.True);

			await set.PumpAsync( new TestArgs { Value = 1 }, CancellationToken.None );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( calls, Is.EqualTo( 1 ) );
				Assert.That( set.SubscriberCount, Is.EqualTo( 1 ) );
			}
		}

        #endregion

        #region Disposal

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            using var set = CreateSet();

            Assert.DoesNotThrow(() => set.Dispose());
            Assert.DoesNotThrow(() => set.Dispose());
        }

        [Test]
        public void Dispose_AfterDisposal_AddSubscriptionThrowsObjectDisposedException()
        {
            var set = CreateSet();
            set.Dispose();

            EventCallback<TestArgs> callback = static (in TestArgs _) => { };

            Assert.Throws<ObjectDisposedException>(() => set.AddSubscription(callback));
        }

        [Test]
        public void Dispose_AfterDisposal_RemoveSubscriptionThrowsObjectDisposedException()
        {
            var set = CreateSet();
            set.Dispose();

            EventCallback<TestArgs> callback = static (in TestArgs _) => { };

            Assert.Throws<ObjectDisposedException>(() => set.RemoveSubscription(callback));
        }

        [Test]
        public void Dispose_AfterDisposal_ContainsCallbackThrowsObjectDisposedException()
        {
            var set = CreateSet();
            set.Dispose();

            EventCallback<TestArgs> callback = static (in TestArgs _) => { };

            Assert.Throws<ObjectDisposedException>(() => set.ContainsCallback(callback, out _));
        }

        [Test]
        public void Dispose_AfterDisposal_PumpThrowsObjectDisposedException()
        {
            var set = CreateSet();
            set.Dispose();

            Assert.Throws<ObjectDisposedException>(() => set.Pump(new TestArgs { Value = 1 }));
        }

        [Test]
        public void Dispose_AfterDisposal_PumpAsyncThrowsObjectDisposedException()
        {
            var set = CreateSet();
            set.Dispose();

            Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await set.PumpAsync(new TestArgs { Value = 1 }, CancellationToken.None));
        }

        #endregion

        #region Concurrent Atomic Behavior

        [Test]
        public async Task ConcurrentAdd_SameCallback_OnlyOneSubscriptionIsAdded()
        {
            using var set = CreateSet();

            const int taskCount = 64;
            var startGate = new ManualResetEventSlim(false);
            var results = new bool[taskCount];

            EventCallback<TestArgs> callback = static (in TestArgs _) => { };

            Task[] tasks = Enumerable.Range(0, taskCount)
                .Select(i => Task.Run(() =>
                {
                    startGate.Wait();
                    results[i] = set.AddSubscription(callback);
                }))
                .ToArray();

            startGate.Set();

            await Task.WhenAll(tasks);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( results.Count( static result => result ), Is.EqualTo( 1 ) );
				Assert.That( set.SubscriberCount, Is.EqualTo( 1 ) );
				Assert.That( set.ContainsCallback( callback, out int index ), Is.True );
				Assert.That( index, Is.Zero );
			}
		}

        [Test]
        public async Task ConcurrentAdd_DistinctCallbacks_AllSubscriptionsAreAdded()
        {
            using var set = CreateSet();

            const int subscriberCount = 128;
            var startGate = new ManualResetEventSlim(false);
            int calls = 0;

            EventCallback<TestArgs>[] callbacks = Enumerable.Range(0, subscriberCount)
                .Select(_ => CreateDistinctCallback(value => Interlocked.Add(ref calls, value)))
                .ToArray();

            Task[] tasks = callbacks
                .Select(callback => Task.Run(() =>
                {
                    startGate.Wait();
                    Assert.That(set.AddSubscription(callback), Is.True);
                }))
                .ToArray();

            startGate.Set();

            await Task.WhenAll(tasks);

            set.Pump(new TestArgs { Value = 1 });

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( set.SubscriberCount, Is.EqualTo( subscriberCount ) );
				Assert.That( calls, Is.EqualTo( subscriberCount ) );
			}
		}

        [Test]
        public async Task ConcurrentPump_ReadOnlyDispatch_IsSafeAndInvokesExpectedNumberOfCallbacks()
        {
            using var set = CreateSet();

            const int subscriberCount = 8;
            const int publisherCount = 8;
            const int publishesPerPublisher = 1_000;

            int calls = 0;

            for (int i = 0; i < subscriberCount; i++)
            {
                Assert.That(
                    set.AddSubscription(CreateDistinctCallback(value => Interlocked.Add(ref calls, value))),
                    Is.True
                );
            }

            Task[] publishers = Enumerable.Range(0, publisherCount)
                .Select(_ => Task.Run(() =>
                {
                    for (int i = 0; i < publishesPerPublisher; i++)
                    {
                        set.Pump(new TestArgs { Value = 1 });
                    }
                }))
                .ToArray();

            await Task.WhenAll(publishers);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( calls, Is.EqualTo( subscriberCount * publisherCount * publishesPerPublisher ) );
				Assert.That( set.PublishCount, Is.EqualTo( publisherCount * publishesPerPublisher ) );
			}
		}

        [Test]
        public async Task ConcurrentRemove_DistinctCallbacks_AllSubscriptionsAreRemovedExactlyOnce()
        {
            using var set = CreateSet();

            const int subscriberCount = 128;
            var startGate = new ManualResetEventSlim(false);

            EventCallback<TestArgs>[] callbacks = Enumerable.Range(0, subscriberCount)
                .Select(_ => CreateDistinctCallback(static _ => { }))
                .ToArray();

            foreach (var callback in callbacks)
            {
                Assert.That(set.AddSubscription(callback), Is.True);
            }

            Task[] tasks = callbacks
                .Select(callback => Task.Run(() =>
                {
                    startGate.Wait();
                    Assert.That(set.RemoveSubscription(callback), Is.True);
                }))
                .ToArray();

            startGate.Set();

            await Task.WhenAll(tasks);

            Assert.That(set.SubscriberCount, Is.Zero);

            foreach (var callback in callbacks)
            {
				using ( Assert.EnterMultipleScope() ) {
					Assert.That( set.ContainsCallback( callback, out int index ), Is.False );
					Assert.That( index, Is.EqualTo( -1 ) );
				}
			}
        }

        #endregion

        private static EventCallback<TestArgs> CreateDistinctCallback(Action<int> onValue)
        {
            return new TestCallbackTarget(onValue).AddValue;
        }

        private sealed class TestCallbackTarget
        {
            private readonly Action<int> _onValue;

            public TestCallbackTarget(Action<int> onValue)
            {
                _onValue = onValue;
            }

            public void AddValue(in TestArgs args)
            {
                _onValue(args.Value);
            }
        }

        private readonly struct TestArgs
        {
            public int Value { get; init; }
        }
	}
}
