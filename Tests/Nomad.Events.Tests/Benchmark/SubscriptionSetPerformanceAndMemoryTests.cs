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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nomad.Core.Events;
using Nomad.Events.Private.SubscriptionSets;

namespace Nomad.Events.Tests {
	[TestFixture]
	[Category("Performance")]
	[Category("Nomad.Events")]
	public class SubscriptionSetPerformanceAndMemoryTests {
		private const int WarmupIterations = 256;
		private const int PublishIterations = 20_000;
		private const int AsyncPublishIterations = 4_000;
		private const int MutationBatches = 128;
		private const int SubscriberCount = 32;
		private static readonly TestArgs SampleArgs = new TestArgs( 7 );
		private static readonly MockLogger logger = new MockLogger();

		private static IEnumerable<TestCaseData> SyncImplementations() {
			yield return new TestCaseData( "SubscriptionSet" );
			yield return new TestCaseData( "LockFreeSubscriptionSet" );
			yield return new TestCaseData( "AtomicSubscriptionSet" );
		}

		private static IEnumerable<TestCaseData> AsyncImplementations() {
			yield return new TestCaseData( "SubscriptionSet" );
			yield return new TestCaseData( "AtomicSubscriptionSet" );
		}

		[OneTimeTearDown]
		public void OneTimeTearDown() {
			logger?.Dispose();
		}

		[TestCaseSource( nameof( SyncImplementations ) )]
		[Category( "Memory" )]
		public void SyncPublish_RemainsEffectivelyAllocationFree( string implementationName ) {
			Func<ISubscriptionSet<TestArgs>> factory = ResolveFactory( implementationName );
			using ISubscriptionSet<TestArgs> set = factory();
			int[] sink = new int[SubscriberCount];

			foreach ( EventCallback<TestArgs> callback in CreateSyncCallbacks( sink ) ) {
				Assert.That( set.AddSubscription( callback ), Is.True );
			}

			for ( int i = 0; i < WarmupIterations; i++ ) {
				set.Pump( in SampleArgs );
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
			for ( int i = 0; i < PublishIterations; i++ ) {
				set.Pump( in SampleArgs );
			}
			long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - beforeBytes;
			double bytesPerPublish = (double)allocatedBytes / PublishIterations;

			TestContext.Out.WriteLine( $"{implementationName} sync publish allocated {allocatedBytes} bytes total ({bytesPerPublish:F4} bytes/publish)." );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( sink.Sum(), Is.GreaterThan( 0 ) );
				Assert.That( set.PublishCount, Is.EqualTo( WarmupIterations + PublishIterations ) );
				Assert.That( bytesPerPublish, Is.LessThanOrEqualTo( 1.0d ), $"{implementationName} sync publish should stay effectively allocation-free." );
			}
		}

		[TestCaseSource( nameof( SyncImplementations ) )]
		[Category( "Performance" )]
		[Explicit( "Profiles synchronous publish throughput and mutation churn for manual performance runs." )]
		public void SyncPublish_AndMutation_Profile_AllImplementations( string implementationName ) {
			Func<ISubscriptionSet<TestArgs>> factory = ResolveFactory( implementationName );
			BenchmarkResult publishMetrics = MeasureSyncPublish( factory );
			BenchmarkResult mutationMetrics = MeasureSyncMutation( factory );

			TestContext.Out.WriteLine( $"{implementationName} sync publish: {publishMetrics}" );
			TestContext.Out.WriteLine( $"{implementationName} mutation churn: {mutationMetrics}" );

			Assert.That( publishMetrics.OperationsPerSecond, Is.GreaterThan( 0 ) );
			Assert.That( mutationMetrics.OperationsPerSecond, Is.GreaterThan( 0 ) );
		}

		[TestCaseSource( nameof( AsyncImplementations ) )]
		[Category( "Performance" )]
		[Explicit( "Profiles asynchronous publish throughput and allocations for manual performance runs." )]
		public void AsyncPublish_Profile_SupportedImplementations( string implementationName ) {
			Func<ISubscriptionSet<TestArgs>> factory = ResolveFactory( implementationName );
			BenchmarkResult metrics = MeasureAsyncPublish( factory );

			TestContext.Out.WriteLine( $"{implementationName} async publish: {metrics}" );

			Assert.That( metrics.OperationsPerSecond, Is.GreaterThan( 0 ) );
		}

