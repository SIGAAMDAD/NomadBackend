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
using System.Numerics;
using NUnit.Framework;
using Nomad.Core.Util;
using Nomad.Events;
using Nomad.Input.Private.Services;
using Nomad.Input.Private.ValueObjects;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Dispatch")]
	[Category("Unit")]
	public class InputDispatchServiceTests {
		private GameEventRegistry _eventRegistry;
		private MockLogger _logger;
		private CompiledBindingGraph _graph;

		[SetUp]
		public void SetUp() {
			_eventRegistry = InputTestHelpers.CreateEventRegistry( out _logger );
			_graph = new CompiledBindingGraph(
				Array.Empty<CompiledBinding>(),
				new[] {
					new CompiledActionInfo( new InternString( "player.jump" ) ),
					new CompiledActionInfo( new InternString( "Throttle" ) ),
					new CompiledActionInfo( new InternString( "Look" ) )
				},
				Array.Empty<Bucket>(),
				Array.Empty<int>(),
				Array.Empty<Bucket>(),
				Array.Empty<int>(),
				Array.Empty<Bucket>(),
				Array.Empty<int>(),
				Array.Empty<int>(),
				Array.Empty<int>()
			);
		}

		[TearDown]
		public void TearDown() {
			_eventRegistry.Dispose();
			_logger.Dispose();
		}

		[Test]
		public void Dispatch_PublishesButtonActionsToTheButtonEvent() {
			var dispatcher = new InputDispatchService( _eventRegistry );
			ButtonActionEventArgs? published = null;
			var gameEvent = _eventRegistry.GetEvent<ButtonActionEventArgs>( $"player.jump:{ButtonActionEventArgs.Name}", ButtonActionEventArgs.NameSpace );
			gameEvent.Subscribe( ( in ButtonActionEventArgs args ) => published = args );

			dispatcher.Dispatch( _graph, new ResolvedAction( new InternString( "player.jump" ), 0, InputValueType.Button, InputActionPhase.Started, 10, buttonValue: true ) );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( published.HasValue, Is.True );
				Assert.That( published!.Value.ActionId.ToString(), Is.EqualTo( "player.jump" ) );
				Assert.That( published.Value.Value, Is.True );
			}
		}

		[Test]
		public void Dispatch_PublishesFloatActionsToTheFloatEvent() {
			var dispatcher = new InputDispatchService( _eventRegistry );
			FloatActionEventArgs? published = null;
			var gameEvent = _eventRegistry.GetEvent<FloatActionEventArgs>( $"Throttle:{FloatActionEventArgs.Name}", FloatActionEventArgs.NameSpace );
			gameEvent.Subscribe( ( in FloatActionEventArgs args ) => published = args );

			dispatcher.Dispatch( _graph, new ResolvedAction( new InternString( "Throttle" ), 1, InputValueType.Float, InputActionPhase.Performed, 20, floatValue: 0.75f ) );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( published.HasValue, Is.True );
				Assert.That( published!.Value.Value, Is.EqualTo( 0.75f ).Within( 0.0001f ) );
			}
		}

		[Test]
		public void Dispatch_PublishesVector2ActionsToTheAxisEvent() {
			var dispatcher = new InputDispatchService( _eventRegistry );
			AxisActionEventArgs? published = null;
			var gameEvent = _eventRegistry.GetEvent<AxisActionEventArgs>( $"Look:{AxisActionEventArgs.Name}", AxisActionEventArgs.NameSpace );
			gameEvent.Subscribe( ( in AxisActionEventArgs args ) => published = args );

			dispatcher.Dispatch( _graph, new ResolvedAction( new InternString( "Look" ), 2, InputValueType.Vector2, InputActionPhase.Performed, 30, vector2Value: new Vector2( 4.0f, -2.0f ) ) );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( published.HasValue, Is.True );
				Assert.That( published!.Value.Value, Is.EqualTo( new Vector2( 4.0f, -2.0f ) ) );
			}
		}
	}
}
