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
	public sealed class LockFreeSubscriptionSetTests
	{
		private MockLogger _logger = null!;
		private TestGameEventMetadata<TestArgs> _eventData = null!;

		[SetUp]
		public void SetUp()
		{
			_logger = new MockLogger();
			_eventData = new TestGameEventMetadata<TestArgs>("Nomad.Tests", "LockFreeSubscriptionSetTests");
		}

		[TearDown]
		public void TearDown()
		{
			_logger.Dispose();
			_eventData.Dispose();
		}

		private LockFreeSubscriptionSet<TestArgs> CreateSet(
			EventExceptionPolicy exceptionPolicy = EventExceptionPolicy.ReportAndContinue
		)
		{
			return new LockFreeSubscriptionSet<TestArgs>(_eventData, _logger, exceptionPolicy);
		}

		#region Construction / Initial State

		[Test]
		public void Constructor_InitializesEmptySet()
		{
			using var set = CreateSet();

			using (Assert.EnterMultipleScope()) {
				Assert.That(set.SubscriberCount, Is.Zero);
				Assert.That(set.PublishCount, Is.Zero);
			}
		}

		[Test]
		public void Constructor_NullEventData_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() =>
				new LockFreeSubscriptionSet<TestArgs>(null!, _logger, EventExceptionPolicy.ReportAndContinue));
		}

		[Test]
		public void Constructor_NullLogger_ThrowsArgumentNullException()
		{
			Assert.Throws<ArgumentNullException>(() =>
				new LockFreeSubscriptionSet<TestArgs>(_eventData, null!, EventExceptionPolicy.ReportAndContinue));
		}

		#endregion

		#region Synchronous Subscription Management

		[Test]
		public void AddSubscription_AddsCallbackAndIncrementsSubscriberCount()
		{
			using var set = CreateSet();
			EventCallback<TestArgs> callback = static (in TestArgs _) => { };

			bool added = set.AddSubscription(callback);

			using (Assert.EnterMultipleScope()) {
				Assert.That(added, Is.True);
				Assert.That(set.SubscriberCount, Is.EqualTo(1));
				Assert.That(set.ContainsCallback(callback, out int index), Is.True);
				Assert.That(index, Is.EqualTo(0));
			}
		}

		[Test]
		public void AddSubscription_NullCallback_ThrowsArgumentNullException()
		{
			using var set = CreateSet();

			Assert.Throws<ArgumentNullException>(() => set.AddSubscription(null!));
		}

		[Test]
		public void AddSubscription_DuplicateCallback_ReturnsFalseAndLeavesOriginalSubscription()
		{
			using var set = CreateSet();
			int calls = 0;
			EventCallback<TestArgs> callback = (in TestArgs _) => calls++;

			bool first = set.AddSubscription(callback);
			bool second = set.AddSubscription(callback);

			set.Pump(new TestArgs { Value = 1 });

			using (Assert.EnterMultipleScope()) {
				Assert.That(first, Is.True);
				Assert.That(second, Is.False);
				Assert.That(set.SubscriberCount, Is.EqualTo(1));
				Assert.That(calls, Is.EqualTo(1));
			}
		}

		[Test]
		public void RemoveSubscription_RemovesExistingCallbackAndDecrementsSubscriberCount()
		{
			using var set = CreateSet();
			EventCallback<TestArgs> callback = static (in TestArgs _) => { };

			set.AddSubscription(callback);

			bool removed = set.RemoveSubscription(callback);

			using (Assert.EnterMultipleScope()) {
				Assert.That(removed, Is.True);
				Assert.That(set.SubscriberCount, Is.Zero);
				Assert.That(set.ContainsCallback(callback, out int index), Is.False);
				Assert.That(index, Is.EqualTo(-1));
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

			using (Assert.EnterMultipleScope()) {
				Assert.That(removed, Is.False);
				Assert.That(set.SubscriberCount, Is.Zero);
			}
		}

		[Test]
		public void RemoveSubscription_RemovesMiddleCallbackAndCompactsIndexes()
		{
			using var set = CreateSet();
			var order = new List<int>();

			EventCallback<TestArgs> first = (in TestArgs _) => order.Add(1);
			EventCallback<TestArgs> second = (in TestArgs _) => order.Add(2);
			EventCallback<TestArgs> third = (in TestArgs _) => order.Add(3);

			set.AddSubscription(first);
			set.AddSubscription(second);
			set.AddSubscription(third);

			bool removed = set.RemoveSubscription(second);
			set.Pump(new TestArgs { Value = 10 });

			using (Assert.EnterMultipleScope()) {
				Assert.That(removed, Is.True);
				Assert.That(set.SubscriberCount, Is.EqualTo(2));
				Assert.That(order, Is.EqualTo(new[] { 1, 3 }));
				Assert.That(set.ContainsCallback(first, out int firstIndex), Is.True);
				Assert.That(firstIndex, Is.EqualTo(0));
				Assert.That(set.ContainsCallback(second, out int removedIndex), Is.False);
				Assert.That(removedIndex, Is.EqualTo(-1));
				Assert.That(set.ContainsCallback(third, out int thirdIndex), Is.True);
				Assert.That(thirdIndex, Is.EqualTo(1));
			}
		}

		[Test]
		public void ContainsCallback_NullCallback_ReturnsFalseAndMinusOneIndex()
		{
			using var set = CreateSet();

			bool found = set.ContainsCallback(null!, out int index);

			using (Assert.EnterMultipleScope()) {
				Assert.That(found, Is.False);
				Assert.That(index, Is.EqualTo(-1));
			}
		}

		[Test]
		public void ContainsCallback_MissingCallback_ReturnsFalseAndMinusOneIndex()
		{
			using var set = CreateSet();
			EventCallback<TestArgs> callback = static (in TestArgs _) => { };

			bool found = set.ContainsCallback(callback, out int index);

			using (Assert.EnterMultipleScope()) {
				Assert.That(found, Is.False);
				Assert.That(index, Is.EqualTo(-1));
			}
		}

		#endregion

		#region Synchronous Pumping

		[Test]
		public void Pump_WithNoSubscribers_DoesNotThrowAndIncrementsPublishCount()
		{
			using var set = CreateSet();

			Assert.DoesNotThrow(() => set.Pump(new TestArgs { Value = 5 }));

			Assert.That(set.PublishCount, Is.EqualTo(1));
		}

		[Test]
		public void Pump_InvokesAllSubscribersInInsertionOrder()
		{
			using var set = CreateSet();
			var order = new List<int>();

			set.AddSubscription((in TestArgs _) => order.Add(1));
			set.AddSubscription((in TestArgs _) => order.Add(2));
			set.AddSubscription((in TestArgs _) => order.Add(3));

			set.Pump(new TestArgs { Value = 10 });

			using (Assert.EnterMultipleScope()) {
				Assert.That(order, Is.EqualTo(new[] { 1, 2, 3 }));
				Assert.That(set.PublishCount, Is.EqualTo(1));
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
		public void Pump_AddSubscriptionDuringDispatch_InvokesNewSubscriberInSamePump()
		{
			using var set = CreateSet();
			var calls = new List<string>();

			EventCallback<TestArgs> lateSubscriber = (in TestArgs _) => calls.Add("late");

			EventCallback<TestArgs> first = (in TestArgs _) =>
			{
				calls.Add("first");
				set.AddSubscription(lateSubscriber);
			};

			set.AddSubscription(first);

			set.Pump(new TestArgs { Value = 1 });

			using (Assert.EnterMultipleScope()) {
				Assert.That(calls, Is.EqualTo(new[] { "first", "late" }));
				Assert.That(set.SubscriberCount, Is.EqualTo(2));
			}
		}

		[Test]
		public void Pump_RemoveSubscriptionDuringDispatch_PreventsRemovedSubscriberFromRunning()
		{
			using var set = CreateSet();
			var calls = new List<string>();

			EventCallback<TestArgs> second = (in TestArgs _) => calls.Add("second");

			EventCallback<TestArgs> first = (in TestArgs _) =>
			{
				calls.Add("first");
				set.RemoveSubscription(second);
			};

			set.AddSubscription(first);
			set.AddSubscription(second);

			set.Pump(new TestArgs { Value = 1 });

			using (Assert.EnterMultipleScope()) {
				Assert.That(calls, Is.EqualTo(new[] { "first" }));
				Assert.That(set.SubscriberCount, Is.EqualTo(1));
				Assert.That(set.ContainsCallback(second, out int index), Is.False);
				Assert.That(index, Is.EqualTo(-1));
			}
		}

		[Test]
		public void Pump_RemoveEarlierSubscriptionDuringDispatch_SkipsSubscriberSwappedBehindCurrentIndex()
		{
			using var set = CreateSet();
			var calls = new List<string>();

			EventCallback<TestArgs> first = (in TestArgs _) => calls.Add("first");
			EventCallback<TestArgs> third = (in TestArgs _) => calls.Add("third");

			EventCallback<TestArgs> second = (in TestArgs _) =>
			{
				calls.Add("second");
				set.RemoveSubscription(first);
			};

			set.AddSubscription(first);
			set.AddSubscription(second);
			set.AddSubscription(third);

			set.Pump(new TestArgs { Value = 1 });

			using (Assert.EnterMultipleScope()) {
				Assert.That(calls, Is.EqualTo(new[] { "first", "second" }));
				Assert.That(set.SubscriberCount, Is.EqualTo(2));
				Assert.That(set.ContainsCallback(third, out int thirdIndex), Is.True);
				Assert.That(thirdIndex, Is.EqualTo(0));
			}
		}

		#endregion

		#region Synchronous Exception Policy

		[Test]
		public void Pump_ReportAndContinue_WhenSubscriberThrows_InvokesRemainingSubscribersAndDoesNotThrow()
		{
			using var set = CreateSet(EventExceptionPolicy.ReportAndContinue);
			int afterThrowingSubscriberCalls = 0;

			set.AddSubscription((in TestArgs _) => throw new InvalidOperationException("boom"));
			set.AddSubscription((in TestArgs _) => afterThrowingSubscriberCalls++);

			Assert.DoesNotThrow(() => set.Pump(new TestArgs { Value = 1 }));

			using (Assert.EnterMultipleScope()) {
				Assert.That(afterThrowingSubscriberCalls, Is.EqualTo(1));
				Assert.That(set.PublishCount, Is.EqualTo(1));
			}
		}

		[Test]
		public void Pump_ReportAndContinue_WhenMultipleSubscribersThrow_ReportsAndCompletesPublish()
		{
			using var set = CreateSet(EventExceptionPolicy.ReportAndContinue);
			int successfulCalls = 0;

			set.AddSubscription((in TestArgs _) => throw new InvalidOperationException("first"));
			set.AddSubscription((in TestArgs _) => throw new ApplicationException("second"));
			set.AddSubscription((in TestArgs _) => successfulCalls++);

			Assert.DoesNotThrow(() => set.Pump(new TestArgs { Value = 1 }));

			using (Assert.EnterMultipleScope()) {
				Assert.That(successfulCalls, Is.EqualTo(1));
				Assert.That(set.PublishCount, Is.EqualTo(1));
			}
		}

		[Test]
		public void Pump_Propagate_WhenSubscriberThrows_ThrowsEventHandlerExceptionAndStopsDispatch()
		{
			using var set = CreateSet(EventExceptionPolicy.Propagate);
			int afterThrowingSubscriberCalls = 0;

			set.AddSubscription((in TestArgs _) => throw new InvalidOperationException("boom"));
			set.AddSubscription((in TestArgs _) => afterThrowingSubscriberCalls++);

			Assert.Throws<EventHandlerException>(() => set.Pump(new TestArgs { Value = 1 }));

			using (Assert.EnterMultipleScope()) {
				Assert.That(afterThrowingSubscriberCalls, Is.Zero);
				Assert.That(set.PublishCount, Is.Zero);
			}
		}

		[Test]
		public void Pump_AggregateAfterDispatch_WhenSubscriberThrows_InvokesRemainingSubscribersThenThrowsEventPublishException()
		{
			using var set = CreateSet(EventExceptionPolicy.AggregateAfterDispatch);
			int afterThrowingSubscriberCalls = 0;

			set.AddSubscription((in TestArgs _) => throw new InvalidOperationException("boom"));
			set.AddSubscription((in TestArgs _) => afterThrowingSubscriberCalls++);

			Assert.Throws<EventPublishException>(() => set.Pump(new TestArgs { Value = 1 }));

			using (Assert.EnterMultipleScope()) {
				Assert.That(afterThrowingSubscriberCalls, Is.EqualTo(1));
				Assert.That(set.PublishCount, Is.EqualTo(1));
			}
		}

		[Test]
		public void Pump_AggregateAfterDispatch_WhenMultipleSubscribersThrow_AggregatesAfterAllCallbacks()
		{
			using var set = CreateSet(EventExceptionPolicy.AggregateAfterDispatch);
			var calls = new List<string>();

			set.AddSubscription((in TestArgs _) => throw new InvalidOperationException("first"));
			set.AddSubscription((in TestArgs _) => calls.Add("middle"));
			set.AddSubscription((in TestArgs _) => throw new ApplicationException("second"));

			Assert.Throws<EventPublishException>(() => set.Pump(new TestArgs { Value = 1 }));

			using (Assert.EnterMultipleScope()) {
				Assert.That(calls, Is.EqualTo(new[] { "middle" }));
				Assert.That(set.PublishCount, Is.EqualTo(1));
			}
		}

		#endregion

		#region Unsupported Asynchronous Surface

		[Test]
		public void AddSubscriptionAsync_AlwaysThrowsNotSupportedException()
		{
			using var set = CreateSet();
			AsyncEventCallback<TestArgs> callback = static (_, _) => Task.CompletedTask;

			var ex = Assert.Throws<NotSupportedException>(() => set.AddSubscriptionAsync(callback));

			Assert.That(ex!.Message, Does.Contain("does not support async subscriptions"));
		}

		[Test]
		public void AddSubscriptionAsync_NullCallback_ThrowsNotSupportedExceptionBeforeNullValidation()
		{
			using var set = CreateSet();

			Assert.Throws<NotSupportedException>(() => set.AddSubscriptionAsync(null!));
		}

		[Test]
		public void RemoveSubscriptionAsync_AlwaysThrowsNotSupportedException()
		{
			using var set = CreateSet();
			AsyncEventCallback<TestArgs> callback = static (_, _) => Task.CompletedTask;

			var ex = Assert.Throws<NotSupportedException>(() => set.RemoveSubscriptionAsync(callback));

			Assert.That(ex!.Message, Does.Contain("does not support async subscriptions"));
		}

		[Test]
		public void ContainsCallbackAsync_AlwaysThrowsNotSupportedException()
		{
			using var set = CreateSet();
			AsyncEventCallback<TestArgs> callback = static (_, _) => Task.CompletedTask;

			var ex = Assert.Throws<NotSupportedException>(() => set.ContainsCallbackAsync(callback, out _));

			Assert.That(ex!.Message, Does.Contain("does not support async subscriptions"));
		}

		[Test]
		public void PumpAsync_AlwaysThrowsNotSupportedException()
		{
			using var set = CreateSet();

			var ex = Assert.Throws<NotSupportedException>(() =>
				set.PumpAsync(new TestArgs { Value = 1 }, CancellationToken.None));

			Assert.That(ex!.Message, Does.Contain("does not support async pumping"));
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
		public void Dispose_AfterDisposal_AsyncMembersStillThrowNotSupportedException()
		{
			var set = CreateSet();
			set.Dispose();

			AsyncEventCallback<TestArgs> callback = static (_, _) => Task.CompletedTask;

			using (Assert.EnterMultipleScope()) {
				Assert.Throws<NotSupportedException>(() => set.AddSubscriptionAsync(callback));
				Assert.Throws<NotSupportedException>(() => set.RemoveSubscriptionAsync(callback));
				Assert.Throws<NotSupportedException>(() => set.ContainsCallbackAsync(callback, out _));
				Assert.Throws<NotSupportedException>(() => set.PumpAsync(new TestArgs { Value = 1 }, CancellationToken.None));
			}
		}

		#endregion

		#region Single-Threaded Stress

		[Test]
		public void AddRemoveAndPump_RepeatedSingleThreadedOperationsMaintainCountsAndIndexes()
		{
			using var set = CreateSet();
			const int subscriptionCount = 64;
			int calls = 0;

			EventCallback<TestArgs>[] callbacks = Enumerable.Range(0, subscriptionCount)
				.Select(_ => CreateDistinctCallback(value => calls += value))
				.ToArray();

			foreach (EventCallback<TestArgs> callback in callbacks) {
				Assert.That(set.AddSubscription(callback), Is.True);
			}

			for (int i = 0; i < callbacks.Length; i += 2) {
				Assert.That(set.RemoveSubscription(callbacks[i]), Is.True);
			}

			set.Pump(new TestArgs { Value = 1 });

			using (Assert.EnterMultipleScope()) {
				Assert.That(set.SubscriberCount, Is.EqualTo(subscriptionCount / 2));
				Assert.That(calls, Is.EqualTo(subscriptionCount / 2));
				Assert.That(set.PublishCount, Is.EqualTo(1));
			}

			for (int i = 1; i < callbacks.Length; i += 2) {
				using (Assert.EnterMultipleScope()) {
					Assert.That(set.ContainsCallback(callbacks[i], out int index), Is.True);
					Assert.That(index, Is.GreaterThanOrEqualTo(0));
				}
			}

			for (int i = 0; i < callbacks.Length; i += 2) {
				using (Assert.EnterMultipleScope()) {
					Assert.That(set.ContainsCallback(callbacks[i], out int index), Is.False);
					Assert.That(index, Is.EqualTo(-1));
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