		[TestCase( "SubscriptionSet" )]
		[TestCase( "AtomicSubscriptionSet" )]
		[Category( "Performance" )]
		[Explicit( "Profiles concurrent publishing on the thread-safe subscription sets." )]
		public void ConcurrentPublish_Profile_ThreadSafeImplementations( string implementationName ) {
			Func<ISubscriptionSet<TestArgs>> factory = implementationName == "AtomicSubscriptionSet"
				? CreateAtomicSubscriptionSet
				: CreateSubscriptionSet;

			using ISubscriptionSet<TestArgs> set = factory();
			long[] sink = new long[SubscriberCount];
			foreach ( EventCallback<TestArgs> callback in CreateConcurrentSyncCallbacks( sink ) ) {
				set.AddSubscription( callback );
			}

			const int publisherThreads = 4;
			const int publishesPerThread = 10_000;
			for ( int i = 0; i < WarmupIterations; i++ ) {
				set.Pump( in SampleArgs );
			}

			Stopwatch stopwatch = Stopwatch.StartNew();
			Task[] tasks = new Task[publisherThreads];
			for ( int i = 0; i < publisherThreads; i++ ) {
				tasks[i] = Task.Run( () => {
					for ( int j = 0; j < publishesPerThread; j++ ) {
						set.Pump( in SampleArgs );
					}
				} );
			}

			Task.WaitAll( tasks );
			stopwatch.Stop();

			int expectedInvocations = SubscriberCount * ( WarmupIterations + ( publisherThreads * publishesPerThread ) );
			double operationsPerSecond = ( publisherThreads * publishesPerThread ) / stopwatch.Elapsed.TotalSeconds;

			TestContext.Out.WriteLine( $"{implementationName} concurrent publish: {operationsPerSecond:N0} publishes/sec over {publisherThreads} threads." );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( sink.Sum(), Is.EqualTo( expectedInvocations * SampleArgs.Value ) );
				Assert.That( set.PublishCount, Is.EqualTo( WarmupIterations + ( publisherThreads * publishesPerThread ) ) );
			}
		}

