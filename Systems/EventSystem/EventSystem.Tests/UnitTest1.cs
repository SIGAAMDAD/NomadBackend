using NomadCore.Systems.EventSystem.Services;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.GameServices;
using NomadCore.Systems.EventSystem.Domain.Registries;
using System;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.Domain.Models;

namespace NomadCore.Systems.EventSystem.Tests {
	[TestFixture]
	public class Tests {
		private readonly record struct EventArgs(
			int Test1,
			bool Conditional
		) : IEventArgs;

		private readonly IGameEventRegistryService _eventRegistry = new GameEventRegistry( new MockLogger() );
		private IGameEvent<EmptyEventArgs> _event;

		[SetUp]
		public void Setup() {
			var eventBus = new GameEventBus();
			_event = _eventRegistry.GetEvent<EmptyEventArgs>( "TestEvent", null, EventFlags.Synchronous | EventFlags.NoLock );
			_event.Subscribe( this, OnEventTriggered );
			_event.Subscribe( this, OnEventTriggered2 );
			_event.Subscribe( this, OnEventTriggered3 );
		}

		[TearDown]
		public void TearDown() {
			_event.Dispose();
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

		private void OnEventTriggered( in EmptyEventArgs args ) {
		}
		private void OnEventTriggered2( in EmptyEventArgs args ) {
		}
		private void OnEventTriggered3( in EmptyEventArgs args ) {
		}
	}
};