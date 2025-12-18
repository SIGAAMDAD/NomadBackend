using NomadCore.Systems.EventSystem.Services;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.GameServices;
using NomadCore.Systems.EventSystem.Domain.Registries;
using System;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.Domain.Models;
using NomadCore.Infrastructure.Collections;
using NUnit.Framework.Internal.Execution;

namespace NomadCore.Systems.EventSystem.Tests {
	[TestFixture]
	public class Tests {
		private readonly record struct EventArgs(
			int Test1,
			bool Conditional
		) : IEventArgs;

		private readonly IGameEventRegistryService _eventRegistry = new GameEventRegistry( new MockLogger() );
		private IGameEvent<EmptyEventArgs> _event;
		private IGameEvent<EventArgs> _typedEvent;

		[SetUp]
		public void Setup() {
			var eventBus = new GameEventBus();
			_event = _eventRegistry.GetEvent<EmptyEventArgs>( StringPool.Intern( "TestEvent" ), EventFlags.Synchronous | EventFlags.NoLock );
			_event.Subscribe( this, OnEventTriggered );
			_event.Subscribe( this, OnEventTriggered2 );
			_event.Subscribe( this, OnEventTriggered3 );

			_typedEvent = _eventRegistry.GetEvent<EventArgs>( StringPool.Intern( "MemoryEvent" ), EventFlags.Synchronous | EventFlags.Asynchronous );
			_typedEvent.Subscribe( this, OnMemoryEventTriggered );
		}

		[TearDown]
		public void TearDown() {
			_event.Dispose();
			_typedEvent.Dispose();
		}

		[Test]
		public void Test_EventPublishingSpeed() {
			var start = DateTime.UtcNow;
			for ( int i = 0; i < 1000; i++ ) {
				_event.Publish( in EmptyEventArgs.Args );
			}
			var end = DateTime.UtcNow;
			var elapsed = end - start;

			Console.WriteLine( $"Publishing 1000 events with EmptyEventArgs took {elapsed.Nanoseconds} ns" );
		}

		[Test]
		public void Test_EventMemoryUsageVariableInstanced() {
			long current = GC.GetTotalMemory( true );
			for ( int i = 0; i < 1000; i++ ) {
				var eventArgs = new EventArgs( 0, true );
				_typedEvent.Publish( in eventArgs );
			}
			long after = GC.GetTotalMemory( true );

			Console.WriteLine( $"Published 1000 events with EventArgs and used { after - current } bytes (variable instanced)" );
		}

		[Test]
		public void Test_EventMemoryUsageRAII() {
			long current = GC.GetTotalMemory( true );
			for ( int i = 0; i < 1000; i++ ) {
				_typedEvent.Publish( new EventArgs( 0, true ) );
			}
			long after = GC.GetTotalMemory( true );

			Console.WriteLine( $"Published 1000 events with EventArgs and used { after - current } bytes (RAII)" );
		}

		private void OnMemoryEventTriggered( in EventArgs args ) {
		}

		private void OnEventTriggered( in EmptyEventArgs args ) {
		}
		private void OnEventTriggered2( in EmptyEventArgs args ) {
		}
		private void OnEventTriggered3( in EmptyEventArgs args ) {
		}
	}
};