		private static BenchmarkResult MeasureSyncPublish( Func<ISubscriptionSet<TestArgs>> factory ) {
			using ISubscriptionSet<TestArgs> set = factory();
			int[] sink = new int[SubscriberCount];

			foreach ( EventCallback<TestArgs> callback in CreateSyncCallbacks( sink ) ) {
				set.AddSubscription( callback );
			}

			for ( int i = 0; i < WarmupIterations; i++ ) {
				set.Pump( in SampleArgs );
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
			Stopwatch stopwatch = Stopwatch.StartNew();
			for ( int i = 0; i < PublishIterations; i++ ) {
				set.Pump( in SampleArgs );
			}
			stopwatch.Stop();
			long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - beforeBytes;

			Assert.That( sink.Sum(), Is.GreaterThan( 0 ) );

			return new BenchmarkResult( PublishIterations, allocatedBytes, stopwatch.Elapsed );
		}

		private static BenchmarkResult MeasureSyncMutation( Func<ISubscriptionSet<TestArgs>> factory ) {
			using ISubscriptionSet<TestArgs> set = factory();
			EventCallback<TestArgs>[] callbacks = CreateSyncCallbacks( new int[SubscriberCount] );

			for ( int i = 0; i < 8; i++ ) {
				RunMutationBatch( set, callbacks );
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
			Stopwatch stopwatch = Stopwatch.StartNew();
			for ( int i = 0; i < MutationBatches; i++ ) {
				RunMutationBatch( set, callbacks );
			}
			stopwatch.Stop();
			long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - beforeBytes;

			Assert.That( set.SubscriberCount, Is.Zero );

			return new BenchmarkResult( MutationBatches * SubscriberCount * 2L, allocatedBytes, stopwatch.Elapsed );
		}

		private static BenchmarkResult MeasureAsyncPublish( Func<ISubscriptionSet<TestArgs>> factory ) {
			using ISubscriptionSet<TestArgs> set = factory();
			int[] sink = new int[SubscriberCount];

			foreach ( AsyncEventCallback<TestArgs> callback in CreateAsyncCallbacks( sink ) ) {
				set.AddSubscriptionAsync( callback );
			}

			for ( int i = 0; i < WarmupIterations; i++ ) {
				set.PumpAsync( SampleArgs, CancellationToken.None ).GetAwaiter().GetResult();
			}

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			long beforeBytes = GC.GetTotalAllocatedBytes( true );
			Stopwatch stopwatch = Stopwatch.StartNew();
			for ( int i = 0; i < AsyncPublishIterations; i++ ) {
				set.PumpAsync( SampleArgs, CancellationToken.None ).GetAwaiter().GetResult();
			}
			stopwatch.Stop();
			long allocatedBytes = GC.GetTotalAllocatedBytes( true ) - beforeBytes;

			Assert.That( sink.Sum(), Is.GreaterThan( 0 ) );

			return new BenchmarkResult( AsyncPublishIterations, allocatedBytes, stopwatch.Elapsed );
		}

		private static void RunMutationBatch( ISubscriptionSet<TestArgs> set, IReadOnlyList<EventCallback<TestArgs>> callbacks ) {
			for ( int i = 0; i < callbacks.Count; i++ ) {
				set.AddSubscription( callbacks[i] );
			}

			for ( int i = 0; i < callbacks.Count; i++ ) {
				set.RemoveSubscription( callbacks[i] );
			}
		}

		private static EventCallback<TestArgs>[] CreateSyncCallbacks( int[] sink ) {
			var callbacks = new EventCallback<TestArgs>[sink.Length];
			for ( int i = 0; i < callbacks.Length; i++ ) {
				int slot = i;
				callbacks[i] = ( in TestArgs args ) => sink[slot] += args.Value;
			}

			return callbacks;
		}

		private static EventCallback<TestArgs>[] CreateConcurrentSyncCallbacks( long[] sink ) {
			var callbacks = new EventCallback<TestArgs>[sink.Length];
			for ( int i = 0; i < callbacks.Length; i++ ) {
				int slot = i;
				callbacks[i] = ( in TestArgs args ) => Interlocked.Add( ref sink[slot], args.Value );
			}

			return callbacks;
		}

		private static AsyncEventCallback<TestArgs>[] CreateAsyncCallbacks( int[] sink ) {
			var callbacks = new AsyncEventCallback<TestArgs>[sink.Length];
			for ( int i = 0; i < callbacks.Length; i++ ) {
				int slot = i;
				callbacks[i] = ( TestArgs args, CancellationToken ct ) => {
					sink[slot] += args.Value;
					return Task.CompletedTask;
				};
			}

			return callbacks;
		}

		private static ISubscriptionSet<TestArgs> CreateSubscriptionSet() {
			return new SubscriptionSet<TestArgs>( new TestEventMetadata( "SubscriptionSet" ), logger, EventExceptionPolicy.AggregateAfterDispatch );
		}

		private static ISubscriptionSet<TestArgs> CreateLockFreeSubscriptionSet() {
			return new LockFreeSubscriptionSet<TestArgs>( new TestEventMetadata( "LockFreeSubscriptionSet" ),  logger, EventExceptionPolicy.ReportAndContinue );
		}

		private static ISubscriptionSet<TestArgs> CreateAtomicSubscriptionSet() {
			return new AtomicSubscriptionSet<TestArgs>( new TestEventMetadata( "AtomicSubscriptionSet" ), logger, EventExceptionPolicy.AggregateAfterDispatch );
		}

		private static Func<ISubscriptionSet<TestArgs>> ResolveFactory( string implementationName ) {
			return implementationName switch {
				"SubscriptionSet" => CreateSubscriptionSet,
				"LockFreeSubscriptionSet" => CreateLockFreeSubscriptionSet,
				"AtomicSubscriptionSet" => CreateAtomicSubscriptionSet,
				_ => throw new ArgumentOutOfRangeException( nameof( implementationName ), implementationName, "Unknown subscription set implementation." )
			};
		}

		private readonly record struct TestArgs( int Value );

		private readonly record struct BenchmarkResult( long Operations, long AllocatedBytes, TimeSpan Elapsed ) {
			public double OperationsPerSecond => Elapsed.TotalSeconds <= 0.0d ? 0.0d : Operations / Elapsed.TotalSeconds;

			public override string ToString() {
				return $"{Operations:N0} ops in {Elapsed.TotalMilliseconds:N2} ms, {OperationsPerSecond:N0} ops/sec, {AllocatedBytes:N0} B allocated";
			}
		}

		private sealed class TestEventMetadata : IGameEvent<TestArgs> {
			public string DebugName { get; }
			public string NameSpace => "Nomad.Input.Tests";
			public int Id => HashCode.Combine( DebugName, typeof( TestArgs ) );

#if EVENT_DEBUG
			public int SubscriberCount { get; } = 0;
			public long PublishCount { get; } = 0;
			public DateTime LastPublishTime { get; } = DateTime.MinValue;
			public TestArgs LastPayload { get; } = default;
#endif

			public event EventCallback<TestArgs> OnPublished {
				add { }
				remove { }
			}

			public event AsyncEventCallback<TestArgs> OnPublishedAsync {
				add { }
				remove { }
			}

			public TestEventMetadata( string debugName ) {
				DebugName = debugName;
			}

			public void Dispose() {
			}

			public void Publish( in TestArgs eventArgs ) {
				throw new NotSupportedException();
			}

			public Task PublishAsync( TestArgs eventArgs, CancellationToken ct = default ) {
				throw new NotSupportedException();
			}

			public ISubscriptionHandle Subscribe( EventCallback<TestArgs> callback ) {
				throw new NotSupportedException();
			}

			public ISubscriptionHandle SubscribeAsync( AsyncEventCallback<TestArgs> asyncCallback ) {
				throw new NotSupportedException();
			}

			public void Unsubscribe( EventCallback<TestArgs> callback ) {
				throw new NotSupportedException();
			}

			public void UnsubscribeAsync( AsyncEventCallback<TestArgs> asyncCallback ) {
				throw new NotSupportedException();
			}
		}
	}
}
