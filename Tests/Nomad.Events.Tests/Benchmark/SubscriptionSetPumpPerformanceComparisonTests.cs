using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Events.Private.SubscriptionSets;

namespace Nomad.Events.PerformanceTests
{
    [TestFixture]
	[Category("Nomad.Events")]
	[Category("Performance")]
    [Explicit("Performance comparison suite. Run manually; timings are environment-sensitive.")]
    [NonParallelizable]
    public sealed class SubscriptionSetPumpPerformanceComparisonTests
    {
        private const int PublishIterations = 100_000;
        private const int ContainsIterations = 250_000;

        private static readonly SubscriptionSetKind[] AllSyncSetKinds =
        {
            SubscriptionSetKind.LockFree,
            SubscriptionSetKind.Locked,
            SubscriptionSetKind.Atomic
        };

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        [TestCase(16)]
        [TestCase(64)]
        [TestCase(256)]
        public void Pump_SyncSubscribers_CompareThroughput(int subscriberCount)
        {
            var results = new List<PerfResult>();

            foreach (var kind in AllSyncSetKinds)
            {
                using var harness = CreateHarness(kind);

                var subscribers = CreateSubscribers(subscriberCount);
                foreach (var subscriber in subscribers)
                {
                    Assert.That(harness.Set.AddSubscription(subscriber.Callback), Is.True);
                }

                var args = new PerfArgs { Value = 1 };

                // Warm-up outside measurement.
                for (int i = 0; i < 5_000; i++)
                {
                    harness.Set.Pump(in args);
                }

                var result = Measure(
                    name: kind.ToString(),
                    operationCount: PublishIterations,
                    action: () =>
                    {
                        for (int i = 0; i < PublishIterations; i++)
                        {
                            harness.Set.Pump(in args);
                        }
                    }
                );

                long expectedCallsPerSubscriber = PublishIterations + 5_000L;
                foreach (var subscriber in subscribers)
                {
                    Assert.That(
                        subscriber.CallCount,
                        Is.EqualTo(expectedCallsPerSubscriber),
                        $"{kind} did not invoke every subscriber."
                    );
                }

                GC.KeepAlive(subscribers);
                results.Add(result);
            }

            WriteComparison(
                title: $"Sync Pump Throughput | Subscribers={subscriberCount} | Iterations={PublishIterations:N0}",
                results: results
            );
        }

        [Test]
        [TestCase(1)]
        [TestCase(16)]
        [TestCase(128)]
        [TestCase(1_024)]
        public void AddSubscriptions_CompareThroughput(int subscriptionCount)
        {
            var results = new List<PerfResult>();

            foreach (var kind in AllSyncSetKinds)
            {
                using var harness = CreateHarness(kind);
                var subscribers = CreateSubscribers(subscriptionCount);

                var result = Measure(
                    name: kind.ToString(),
                    operationCount: subscriptionCount,
                    action: () =>
                    {
                        foreach (var subscriber in subscribers)
                        {
                            harness.Set.AddSubscription(subscriber.Callback);
                        }
                    }
                );

                Assert.That(harness.Set.SubscriberCount, Is.EqualTo(subscriptionCount));
                GC.KeepAlive(subscribers);
                results.Add(result);
            }

            WriteComparison(
                title: $"Sync Add Throughput | Subscriptions={subscriptionCount:N0}",
                results: results
            );
        }

        [Test]
        [TestCase(1)]
        [TestCase(16)]
        [TestCase(128)]
        [TestCase(1_024)]
        public void AddRemoveSubscriptions_CompareThroughput(int subscriptionCount)
        {
            var results = new List<PerfResult>();

            foreach (var kind in AllSyncSetKinds)
            {
                using var harness = CreateHarness(kind);
                var subscribers = CreateSubscribers(subscriptionCount);

                var result = Measure(
                    name: kind.ToString(),
                    operationCount: subscriptionCount * 2,
                    action: () =>
                    {
                        foreach (var subscriber in subscribers)
                        {
                            harness.Set.AddSubscription(subscriber.Callback);
                        }

                        foreach (var subscriber in subscribers)
                        {
                            harness.Set.RemoveSubscription(subscriber.Callback);
                        }
                    }
                );

                Assert.That(harness.Set.SubscriberCount, Is.Zero);
                GC.KeepAlive(subscribers);
                results.Add(result);
            }

            WriteComparison(
                title: $"Sync Add/Remove Churn | Subscriptions={subscriptionCount:N0}",
                results: results
            );
        }

