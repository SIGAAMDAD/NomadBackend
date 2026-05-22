using System;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.Util;
using Nomad.Events.Private;
using Nomad.Events.Private.EventTypes;
using NUnit.Framework;

namespace Nomad.Events.Tests
{
	[TestFixture]
	public sealed class DelayedEventTests
	{
		private readonly struct TestPayload : IEquatable<TestPayload>
		{
			public readonly int Value;
			public readonly int OtherValue;

			public TestPayload(int value, int otherValue = 0)
			{
				Value = value;
				OtherValue = otherValue;
			}

			public bool Equals(TestPayload other)
			{
				return Value == other.Value && OtherValue == other.OtherValue;
			}

			public override bool Equals(object? obj)
			{
				return obj is TestPayload other && Equals(other);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(Value, OtherValue);
			}

			public override string ToString()
			{
				return $"{nameof(Value)}={Value}, {nameof(OtherValue)}={OtherValue}";
			}
		}

		private sealed class SynchronizationContextScope : IDisposable
		{
			private readonly SynchronizationContext? _previousContext;

			public SynchronizationContextScope(SynchronizationContext? context)
			{
				_previousContext = SynchronizationContext.Current;
				SynchronizationContext.SetSynchronizationContext(context);
			}

			public void Dispose()
			{
				SynchronizationContext.SetSynchronizationContext(_previousContext);
			}
		}

		private static GameEvent<TestPayload> CreateSource(
			string nameSpace = "Nomad.Tests.Events",
			string name = "DelayedEventTests",
			EventFlags flags = EventFlags.Synchronous)
		{
			ILoggerService logger = new MockLogger();

			return new GameEvent<TestPayload>(
				new InternString(nameSpace),
				new InternString(name),
				logger,
				flags
			);
		}

		[Test]
		public void Constructor_WithGameEventSource_CreatesDelayedEvent()
		{
			var source = CreateSource(
				nameSpace: "Nomad.Tests.Constructor",
				name: "Constructor_WithGameEventSource_CreatesDelayedEvent"
			);

			var delayed = new DelayedEvent<TestPayload>(source, 0);

			Assert.Multiple(() =>
			{
				Assert.That(delayed.DebugName, Is.EqualTo(source.DebugName));
				Assert.That(delayed.NameSpace, Is.EqualTo(source.NameSpace));
				Assert.That(delayed.Id, Is.EqualTo(source.Id));
			});

			delayed.Dispose();
		}

		[Test]
		public void Constructor_WithNullSource_ThrowsInvalidCastException()
		{
			Assert.Throws<InvalidCastException>(
				() => new DelayedEvent<TestPayload>(null!, 0)
			);
		}

		[Test]
		public void Properties_ForwardDebugNameNameSpaceAndIdFromSource()
		{
			var source = CreateSource(
				nameSpace: "Nomad.Tests.Forwarding",
				name: "Properties_ForwardDebugNameNameSpaceAndIdFromSource"
			);

			var delayed = new DelayedEvent<TestPayload>(source, 0);

			Assert.Multiple(() =>
			{
				Assert.That(delayed.DebugName, Is.EqualTo("Properties_ForwardDebugNameNameSpaceAndIdFromSource"));
				Assert.That(delayed.NameSpace, Is.EqualTo("Nomad.Tests.Forwarding"));
				Assert.That(delayed.Id, Is.EqualTo(source.Id));
			});

			delayed.Dispose();
		}

		[Test]
		public void Subscribe_ReturnsSubscriptionHandle()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			EventCallback<TestPayload> callback = static (in TestPayload _) =>
			{
			};

			ISubscriptionHandle handle = delayed.Subscribe(callback);

			Assert.That(handle, Is.Not.Null);

			delayed.Dispose();
		}

		[Test]
		public void Subscribe_ForwardsCallbackToSource()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			var expected = new TestPayload(10, 20);
			var actual = default(TestPayload);
			int callCount = 0;

			EventCallback<TestPayload> callback = (in TestPayload payload) =>
			{
				actual = payload;
				callCount++;
			};

			delayed.Subscribe(callback);
			delayed.Publish(in expected);

			Assert.Multiple(() =>
			{
				Assert.That(callCount, Is.EqualTo(1));
				Assert.That(actual, Is.EqualTo(expected));
			});

