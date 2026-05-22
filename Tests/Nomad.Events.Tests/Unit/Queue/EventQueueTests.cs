using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Nomad.Events.Private;
using Nomad.Core.Events; // Contains EventQueue

namespace Nomad.Events.Tests {

    // Test structs for event arguments
    public struct TestArgs {
        public int Value { get; set; }
    }

    public struct OtherArgs {
        public string Text { get; set; }
    }

    [TestFixture]
    [Category( "Nomad.Events" )]
    [Category( "Queue" )]
    [Category( "Unit" )]
    public class EventQueueTests {
        private EventQueue _queue;

        [SetUp]
        public void Setup() {
            _queue = new EventQueue();
        }

        [Test]
        public void Constructor_InitializesQueuesForAllPriorities() {
            // Count should be zero initially
            Assert.That( _queue.Count, Is.Zero );
        }

        [Test]
        public void Enqueue_SingleEvent_CountIncrements() {
            var mockEvent = new MockGameEvent<TestArgs>();
            var args = new TestArgs { Value = 42 };

            _queue.Enqueue( mockEvent, args, EventPriority.Normal );

            Assert.That( _queue.Count, Is.EqualTo( 1 ) );
        }

        [Test]
        public void Enqueue_MultipleEvents_CountMatches() {
            var mockEvent1 = new MockGameEvent<TestArgs>();
            var mockEvent2 = new MockGameEvent<TestArgs>();
            var args1 = new TestArgs { Value = 1 };
            var args2 = new TestArgs { Value = 2 };

            _queue.Enqueue( mockEvent1, args1, EventPriority.Low );
            _queue.Enqueue( mockEvent2, args2, EventPriority.High );

            Assert.That( _queue.Count, Is.EqualTo( 2 ) );
        }

        [Test]
        public void Enqueue_DifferentArgumentTypes_Works() {
            var mockEvent1 = new MockGameEvent<TestArgs>();
            var mockEvent2 = new MockGameEvent<OtherArgs>();
            var args1 = new TestArgs { Value = 10 };
            var args2 = new OtherArgs { Text = "Hello" };

            _queue.Enqueue( mockEvent1, args1, EventPriority.Normal );
            _queue.Enqueue( mockEvent2, args2, EventPriority.Normal );

            Assert.That( _queue.Count, Is.EqualTo( 2 ) );
        }

        [Test]
        public void ProcessAll_EmptyQueue_DoesNothing() {
            _queue.ProcessAll();
            Assert.That( _queue.Count, Is.Zero );
        }

        [Test]
        public void ProcessAll_ProcessesEventsInPriorityOrder() {
            var processedOrder = new List<(string EventName, EventPriority Priority, int Value)>();
            var criticalEvent = CreateTrackingEvent<TestArgs>( "Critical", ( args ) => processedOrder.Add( ("Critical", EventPriority.Critical, args.Value) ) );
            var highEvent = CreateTrackingEvent<TestArgs>( "High", ( args ) => processedOrder.Add( ("High", EventPriority.High, args.Value) ) );
            var normalEvent = CreateTrackingEvent<TestArgs>( "Normal", ( args ) => processedOrder.Add( ("Normal", EventPriority.Normal, args.Value) ) );
            var lowEvent = CreateTrackingEvent<TestArgs>( "Low", ( args ) => processedOrder.Add( ("Low", EventPriority.Low, args.Value) ) );
            var veryLowEvent = CreateTrackingEvent<TestArgs>( "VeryLow", ( args ) => processedOrder.Add( ("VeryLow", EventPriority.VeryLow, args.Value) ) );

            // Enqueue in mixed order
            _queue.Enqueue( normalEvent, new TestArgs { Value = 3 }, EventPriority.Normal );
            _queue.Enqueue( veryLowEvent, new TestArgs { Value = 5 }, EventPriority.VeryLow );
            _queue.Enqueue( criticalEvent, new TestArgs { Value = 1 }, EventPriority.Critical );
            _queue.Enqueue( lowEvent, new TestArgs { Value = 4 }, EventPriority.Low );
            _queue.Enqueue( highEvent, new TestArgs { Value = 2 }, EventPriority.High );

            _queue.ProcessAll();

            // Expected order: Critical (1), High (2), Normal (3), Low (4), VeryLow (5)
            Assert.That( processedOrder, Has.Count.EqualTo( 5 ) );
            using ( Assert.EnterMultipleScope() ) {
                Assert.That( processedOrder[ 0 ].Priority, Is.EqualTo( EventPriority.Critical ) );
                Assert.That( processedOrder[ 0 ].Value, Is.EqualTo( 1 ) );
                Assert.That( processedOrder[ 1 ].Priority, Is.EqualTo( EventPriority.High ) );
                Assert.That( processedOrder[ 1 ].Value, Is.EqualTo( 2 ) );
                Assert.That( processedOrder[ 2 ].Priority, Is.EqualTo( EventPriority.Normal ) );
                Assert.That( processedOrder[ 2 ].Value, Is.EqualTo( 3 ) );
                Assert.That( processedOrder[ 3 ].Priority, Is.EqualTo( EventPriority.Low ) );
                Assert.That( processedOrder[ 3 ].Value, Is.EqualTo( 4 ) );
                Assert.That( processedOrder[ 4 ].Priority, Is.EqualTo( EventPriority.VeryLow ) );
                Assert.That( processedOrder[ 4 ].Value, Is.EqualTo( 5 ) );
            }
        }