        [Test]
        [TestCase(1)]
        [TestCase(16)]
        [TestCase(128)]
        [TestCase(1_024)]
        public void ContainsLastCallback_CompareWorstCaseLookup(int subscriptionCount)
        {
            var results = new List<PerfResult>();

            foreach (var kind in AllSyncSetKinds)
            {
                using var harness = CreateHarness(kind);
                var subscribers = CreateSubscribers(subscriptionCount);

                foreach (var subscriber in subscribers)
                {
                    harness.Set.AddSubscription(subscriber.Callback);
                }

                var target = subscribers[^1].Callback;

                var result = Measure(
                    name: kind.ToString(),
                    operationCount: ContainsIterations,
                    action: () =>
                    {
                        for (int i = 0; i < ContainsIterations; i++)
                        {
                            bool found = harness.Set.ContainsCallback(target, out int index);

                            if (!found || index != subscriptionCount - 1)
                            {
                                throw new InvalidOperationException(
                                    $"{kind} failed callback lookup. Found={found}, Index={index}."
                                );
                            }
                        }
                    }
                );

                GC.KeepAlive(subscribers);
                results.Add(result);
            }

            WriteComparison(
                title: $"Worst-Case ContainsCallback | Subscribers={subscriptionCount:N0} | Iterations={ContainsIterations:N0}",
                results: results
            );
        }

        [Test]
        [TestCase(1)]
        [TestCase(16)]
        [TestCase(128)]
        public async Task PumpAsync_CompareLockedVsAtomic(int subscriberCount)
        {
            var asyncSetKinds = new[]
            {
                SubscriptionSetKind.Locked,
                SubscriptionSetKind.Atomic
            };

            var results = new List<PerfResult>();
            var args = new PerfArgs { Value = 1 };

            foreach (var kind in asyncSetKinds)
            {
                using var harness = CreateHarness(kind);
                var subscribers = CreateAsyncSubscribers(subscriberCount);

                foreach (var subscriber in subscribers)
                {
                    Assert.That(harness.Set.AddSubscriptionAsync(subscriber.Callback), Is.True);
                }

                for (int i = 0; i < 1_000; i++)
                {
                    await harness.Set.PumpAsync(args, CancellationToken.None);
                }

                var result = await MeasureAsync(
                    name: kind.ToString(),
                    operationCount: PublishIterations,
                    action: async () =>
                    {
                        for (int i = 0; i < PublishIterations; i++)
                        {
                            await harness.Set.PumpAsync(args, CancellationToken.None);
                        }
                    }
                );

                long expectedCallsPerSubscriber = PublishIterations + 1_000L;
                foreach (var subscriber in subscribers)
                {
                    Assert.That(
                        subscriber.CallCount,
                        Is.EqualTo(expectedCallsPerSubscriber),
                        $"{kind} did not invoke every async subscriber."
                    );
                }

                GC.KeepAlive(subscribers);
                results.Add(result);
            }

            WriteComparison(
                title: $"Async Pump Throughput | Subscribers={subscriberCount} | Iterations={PublishIterations:N0}",
                results: results
            );
        }

        private static SetHarness CreateHarness(SubscriptionSetKind kind)
        {
            var eventData = new PerfGameEvent("Nomad.Events.Tests", "SubscriptionSetPerformance");
            var logger = new MockLogger();

            ISubscriptionSet<PerfArgs> set = kind switch
            {
                SubscriptionSetKind.LockFree =>
                    new LockFreeSubscriptionSet<PerfArgs>(
                        eventData,
                        logger,
                        EventExceptionPolicy.ReportAndContinue
                    ),

                SubscriptionSetKind.Locked =>
                    new SubscriptionSet<PerfArgs>(
                        eventData,
                        logger,
                        EventExceptionPolicy.ReportAndContinue
                    ),

                SubscriptionSetKind.Atomic =>
                    new AtomicSubscriptionSet<PerfArgs>(
                        eventData,
                        logger,
                        EventExceptionPolicy.ReportAndContinue
                    ),

                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };

            return new SetHarness(set, logger);
        }

