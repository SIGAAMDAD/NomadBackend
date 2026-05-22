using System;
using System.Diagnostics;
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
	public sealed class ScheduledEventTests
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
			string name = "ScheduledEventTests",
			EventFlags flags = EventFlags.Synchronous)
		{
			/*
				If your SubscriptionSet implementation requires a non-null ILoggerService,
				replace null! with your project's null logger / test logger.

				Example:
					ILoggerService logger = new NullLoggerService();
			*/
			ILoggerService logger = new MockLogger();

			return new GameEvent<TestPayload>(
				new InternString(nameSpace),
				new InternString(name),
				logger,
				flags
			);
		}

		private static bool WaitUntil(Func<bool> predicate, TimeSpan timeout)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();

			while (stopwatch.Elapsed < timeout)
			{
				if (predicate())
				{
					return true;
				}

				Thread.Sleep(5);
			}

			return predicate();
		}

		[Test]
		public void Constructor_WithFixedPayloadAndGameEventSource_CreatesScheduledEvent()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource(
				nameSpace: "Nomad.Tests.Constructor",
				name: "Constructor_WithFixedPayloadAndGameEventSource_CreatesScheduledEvent"
			);

			var payload = new TestPayload(1, 2);

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				payload,
				publishIntervalMS: 1000
			);

			Assert.Multiple(() =>
			{
				Assert.That(scheduled.DebugName, Is.EqualTo(source.DebugName));
				Assert.That(scheduled.NameSpace, Is.EqualTo(source.NameSpace));
				Assert.That(scheduled.Id, Is.EqualTo(source.Id));
			});

			scheduled.Dispose();
		}

		[Test]
		public void Constructor_WithPayloadCallbackAndGameEventSource_CreatesScheduledEvent()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource(
				nameSpace: "Nomad.Tests.Constructor",
				name: "Constructor_WithPayloadCallbackAndGameEventSource_CreatesScheduledEvent"
			);

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				static () => new TestPayload(10, 20),
				publishIntervalMS: 1000
			);

			Assert.Multiple(() =>
			{
				Assert.That(scheduled.DebugName, Is.EqualTo(source.DebugName));
				Assert.That(scheduled.NameSpace, Is.EqualTo(source.NameSpace));
				Assert.That(scheduled.Id, Is.EqualTo(source.Id));
			});

			scheduled.Dispose();
		}

		[Test]
		public void Constructor_WithNullSourceAndFixedPayload_ThrowsInvalidCastException()
		{
			var payload = new TestPayload(1);

			Assert.Throws<InvalidCastException>(
				() => new ScheduledEvent<TestPayload>(
					null!,
					payload,
					publishIntervalMS: 100
				)
			);
		}

		[Test]
		public void Constructor_WithNullSourceAndPayloadCallback_ThrowsInvalidCastException()
		{
			Assert.Throws<InvalidCastException>(
				() => new ScheduledEvent<TestPayload>(
					null!,
					static () => new TestPayload(1),
					publishIntervalMS: 100
				)
			);
		}

		[Test]
		public void Properties_ForwardDebugNameNameSpaceAndIdFromSource()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource(
				nameSpace: "Nomad.Tests.Forwarding",
				name: "Properties_ForwardDebugNameNameSpaceAndIdFromSource"
			);

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(123),
				publishIntervalMS: 1000
			);

			Assert.Multiple(() =>
			{
				Assert.That(scheduled.DebugName, Is.EqualTo("Properties_ForwardDebugNameNameSpaceAndIdFromSource"));
				Assert.That(scheduled.NameSpace, Is.EqualTo("Nomad.Tests.Forwarding"));
				Assert.That(scheduled.Id, Is.EqualTo(source.Id));
			});

			scheduled.Dispose();
		}

		[Test]
		public void Subscribe_ReturnsSubscriptionHandle()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			EventCallback<TestPayload> callback = static (in TestPayload _) =>
			{
			};

			ISubscriptionHandle handle = scheduled.Subscribe(callback);

			Assert.That(handle, Is.Not.Null);

			scheduled.Dispose();
		}

		[Test]
		public void Subscribe_ForwardsCallbackToSource()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			var expected = new TestPayload(10, 20);
			var actual = default(TestPayload);
			int callCount = 0;

			EventCallback<TestPayload> callback = (in TestPayload payload) =>
			{
				actual = payload;
				callCount++;
			};

			scheduled.Subscribe(callback);
			scheduled.Publish(in expected);

			Assert.Multiple(() =>
			{
				Assert.That(callCount, Is.EqualTo(1));
				Assert.That(actual, Is.EqualTo(expected));
			});

			scheduled.Dispose();
		}

		[Test]
		public void OnPublished_Add_ForwardsToSubscribe()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			var expected = new TestPayload(123, 456);
			var actual = default(TestPayload);
			int callCount = 0;

			EventCallback<TestPayload> callback = (in TestPayload payload) =>
			{
				actual = payload;
				callCount++;
			};

			scheduled.OnPublished += callback;
			scheduled.Publish(in expected);

			Assert.Multiple(() =>
			{
				Assert.That(callCount, Is.EqualTo(1));
				Assert.That(actual, Is.EqualTo(expected));
			});

			scheduled.Dispose();
		}

		[Test]
		public void OnPublished_Remove_ForwardsToUnsubscribe()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			int callCount = 0;

			EventCallback<TestPayload> callback = (in TestPayload _) =>
			{
				callCount++;
			};

			var firstPayload = new TestPayload(1);
			var secondPayload = new TestPayload(2);

			scheduled.OnPublished += callback;
			scheduled.Publish(in firstPayload);

			scheduled.OnPublished -= callback;
			scheduled.Publish(in secondPayload);

			Assert.That(callCount, Is.EqualTo(1));

			scheduled.Dispose();
		}

		[Test]
		public void Unsubscribe_RemovesPreviouslySubscribedCallback()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			int callCount = 0;

			EventCallback<TestPayload> callback = (in TestPayload _) =>
			{
				callCount++;
			};

			var firstPayload = new TestPayload(100);
			var secondPayload = new TestPayload(200);

			scheduled.Subscribe(callback);
			scheduled.Publish(in firstPayload);

			scheduled.Unsubscribe(callback);
			scheduled.Publish(in secondPayload);

			Assert.That(callCount, Is.EqualTo(1));

			scheduled.Dispose();
		}

		[Test]
		public void Publish_ForwardsPayloadImmediatelyToSource()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			var expected = new TestPayload(777, 888);
			var actual = default(TestPayload);
			int callCount = 0;

			scheduled.Subscribe((in TestPayload payload) =>
			{
				actual = payload;
				callCount++;
			});

			scheduled.Publish(in expected);

			Assert.Multiple(() =>
			{
				Assert.That(callCount, Is.EqualTo(1));
				Assert.That(actual, Is.EqualTo(expected));
			});

			scheduled.Dispose();
		}

		[Test]
		public void ScheduledFixedPayload_PublishesPayloadOnRecurringInterval()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();
			var expected = new TestPayload(314, 159);

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				expected,
				publishIntervalMS: 20
			);

			var actual = default(TestPayload);
			int callCount = 0;

			scheduled.Subscribe((in TestPayload payload) =>
			{
				actual = payload;
				Interlocked.Increment(ref callCount);
			});

			Assert.That(
				WaitUntil(() => Volatile.Read(ref callCount) >= 2, TimeSpan.FromSeconds(2)),
				Is.True
			);

			Assert.Multiple(() =>
			{
				Assert.That(Volatile.Read(ref callCount), Is.GreaterThanOrEqualTo(2));
				Assert.That(actual, Is.EqualTo(expected));
			});

			scheduled.Dispose();
		}

		[Test]
		public void ScheduledFixedPayload_UsesCapturedPayloadValue()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var original = new TestPayload(1, 2);
			var mutated = original;

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				mutated,
				publishIntervalMS: 20
			);

			mutated = new TestPayload(999, 999);

			var actual = default(TestPayload);
			int callCount = 0;

			scheduled.Subscribe((in TestPayload payload) =>
			{
				actual = payload;
				Interlocked.Increment(ref callCount);
			});

			Assert.That(
				WaitUntil(() => Volatile.Read(ref callCount) >= 1, TimeSpan.FromSeconds(2)),
				Is.True
			);

			Assert.That(actual, Is.EqualTo(original));

			scheduled.Dispose();
		}

		[Test]
		public void ScheduledPayloadCallback_InvokesCallbackForEachRecurringPublish()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			int payloadValue = 0;
			int callCount = 0;
			var lastPayload = default(TestPayload);

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				() =>
				{
					int next = Interlocked.Increment(ref payloadValue);
					return new TestPayload(next, next * 10);
				},
				publishIntervalMS: 20
			);

			scheduled.Subscribe((in TestPayload payload) =>
			{
				lastPayload = payload;
				Interlocked.Increment(ref callCount);
			});

			Assert.That(
				WaitUntil(() => Volatile.Read(ref callCount) >= 3, TimeSpan.FromSeconds(2)),
				Is.True
			);

			Assert.Multiple(() =>
			{
				Assert.That(Volatile.Read(ref callCount), Is.GreaterThanOrEqualTo(3));
				Assert.That(Volatile.Read(ref payloadValue), Is.GreaterThanOrEqualTo(3));
				Assert.That(lastPayload.Value, Is.GreaterThanOrEqualTo(3));
				Assert.That(lastPayload.OtherValue, Is.EqualTo(lastPayload.Value * 10));
			});

			scheduled.Dispose();
		}

		[Test]
		public void Dispose_StopsFutureRecurringPublishes()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(42),
				publishIntervalMS: 20
			);

			int callCount = 0;

			scheduled.Subscribe((in TestPayload _) =>
			{
				Interlocked.Increment(ref callCount);
			});

			Assert.That(
				WaitUntil(() => Volatile.Read(ref callCount) >= 1, TimeSpan.FromSeconds(2)),
				Is.True
			);

			scheduled.Dispose();

			int countAfterDispose = Volatile.Read(ref callCount);

			Thread.Sleep(150);

			Assert.That(Volatile.Read(ref callCount), Is.EqualTo(countAfterDispose));
		}

		[Test]
		public void Dispose_CanBeCalledMultipleTimes()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			Assert.DoesNotThrow(() =>
			{
				scheduled.Dispose();
				scheduled.Dispose();
				scheduled.Dispose();
			});
		}

		[Test]
		public void Dispose_AfterImmediatePublish_DoesNotThrow()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			int callCount = 0;

			scheduled.Subscribe((in TestPayload _) =>
			{
				callCount++;
			});

			var payload = new TestPayload(42);

			scheduled.Publish(in payload);

			Assert.DoesNotThrow(() => scheduled.Dispose());
			Assert.That(callCount, Is.EqualTo(1));
		}

		[Test]
		public async Task PublishAsync_ThrowsNotSupportedException()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			var payload = new TestPayload(1);

			Assert.ThrowsAsync<NotSupportedException>(
				async () => await scheduled.PublishAsync(payload)
			);

			await Task.CompletedTask;

			scheduled.Dispose();
		}

		[Test]
		public void SubscribeAsync_ThrowsNotSupportedException()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			Assert.Throws<NotSupportedException>(
				() => scheduled.SubscribeAsync(null!)
			);

			scheduled.Dispose();
		}

		[Test]
		public void UnsubscribeAsync_ThrowsNotSupportedException()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			Assert.Throws<NotSupportedException>(
				() => scheduled.UnsubscribeAsync(null!)
			);

			scheduled.Dispose();
		}

		[Test]
		public void OnPublishedAsync_Add_ThrowsNotSupportedException()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			Assert.Throws<NotSupportedException>(() =>
			{
				scheduled.OnPublishedAsync += null!;
			});

			scheduled.Dispose();
		}

		[Test]
		public void OnPublishedAsync_Remove_ThrowsNotSupportedException()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			Assert.Throws<NotSupportedException>(() =>
			{
				scheduled.OnPublishedAsync -= null!;
			});

			scheduled.Dispose();
		}

#if EVENT_DEBUG
		[Test]
		public void DebugProperties_ForwardValuesFromSource()
		{
			using var contextScope = new SynchronizationContextScope(null);

			var source = CreateSource();

			var scheduled = new ScheduledEvent<TestPayload>(
				source,
				new TestPayload(1),
				publishIntervalMS: 1000
			);

			var expected = new TestPayload(55, 66);

			scheduled.Subscribe(static (in TestPayload _) =>
			{
			});

			scheduled.Publish(in expected);

			Assert.Multiple(() =>
			{
				Assert.That(scheduled.LastPayload, Is.EqualTo(expected));
				Assert.That(scheduled.SubscriberCount, Is.EqualTo(source.SubscriberCount));
				Assert.That(scheduled.PublishCount, Is.EqualTo(source.PublishCount));
				Assert.That(scheduled.LastPublishTime, Is.EqualTo(source.LastPublishTime));
			});

			scheduled.Dispose();
		}
#endif
	}
}