        [Test]
        public void ConcurrentEnqueue_ProcessAll_NoLoss() {
            const int threadCount = 4;
            const int eventsPerThread = 1000;
            var queue = new EventQueue();
            var mockEvent = new MockGameEvent<TestArgs>();
            var enqueuedCount = 0;

            var tasks = new Task[ threadCount ];
            for ( int t = 0; t < threadCount; t++ ) {
                tasks[ t ] = Task.Run( () => {
                    for ( int i = 0; i < eventsPerThread; i++ ) {
                        queue.Enqueue( mockEvent, new TestArgs { Value = i }, EventPriority.Normal );
                        Interlocked.Increment( ref enqueuedCount );
                    }
                } );
            }

            Task.WaitAll( tasks );

            queue.ProcessAll();

            using ( Assert.EnterMultipleScope() ) {
                Assert.That( mockEvent.PublishCount, Is.EqualTo( enqueuedCount ) );
                Assert.That( queue.Count, Is.Zero );
            }
        }

        [Test]
        public void ConcurrentEnqueueAndProcessNext_AllEventsProcessed() {
            const int threadCount = 4;
            const int eventsPerThread = 500;
            var queue = new EventQueue();
            var mockEvent = new MockGameEvent<TestArgs>();
            var enqueuedCount = 0;
            var processedCount = 0;
            int totalEvents = threadCount * eventsPerThread;

            var barrier = new Barrier( threadCount * 2 ); // producers + consumers
            var producers = new Task[ threadCount ];
            var consumers = new Task[ threadCount ];

            // Producers
            for ( int t = 0; t < threadCount; t++ ) {
                producers[ t ] = Task.Run( () => {
                    barrier.SignalAndWait();
                    for ( int i = 0; i < eventsPerThread; i++ ) {
                        queue.Enqueue( mockEvent, new TestArgs { Value = i }, EventPriority.Normal );
                        Interlocked.Increment( ref enqueuedCount );
                    }
                } );
            }

            // Consumers
            for ( int t = 0; t < threadCount; t++ ) {
                consumers[ t ] = Task.Run( () => {
                    barrier.SignalAndWait();
                    while ( true ) {
                        if ( queue.ProcessNext() ) {
                            Interlocked.Increment( ref processedCount );
                        } else {
                            // Stop when all events are processed
                            if ( Volatile.Read( ref processedCount ) >= totalEvents )
                                break;
                            Thread.SpinWait( 10 );
                        }
                    }
                } );
            }

            Task.WaitAll( producers );
            Task.WaitAll( consumers );

            using ( Assert.EnterMultipleScope() ) {
                Assert.That( processedCount, Is.EqualTo( totalEvents ) );
                Assert.That( mockEvent.PublishCount, Is.EqualTo( totalEvents ) );
                Assert.That( queue.Count, Is.Zero );
            }
        }

        [Test]
        public void ConcurrentClearAndEnqueue_DoesNotCorrupt() {
            var queue = new EventQueue();
            var mockEvent = new MockGameEvent<TestArgs>();
            const int iterations = 1000;

            var t1 = Task.Run( () => {
                for ( int i = 0; i < iterations; i++ )
                    queue.Enqueue( mockEvent, new TestArgs { Value = i }, EventPriority.Normal );
            } );

            var t2 = Task.Run( () => {
                for ( int i = 0; i < iterations; i++ )
                    queue.Clear();
            } );

            Task.WaitAll( t1, t2 );

            // Just ensure no exception was thrown – the queue should be usable
            queue.ProcessAll();
            Assert.Pass();
        }

        [Test]
        public void ProcessAll_SamePriority_FIFO() {
            var processedOrder = new List<int>();
            var mockEvent = CreateTrackingEvent<TestArgs>( "Test", ( args ) => processedOrder.Add( args.Value ) );

            // Enqueue three events with same priority
            _queue.Enqueue( mockEvent, new TestArgs { Value = 1 }, EventPriority.Normal );
            _queue.Enqueue( mockEvent, new TestArgs { Value = 2 }, EventPriority.Normal );
            _queue.Enqueue( mockEvent, new TestArgs { Value = 3 }, EventPriority.Normal );

            _queue.ProcessAll();

            Assert.That( processedOrder, Is.EqualTo( new[] { 1, 2, 3 } ) );
        }