        private static List<SyncSubscriber> CreateSubscribers(int count)
        {
            var subscribers = new List<SyncSubscriber>(count);

            for (int i = 0; i < count; i++)
            {
                subscribers.Add(new SyncSubscriber());
            }

            return subscribers;
        }

        private static List<AsyncSubscriber> CreateAsyncSubscribers(int count)
        {
            var subscribers = new List<AsyncSubscriber>(count);

            for (int i = 0; i < count; i++)
            {
                subscribers.Add(new AsyncSubscriber());
            }

            return subscribers;
        }

        private static PerfResult Measure(string name, int operationCount, Action action)
        {
            ForceFullCollection();

            long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            long gen0Before = GC.CollectionCount(0);
            long gen1Before = GC.CollectionCount(1);
            long gen2Before = GC.CollectionCount(2);

            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();

            long allocatedAfter = GC.GetTotalAllocatedBytes(precise: true);

            return new PerfResult(
                Name: name,
                Elapsed: stopwatch.Elapsed,
                OperationCount: operationCount,
                AllocatedBytes: allocatedAfter - allocatedBefore,
                Gen0Collections: GC.CollectionCount(0) - gen0Before,
                Gen1Collections: GC.CollectionCount(1) - gen1Before,
                Gen2Collections: GC.CollectionCount(2) - gen2Before
            );
        }

        private static async Task<PerfResult> MeasureAsync(
            string name,
            int operationCount,
            Func<Task> action
        )
        {
            ForceFullCollection();

            long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            long gen0Before = GC.CollectionCount(0);
            long gen1Before = GC.CollectionCount(1);
            long gen2Before = GC.CollectionCount(2);

            var stopwatch = Stopwatch.StartNew();
            await action();
            stopwatch.Stop();

            long allocatedAfter = GC.GetTotalAllocatedBytes(precise: true);

            return new PerfResult(
                Name: name,
                Elapsed: stopwatch.Elapsed,
                OperationCount: operationCount,
                AllocatedBytes: allocatedAfter - allocatedBefore,
                Gen0Collections: GC.CollectionCount(0) - gen0Before,
                Gen1Collections: GC.CollectionCount(1) - gen1Before,
                Gen2Collections: GC.CollectionCount(2) - gen2Before
            );
        }

        private static void WriteComparison(string title, IReadOnlyList<PerfResult> results)
        {
            TestContext.Out.WriteLine("");
            TestContext.Out.WriteLine(title);
            TestContext.Out.WriteLine(new string('-', title.Length));
            TestContext.Out.WriteLine(
                $"{"Set",-12} {"Total ms",12} {"ns/op",12} {"Ops/sec",14} {"Allocated",14} {"Gen0",8} {"Gen1",8} {"Gen2",8}"
            );

            foreach (var result in results.OrderBy(result => result.NanosecondsPerOperation))
            {
                TestContext.Out.WriteLine(
                    $"{result.Name,-12} " +
                    $"{result.Elapsed.TotalNanoseconds,12:N3} " +
                    $"{result.NanosecondsPerOperation,12:N2} " +
                    $"{result.OperationsPerSecond,14:N0} " +
                    $"{result.AllocatedBytes,14:N0} " +
                    $"{result.Gen0Collections,8:N0} " +
                    $"{result.Gen1Collections,8:N0} " +
                    $"{result.Gen2Collections,8:N0}"
                );
            }

            var fastest = results.OrderBy(result => result.NanosecondsPerOperation).First();

            TestContext.Out.WriteLine("");
            TestContext.Out.WriteLine($"Fastest: {fastest.Name}");

            foreach (var result in results.OrderBy(result => result.NanosecondsPerOperation).Skip(1))
            {
                TestContext.Out.WriteLine(
                    $"{result.Name} is {result.NanosecondsPerOperation / fastest.NanosecondsPerOperation:N2}x slower than {fastest.Name}."
                );
            }
        }

