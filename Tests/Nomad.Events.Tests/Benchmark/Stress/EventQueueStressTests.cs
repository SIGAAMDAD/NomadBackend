using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Nomad.Core.Events;
using Nomad.Events.Private;
using Nomad.Core.Util;
using System.Diagnostics.Eventing.Reader;

namespace Nomad.Events.Tests.Stress
{
	[TestFixture]
	[Category("Stress")]
	[Category("Nomad.Events")]
	public class EventQueueStressTests
	{
		// Simple payload for testing
		private readonly struct TestArgs
		{
			public int Id { get; }
			public TestArgs(int id) => Id = id;
		}

		// Helper to create a mock event that records published IDs
		private static IGameEvent<TestArgs> CreateMockEvent(ConcurrentBag<int> receivedIds)
		{
			var logger = new MockLogger();
			var mock = new GameEvent<TestArgs>(new InternString("Test"), new InternString("TestNamespace"), logger, EventFlags.Default);
			mock.Subscribe((in TestArgs args) => receivedIds.Add(args.Id));
			return mock;
		}

		// --------------------------------------------------------------------
		// 1. Single producer, single consumer – ProcessAll
		// --------------------------------------------------------------------
		[Test]
		public void SingleProducerSingleConsumer_ProcessAll_AllEventsProcessed()
		{
			var queue = new EventQueue();
			var receivedIds = new ConcurrentBag<int>();
			var mockEvent = CreateMockEvent(receivedIds);
			const int eventCount = 10000;

			for (int i = 0; i < eventCount; i++)
				queue.Enqueue(mockEvent, new TestArgs(i));

			queue.ProcessAll();

			Assert.That(receivedIds.Count, Is.EqualTo(eventCount));
			for (int i = 0; i < eventCount; i++)
				Assert.That(receivedIds, Does.Contain(i));
		}

		// --------------------------------------------------------------------
		// 2. Multiple producers, single consumer – ProcessAll
		// --------------------------------------------------------------------
		[Test]
		public void MultipleProducersSingleConsumer_ProcessAll_AllEventsProcessed()
		{
			var queue = new EventQueue();
			var receivedIds = new ConcurrentBag<int>();
			var mockEvent = CreateMockEvent(receivedIds);
			const int producerCount = 8;
			const int eventsPerProducer = 2000;
			int totalEvents = producerCount * eventsPerProducer;

			Parallel.For(0, producerCount, p =>
			{
				for (int i = 0; i < eventsPerProducer; i++)
				{
					int id = p * eventsPerProducer + i;
					queue.Enqueue(mockEvent, new TestArgs(id));
				}
			});

			queue.ProcessAll();

			Assert.That(receivedIds, Has.Count.EqualTo(totalEvents));
			for (int i = 0; i < totalEvents; i++)
				Assert.That(receivedIds, Does.Contain(i));
		}

		// --------------------------------------------------------------------
		// 3. Multiple producers, multiple consumers – ProcessNext
		// --------------------------------------------------------------------
		[Test]
		public void MultipleProducersMultipleConsumers_ProcessNext_AllEventsProcessed()
		{
			var queue = new EventQueue();
			var receivedIds = new ConcurrentBag<int>();
			var mockEvent = CreateMockEvent(receivedIds);
			const int producerCount = 4;
			const int consumerCount = 4;
			const int eventsPerProducer = 2000;
			int totalEvents = producerCount * eventsPerProducer;
			long processedCount = 0;

			// Produce
			Parallel.For(0, producerCount, p =>
			{
				for (int i = 0; i < eventsPerProducer; i++)
				{
					int id = p * eventsPerProducer + i;
					queue.Enqueue(mockEvent, new TestArgs(id));
				}
			});

			// Consume on multiple threads until all events are processed
			var consumers = new Task[consumerCount];
			for (int c = 0; c < consumerCount; c++)
			{
				consumers[c] = Task.Run(() =>
				{
					while (Interlocked.Read(ref processedCount) < totalEvents)
					{
						if (queue.ProcessNext())
							Interlocked.Increment(ref processedCount);
						else
							Thread.Yield(); // Avoid tight loop when queue is empty
					}
				});
			}

			Task.WaitAll(consumers);

			Assert.That(receivedIds, Has.Count.EqualTo(totalEvents));
			for (int i = 0; i < totalEvents; i++)
				Assert.That(receivedIds, Does.Contain(i));
		}

		// --------------------------------------------------------------------
		// 5. Pool reuse – memory allocations should be minimal
		// --------------------------------------------------------------------
		[Test]
		public void PoolReuse_AfterProcessing_ObjectsReturnedToPool()
		{
			var queue = new EventQueue();
			var receivedIds = new ConcurrentBag<int>();
			var mockEvent = CreateMockEvent(receivedIds);
			const int iterations = 10000;

			// Force GC to clean up before measurement
			GC.Collect();
			GC.WaitForPendingFinalizers();
			long beforeBytes = GC.GetAllocatedBytesForCurrentThread();

			for (int i = 0; i < iterations; i++)
				queue.Enqueue(mockEvent, new TestArgs(i));
			queue.ProcessAll();

			long afterBytes = GC.GetAllocatedBytesForCurrentThread();
			long bytesPerEvent = (afterBytes - beforeBytes) / iterations;

			System.Console.WriteLine($"Bytes allocated per event: {bytesPerEvent}");

			// Expect near‑zero allocations after warm‑up; a small tolerance is acceptable.
			Assert.That(bytesPerEvent, Is.LessThan(100), "Pool is not reusing objects effectively; too many allocations.");
		}

		// --------------------------------------------------------------------
		// 6. Concurrent enqueue and process – no lost events
		// --------------------------------------------------------------------
		[Test]
		public void ConcurrentEnqueueAndProcess_NoLostEvents()
		{
			var queue = new EventQueue();
			var receivedIds = new ConcurrentBag<int>();
			var mockEvent = CreateMockEvent(receivedIds);
			const int totalEvents = 10000;

			var producer = Task.Run(() =>
			{
				for (int i = 0; i < totalEvents; i++)
					queue.Enqueue(mockEvent, new TestArgs(i));
			});

			var consumer = Task.Run(() =>
			{
				int processed = 0;
				while (processed < totalEvents)
				{
					if (queue.ProcessNext())
						processed++;
					else
						Thread.Yield();
				}
			});

			Task.WaitAll(producer, consumer);

			Assert.That(receivedIds, Has.Count.EqualTo(totalEvents));
			for (int i = 0; i < totalEvents; i++)
				Assert.That(receivedIds, Does.Contain(i));
		}

		// --------------------------------------------------------------------
		// 7. Clear – removes all pending events
		// --------------------------------------------------------------------
		[Test]
		public void Clear_RemovesAllPendingEvents()
		{
			var queue = new EventQueue();
			var receivedIds = new ConcurrentBag<int>();
			var mockEvent = CreateMockEvent(receivedIds);
			const int eventCount = 1000;

			for (int i = 0; i < eventCount; i++)
				queue.Enqueue(mockEvent, new TestArgs(i));

			queue.Clear();
			queue.ProcessAll(); // Should process nothing

			using (Assert.EnterMultipleScope())
			{
				Assert.That(receivedIds.Count, Is.Zero);
				Assert.That(queue.Count, Is.Zero);
			}
		}
	}
}