        [Test]
        public void ProcessNext_ReturnsTrueAndProcessesHighestPriority() {
            var processed = new List<int>();
            var lowEvent = CreateTrackingEvent<TestArgs>( "Low", ( args ) => processed.Add( args.Value ) );
            var highEvent = CreateTrackingEvent<TestArgs>( "High", ( args ) => processed.Add( args.Value ) );

            _queue.Enqueue( lowEvent, new TestArgs { Value = 10 }, EventPriority.Low );
            _queue.Enqueue( highEvent, new TestArgs { Value = 20 }, EventPriority.High );

            bool result = _queue.ProcessNext();

            using ( Assert.EnterMultipleScope() ) {
                Assert.That( result, Is.True );
                Assert.That( processed, Is.EqualTo( new[] { 20 } ) ); // High priority processed first
                Assert.That( _queue.Count, Is.EqualTo( 1 ) ); // Low still remains
            }
        }

        [Test]
        public void ProcessNext_EmptyQueue_ReturnsFalse() {
            bool result = _queue.ProcessNext();
            Assert.That( result, Is.False );
        }

        [Test]
        public void ProcessNext_ProcessesOnlyOneEvent() {
            var processed = new List<int>();
            var mockEvent = CreateTrackingEvent<TestArgs>( "Test", ( args ) => processed.Add( args.Value ) );

            _queue.Enqueue( mockEvent, new TestArgs { Value = 1 }, EventPriority.Normal );
            _queue.Enqueue( mockEvent, new TestArgs { Value = 2 }, EventPriority.Normal );

            bool result1 = _queue.ProcessNext();
            bool result2 = _queue.ProcessNext();
            bool result3 = _queue.ProcessNext();

            using ( Assert.EnterMultipleScope() ) {
                Assert.That( result1, Is.True );
                Assert.That( result2, Is.True );
                Assert.That( result3, Is.False );
                Assert.That( processed, Is.EqualTo( new[] { 1, 2 } ) );
                Assert.That( _queue.Count, Is.Zero );
            }
        }

        [Test]
        public void Clear_RemovesAllEvents() {
            var mockEvent = new MockGameEvent<TestArgs>();
            _queue.Enqueue( mockEvent, new TestArgs { Value = 1 }, EventPriority.Normal );
            _queue.Enqueue( mockEvent, new TestArgs { Value = 2 }, EventPriority.High );
            _queue.Enqueue( mockEvent, new TestArgs { Value = 3 }, EventPriority.Low );

            Assert.That( _queue.Count, Is.EqualTo( 3 ) );

            _queue.Clear();

            Assert.That( _queue.Count, Is.Zero );

            // ProcessAll should do nothing
            _queue.ProcessAll();
            Assert.That( mockEvent.PublishCount, Is.Zero );
        }

        [Test]
        public void Clear_AfterPartialProcessing_StillClears() {
            var processed = new List<int>();
            var mockEvent = CreateTrackingEvent<TestArgs>( "Test", ( args ) => processed.Add( args.Value ) );

            _queue.Enqueue( mockEvent, new TestArgs { Value = 1 }, EventPriority.High );
            _queue.Enqueue( mockEvent, new TestArgs { Value = 2 }, EventPriority.Normal );
            _queue.Enqueue( mockEvent, new TestArgs { Value = 3 }, EventPriority.Low );

            _queue.ProcessNext(); // processes High (1)

            _queue.Clear();

            _queue.ProcessAll(); // should process nothing

            using ( Assert.EnterMultipleScope() ) {
                Assert.That( processed, Is.EqualTo( new[] { 1 } ) );
                Assert.That( _queue.Count, Is.Zero );
            }
        }

        [Test]
        public void Count_ReflectsQueuedItemsAcrossPriorities() {
            Assert.That( _queue.Count, Is.Zero );

            var mockEvent = new MockGameEvent<TestArgs>();
            _queue.Enqueue( mockEvent, new TestArgs(), EventPriority.VeryLow );
            Assert.That( _queue.Count, Is.EqualTo( 1 ) );

            _queue.Enqueue( mockEvent, new TestArgs(), EventPriority.VeryHigh );
            Assert.That( _queue.Count, Is.EqualTo( 2 ) );

            _queue.Enqueue( mockEvent, new TestArgs(), EventPriority.Critical );
            Assert.That( _queue.Count, Is.EqualTo( 3 ) );

            _queue.ProcessNext(); // removes Critical
            Assert.That( _queue.Count, Is.EqualTo( 2 ) );
        }

        private static MockGameEvent<T> CreateTrackingEvent<T>( string name, Action<T> onPublish ) where T : struct {
            return new MockGameEvent<T>( onPublish ) { DebugName = name };
        }
    }
}