        private static void ForceFullCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private enum SubscriptionSetKind
        {
            LockFree,
            Locked,
            Atomic
        }

        private readonly record struct PerfResult(
            string Name,
            TimeSpan Elapsed,
            int OperationCount,
            long AllocatedBytes,
            long Gen0Collections,
            long Gen1Collections,
            long Gen2Collections
        )
        {
            public double NanosecondsPerOperation =>
                Elapsed.TotalSeconds * 1_000_000_000.0 / Math.Max(1, OperationCount);

            public double OperationsPerSecond =>
                OperationCount / Math.Max(Elapsed.TotalSeconds, double.Epsilon);
        }

        private readonly struct PerfArgs
        {
            public int Value { get; init; }
        }

        private sealed class SyncSubscriber
        {
            public long CallCount;

            public EventCallback<PerfArgs> Callback => OnEvent;

            private void OnEvent(in PerfArgs args)
            {
                CallCount += args.Value;
            }
        }

        private sealed class AsyncSubscriber
        {
            public long CallCount;

            public AsyncEventCallback<PerfArgs> Callback => OnEventAsync;

            private Task OnEventAsync(PerfArgs args, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                CallCount += args.Value;
                return Task.CompletedTask;
            }
        }

        private sealed class SetHarness : IDisposable
        {
            public ISubscriptionSet<PerfArgs> Set { get; }

            private readonly ILoggerService _logger;

            public SetHarness(ISubscriptionSet<PerfArgs> set, ILoggerService logger)
            {
                Set = set;
                _logger = logger;
            }

            public void Dispose()
            {
                Set.Dispose();
                _logger.Dispose();
            }
        }

        private sealed class PerfGameEvent : IGameEvent<PerfArgs>
        {
            public string DebugName { get; }
            public string NameSpace { get; }
            public int Id { get; }

#if EVENT_DEBUG
            public int SubscriberCount { get; } = 0;
			public long PublishCount { get; } = 0;
			public DateTime LastPublishTime { get; } = DateTime.MinValue;
            public PerfArgs LastPayload { get; } = default;
#endif

            public PerfGameEvent(string nameSpace, string debugName)
            {
                NameSpace = nameSpace;
                DebugName = debugName;
                Id = HashCode.Combine(nameSpace, debugName);
            }

            public event EventCallback<PerfArgs> OnPublished
            {
                add { }
                remove { }
            }

            public event AsyncEventCallback<PerfArgs> OnPublishedAsync
            {
                add { }
                remove { }
            }

            public void Publish(in PerfArgs eventArgs)
            {
                throw new NotSupportedException("This test stub is metadata-only.");
            }

            public Task PublishAsync(PerfArgs eventArgs, CancellationToken ct = default)
            {
                throw new NotSupportedException("This test stub is metadata-only.");
            }

            public ISubscriptionHandle Subscribe(EventCallback<PerfArgs> callback)
            {
                throw new NotSupportedException("This test stub is metadata-only.");
            }

            public ISubscriptionHandle SubscribeAsync(AsyncEventCallback<PerfArgs> asyncCallback)
            {
                throw new NotSupportedException("This test stub is metadata-only.");
            }

            public void Unsubscribe(EventCallback<PerfArgs> callback)
            {
                throw new NotSupportedException("This test stub is metadata-only.");
            }

            public void UnsubscribeAsync(AsyncEventCallback<PerfArgs> asyncCallback)
            {
                throw new NotSupportedException("This test stub is metadata-only.");
            }

#if NET7_0_OR_GREATER
            public void operator +=(EventCallback<PerfArgs> other)
            {
                throw new NotSupportedException("This test stub is metadata-only.");
            }

            public void operator +=(AsyncEventCallback<PerfArgs> other)
            {
                throw new NotSupportedException("This test stub is metadata-only.");
            }

            public void operator -=(EventCallback<PerfArgs> other)
            {
                throw new NotSupportedException("This test stub is metadata-only.");
            }

            public void operator -=(AsyncEventCallback<PerfArgs> other)
            {
                throw new NotSupportedException("This test stub is metadata-only.");
            }
#endif

            public void Dispose()
            {
            }
        }
    }
}