			delayed.Dispose();
		}

		[Test]
		public void OnPublished_Add_ForwardsToSubscribe()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			var expected = new TestPayload(123, 456);
			var actual = default(TestPayload);
			int callCount = 0;

			EventCallback<TestPayload> callback = (in TestPayload payload) =>
			{
				actual = payload;
				callCount++;
			};

			delayed.OnPublished += callback;
			delayed.Publish(in expected);

			Assert.Multiple(() =>
			{
				Assert.That(callCount, Is.EqualTo(1));
				Assert.That(actual, Is.EqualTo(expected));
			});

			delayed.Dispose();
		}

		[Test]
		public void OnPublished_Remove_ForwardsToUnsubscribe()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			int callCount = 0;

			EventCallback<TestPayload> callback = (in TestPayload _) =>
			{
				callCount++;
			};

			var firstPayload = new TestPayload(1);
			var secondPayload = new TestPayload(2);

			delayed.OnPublished += callback;
			delayed.Publish(in firstPayload);

			delayed.OnPublished -= callback;
			delayed.Publish(in secondPayload);

			Assert.That(callCount, Is.EqualTo(1));

			delayed.Dispose();
		}

		[Test]
		public void Unsubscribe_RemovesPreviouslySubscribedCallback()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			int callCount = 0;

			EventCallback<TestPayload> callback = (in TestPayload _) =>
			{
				callCount++;
			};

			var firstPayload = new TestPayload(100);
			var secondPayload = new TestPayload(200);

			delayed.Subscribe(callback);
			delayed.Publish(in firstPayload);

			delayed.Unsubscribe(callback);
			delayed.Publish(in secondPayload);

			Assert.That(callCount, Is.EqualTo(1));

			delayed.Dispose();
		}

		[Test]
		public void Publish_WithZeroDelay_ForwardsPayloadImmediately()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			var expected = new TestPayload(777, 888);
			var actual = default(TestPayload);
			int callCount = 0;

			delayed.Subscribe((in TestPayload payload) =>
			{
				actual = payload;
				callCount++;
			});

			delayed.Publish(in expected);

			Assert.Multiple(() =>
			{
				Assert.That(callCount, Is.EqualTo(1));
				Assert.That(actual, Is.EqualTo(expected));
			});

			delayed.Dispose();
		}

		[Test]
		public void Publish_WithPositiveDelay_ForwardsPayloadAfterDelay()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 20);

			var expected = new TestPayload(314, 159);
			var actual = default(TestPayload);
			int callCount = 0;

			using var published = new ManualResetEventSlim(false);

			delayed.Subscribe((in TestPayload payload) =>
			{
				actual = payload;
				Interlocked.Increment(ref callCount);
				published.Set();
			});

			delayed.Publish(in expected);

			Assert.That(published.Wait(TimeSpan.FromSeconds(2)), Is.True);

			Assert.Multiple(() =>
			{
				Assert.That(Volatile.Read(ref callCount), Is.EqualTo(1));
				Assert.That(actual, Is.EqualTo(expected));
			});

			delayed.Dispose();
		}

		[Test]
		public void Publish_CopiesPayloadBeforeScheduling()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 20);

			var original = new TestPayload(1, 2);
			var mutated = original;
			var actual = default(TestPayload);

			using var published = new ManualResetEventSlim(false);

			delayed.Subscribe((in TestPayload payload) =>
			{
				actual = payload;
				published.Set();
			});

			delayed.Publish(in mutated);

			mutated = new TestPayload(999, 999);

			Assert.That(published.Wait(TimeSpan.FromSeconds(2)), Is.True);
			Assert.That(actual, Is.EqualTo(original));

			delayed.Dispose();
		}

		[Test]
		public void Publish_WhenDisposedBeforeDelayedCallbackExecutes_DoesNotForwardToSource()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 100);

			int callCount = 0;

			using var published = new ManualResetEventSlim(false);

			delayed.Subscribe((in TestPayload _) =>
			{
				Interlocked.Increment(ref callCount);
				published.Set();
			});

			var payload = new TestPayload(5);

			delayed.Publish(in payload);
			delayed.Dispose();

			Assert.That(published.Wait(TimeSpan.FromMilliseconds(500)), Is.False);
			Assert.That(Volatile.Read(ref callCount), Is.EqualTo(0));
		}

		[Test]
		public void Dispose_CanBeCalledMultipleTimes()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			Assert.DoesNotThrow(() =>
			{
				delayed.Dispose();
				delayed.Dispose();
				delayed.Dispose();
			});
		}

		[Test]
		public void Dispose_AfterImmediatePublish_DoesNotThrow()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			int callCount = 0;

			delayed.Subscribe((in TestPayload _) =>
			{
				callCount++;
			});

			var payload = new TestPayload(42);

			delayed.Publish(in payload);

			Assert.DoesNotThrow(() => delayed.Dispose());
			Assert.That(callCount, Is.EqualTo(1));
		}

		[Test]
		public async Task PublishAsync_ThrowsNotSupportedException()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			var payload = new TestPayload(1);

			Assert.ThrowsAsync<NotSupportedException>(
				async () => await delayed.PublishAsync(payload)
			);

			await Task.CompletedTask;

			delayed.Dispose();
		}

		[Test]
		public void SubscribeAsync_ThrowsNotSupportedException()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			Assert.Throws<NotSupportedException>(
				() => delayed.SubscribeAsync(null!)
			);

			delayed.Dispose();
		}

		[Test]
		public void UnsubscribeAsync_ThrowsNotSupportedException()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			Assert.Throws<NotSupportedException>(
				() => delayed.UnsubscribeAsync(null!)
			);

			delayed.Dispose();
		}

		[Test]
		public void OnPublishedAsync_Add_ThrowsNotSupportedException()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			Assert.Throws<NotSupportedException>(() =>
			{
				delayed.OnPublishedAsync += null!;
			});

			delayed.Dispose();
		}

		[Test]
		public void OnPublishedAsync_Remove_ThrowsNotSupportedException()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			Assert.Throws<NotSupportedException>(() =>
			{
				delayed.OnPublishedAsync -= null!;
			});

			delayed.Dispose();
		}

#if EVENT_DEBUG
		[Test]
		public void DebugProperties_ForwardValuesFromSource()
		{
			var source = CreateSource();
			var delayed = new DelayedEvent<TestPayload>(source, 0);

			var expected = new TestPayload(55, 66);

			delayed.Subscribe(static (in TestPayload _) =>
			{
			});

			delayed.Publish(in expected);

			Assert.Multiple(() =>
			{
				Assert.That(delayed.LastPayload, Is.EqualTo(expected));
				Assert.That(delayed.SubscriberCount, Is.EqualTo(source.SubscriberCount));
				Assert.That(delayed.PublishCount, Is.EqualTo(source.PublishCount));
				Assert.That(delayed.LastPublishTime, Is.EqualTo(source.LastPublishTime));
			});

			delayed.Dispose();
		}
#endif
	}
}