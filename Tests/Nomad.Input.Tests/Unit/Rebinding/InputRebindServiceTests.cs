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

using System.Numerics;
using NUnit.Framework;
using Nomad.Core.Input;
using Nomad.Events;
using Nomad.Input.Private.Repositories;
using Nomad.Input.Private.Services;
using Nomad.Input.ValueObjects;
using Nomad.Core.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category( "Nomad.Input" )]
	[Category( "Rebinding" )]
	[Category( "Unit" )]
	public class InputRebindServiceTests {
		private const string DefaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
		private const string GameplayPath = "Assets/Config/Bindings/Gameplay.json";

		private GameEventRegistry _eventRegistry;
		private MockLogger _logger;

		[SetUp]
		public void SetUp() {
			_eventRegistry = InputTestHelpers.CreateEventRegistry( out _logger );
		}

		[TearDown]
		public void TearDown() {
			_eventRegistry.Dispose();
			_logger.Dispose();
		}

		[Test]
		public void BeginRebind_StartsSupportedRequestAndRaisesStartedEvent() {
			var (repository, service, _) = CreateService(
				"""
				{
				  "Bindings": []
				}
				""",
				"""
				{
				  "Bindings": [
				    {
				      "Name": "Jump",
				      "Id": "player.jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space", "Modifiers": ["Shift"] }
				    }
				  ]
				}
				"""
			);
			using ( repository )
			using ( service ) {
				InputRebindRequest? started = null;
				var request = new InputRebindRequest( "Gameplay", "player.jump", 0 );
				service.RebindStarted += value => started = value;

				bool begun = service.BeginRebind( request );

				using ( Assert.EnterMultipleScope() ) {
					Assert.That( begun, Is.True );
					Assert.That( service.IsRebinding, Is.True );
					Assert.That( service.CurrentRequest, Is.EqualTo( request ) );
					Assert.That( started, Is.EqualTo( request ) );
				}
			}
		}

		[Test]
		public void BeginRebind_WhenPartIsUnsupportedOrAlreadyRunning_ReturnsFalse() {
			var (repository, service, _) = CreateService(
				"{ \"Bindings\": [] }",
				"""
				{
				  "Bindings": [
				    {
				      "Name": "Jump",
				      "Id": "player.jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				"""
			);
			using ( repository )
			using ( service ) {
				bool unsupported = service.BeginRebind( new InputRebindRequest( "Gameplay", "player.jump", 0, InputRebindPart.Up ) );
				bool started = service.BeginRebind( new InputRebindRequest( "Gameplay", "player.jump", 0 ) );
				bool duplicate = service.BeginRebind( new InputRebindRequest( "Gameplay", "player.jump", 0 ) );

				using ( Assert.EnterMultipleScope() ) {
					Assert.That( unsupported, Is.False );
					Assert.That( started, Is.True );
					Assert.That( duplicate, Is.False );
				}
			}
		}

		[Test]
		public void CancelRebind_ClearsCurrentRequestAndRaisesCanceledEvent() {
			var (repository, service, _) = CreateService(
				"{ \"Bindings\": [] }",
				"""
				{
				  "Bindings": [
				    {
				      "Name": "Jump",
				      "Id": "player.jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				"""
			);
			using ( repository )
			using ( service ) {
				InputRebindRequest? canceled = null;
				var request = new InputRebindRequest( "Gameplay", "player.jump", 0 );
				service.RebindCanceled += value => canceled = value;
				service.BeginRebind( request );

				bool result = service.CancelRebind();

				using ( Assert.EnterMultipleScope() ) {
					Assert.That( result, Is.True );
					Assert.That( service.IsRebinding, Is.False );
					Assert.That( service.CurrentRequest, Is.Null );
					Assert.That( canceled, Is.EqualTo( request ) );
				}
			}
		}

		[Test]
		public void ApplyBinding_UpdatesRepositoryCompilesGraphAndRaisesCompletedEvent() {
			var (repository, service, compiledRepository) = CreateService(
				"{ \"Bindings\": [] }",
				"""
				{
				  "Bindings": [
				    {
				      "Name": "Jump",
				      "Id": "player.jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				"""
			);
			using ( repository )
			using ( service ) {
				InputRebindResult? completed = null;
				var request = new InputRebindRequest( "Gameplay", "player.jump", 0 );
				var replacement = InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Enter );
				service.RebindCompleted += value => completed = value;
				service.BeginRebind( request );

				bool applied = service.ApplyBinding( request, replacement );

				using ( Assert.EnterMultipleScope() ) {
					Assert.That( applied, Is.True );
					Assert.That( repository.GetBindMappings()[ "Gameplay" ][ 0 ].Bindings[ 0 ].Button.ControlId, Is.EqualTo( InputControlId.Enter ) );
					Assert.That( CompiledBindingRepository.GetButtonCandidateIndices( compiledRepository.Current, InputDeviceSlot.Keyboard, InputControlId.Enter, true ).Length, Is.EqualTo( 1 ) );
					Assert.That( service.IsRebinding, Is.False );
					Assert.That( completed.HasValue, Is.True );
					Assert.That( completed!.Value.Binding.Button.ControlId, Is.EqualTo( InputControlId.Enter ) );
				}
			}
		}

		[Test]
		public void KeyboardEvent_RebindsButtonsAndClearsModifiers() {
			var (repository, service, _) = CreateService(
				"{ \"Bindings\": [] }",
				"""
				{
				  "Bindings": [
				    {
				      "Name": "Jump",
				      "Id": "player.jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space", "Modifiers": ["Shift"] }
				    }
				  ]
				}
				"""
			);
			using ( repository )
			using ( service ) {
				service.BeginRebind( new InputRebindRequest( "Gameplay", "player.jump", 0 ) );

				_eventRegistry.GetEvent<KeyboardEventArgs>( KeyboardEventArgs.Name, KeyboardEventArgs.NameSpace )
					.Publish( new KeyboardEventArgs( KeyNum.Enter, 100, true ) );

				var rebound = repository.GetBindMappings()[ "Gameplay" ][ 0 ].Bindings[ 0 ];

				using ( Assert.EnterMultipleScope() ) {
					Assert.That( rebound.Button.ControlId, Is.EqualTo( InputControlId.Enter ) );
					Assert.That( rebound.Button.DeviceId, Is.EqualTo( InputDeviceSlot.Keyboard ) );
					Assert.That( rebound.Button.Modifiers, Is.Empty );
				}
			}
		}

		[Test]
		public void MouseMotionEvent_RebindsDeltaBindings() {
			var (repository, service, _) = CreateService(
				"{ \"Bindings\": [] }",
				"""
				{
				  "Bindings": [
				    {
					  "Id": "Look",
				      "Name": "Look",
				      "ValueType": "Vector2",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": {
				        "Kind": "Delta2D",
				        "DeviceId": "Mouse",
				        "ControlId": "Delta"
				      }
				    }
				  ]
				}
				"""
			);
			using ( repository )
			using ( service ) {
				service.BeginRebind( new InputRebindRequest( "Gameplay", "Look", 0 ) );

				_eventRegistry.GetEvent<MouseMotionEventArgs>( MouseMotionEventArgs.Name, MouseMotionEventArgs.NameSpace )
					.Publish( new MouseMotionEventArgs( 100, 12, -4 ) );

				var rebound = repository.GetBindMappings()[ "Gameplay" ][ 0 ].Bindings[ 0 ];

				using ( Assert.EnterMultipleScope() ) {
					Assert.That( rebound.Delta2D.DeviceId, Is.EqualTo( InputDeviceSlot.Mouse ) );
					Assert.That( rebound.Delta2D.ControlId, Is.EqualTo( InputControlId.Delta ) );
				}
			}
		}

		[Test]
		public void GamepadAxisEvent_RebindsAxis2DBindingsWhenPastThreshold() {
			var (repository, service, _) = CreateService(
				"{ \"Bindings\": [] }",
				"""
				{
				  "Bindings": [
				    {
					  "Id": "Aim",
				      "Name": "Aim",
				      "ValueType": "Vector2",
				      "Scheme": "Gamepad",
				      "Bindings": {
				        "Kind": "Axis2D",
				        "DeviceId": "Gamepad",
				        "ControlId": "LeftStick"
				      }
				    }
				  ]
				}
				"""
			);
			using ( repository )
			using ( service ) {
				service.BeginRebind( new InputRebindRequest( "Gameplay", "Aim", 0 ) );

				_eventRegistry.GetEvent<GamepadAxisEventArgs>( GamepadAxisEventArgs.Name, GamepadAxisEventArgs.NameSpace )
					.Publish( new GamepadAxisEventArgs( GamepadStick.Right, 100, 1, new Vector2( 0.6f, 0.0f ) ) );

				var rebound = repository.GetBindMappings()[ "Gameplay" ][ 0 ].Bindings[ 0 ];

				using ( Assert.EnterMultipleScope() ) {
					Assert.That( rebound.Axis2D.DeviceId, Is.EqualTo( InputDeviceSlot.Gamepad1 ) );
					Assert.That( rebound.Axis2D.ControlId, Is.EqualTo( InputControlId.RightStick ) );
				}
			}
		}

		[Test]
		public void KeyboardEvent_RebindsCompositeParts() {
			var (repository, service, _) = CreateService(
				"{ \"Bindings\": [] }",
				"""
				{
				  "Bindings": [
				    {
					  "Id": "Throttle",
				      "Name": "Throttle",
				      "ValueType": "Float",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": {
				        "Kind": "Axis1DComposite",
				        "Negative": "S",
				        "Positive": "W"
				      }
				    },
				    {
					  "Id": "Move",
				      "Name": "Move",
				      "ValueType": "Vector2",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": {
				        "Kind": "Axis2DComposite",
				        "Up": "W",
				        "Down": "S",
				        "Left": "A",
				        "Right": "D"
				      }
				    }
				  ]
				}
				"""
			);
			using ( repository )
			using ( service ) {
				service.BeginRebind( new InputRebindRequest( "Gameplay", "Throttle", 0, InputRebindPart.Negative ) );
				_eventRegistry.GetEvent<KeyboardEventArgs>( KeyboardEventArgs.Name, KeyboardEventArgs.NameSpace )
					.Publish( new KeyboardEventArgs( KeyNum.Q, 10, true ) );

				service.BeginRebind( new InputRebindRequest( "Gameplay", "Move", 0, InputRebindPart.Right ) );
				_eventRegistry.GetEvent<KeyboardEventArgs>( KeyboardEventArgs.Name, KeyboardEventArgs.NameSpace )
					.Publish( new KeyboardEventArgs( KeyNum.L, 11, true ) );

				using ( Assert.EnterMultipleScope() ) {
					Assert.That( repository.GetBindMappings()[ "Gameplay" ][ 0 ].Bindings[ 0 ].Axis1DComposite.Negative, Is.EqualTo( InputControlId.Q ) );
					Assert.That( repository.GetBindMappings()[ "Gameplay" ][ 1 ].Bindings[ 0 ].Axis2DComposite.Right, Is.EqualTo( InputControlId.L ) );
				}
			}
		}

		[Test]
		public void ApplyBinding_WhenRequestDoesNotResolve_ReturnsFalse() {
			var (repository, service, _) = CreateService(
				"{ \"Bindings\": [] }",
				"""
				{
				  "Bindings": [
				    {
				      "Name": "Jump",
				      "Id": "player.jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				"""
			);
			using ( repository )
			using ( service ) {
				var binding = InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Enter );

				using ( Assert.EnterMultipleScope() ) {
					Assert.That( service.ApplyBinding( new InputRebindRequest( "Missing", "player.jump", 0 ), binding ), Is.False );
					Assert.That( service.ApplyBinding( new InputRebindRequest( "Gameplay", "player.jump", 1 ), binding ), Is.False );
					Assert.That( service.ApplyBinding( new InputRebindRequest( "Gameplay", "Missing", 0 ), binding ), Is.False );
				}
			}
		}

		private (BindRepository Repository, InputRebindService Service, CompiledBindingRepository CompiledRepository) CreateService( string defaultsJson, string gameplayJson ) {
			var fileSystem = new InputFileSystemFixture(
				(DefaultsPath, defaultsJson),
				(GameplayPath, gameplayJson)
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem( _eventRegistry, DefaultsPath );
			var repository = new BindRepository( fileSystem.Object, cvarSystem, _logger );
			repository.SetActiveMapping( InputScheme.KeyboardAndMouse, "Gameplay" );
			repository.SetActiveMapping( InputScheme.Gamepad, "Gameplay" );
			var compiledRepository = new CompiledBindingRepository();
			var compiler = new BindingCompilerService( compiledRepository );
			compiler.CompileIntoRepository( repository.GetAllBindings() );
			var service = new InputRebindService( repository, compiler, _eventRegistry );
			return (repository, service, compiledRepository);
		}
	}
}
