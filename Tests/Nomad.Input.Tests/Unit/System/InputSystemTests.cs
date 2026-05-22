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

using System.Reflection;
using System.Numerics;
using NUnit.Framework;
using Nomad.Core.Input;
using Nomad.Events;
using Nomad.Input.Private.Services;
using Nomad.Input.ValueObjects;
using Nomad.Core.ServiceRegistry.Services;
using Nomad.Core.Input.ValueObjects;
using Nomad.Core.Engine.Services;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("System")]
	[Category("Unit")]
	public class InputSystemTests {
		private static readonly FieldInfo InputModeField = typeof( InputSystem ).GetField( "_mode", BindingFlags.Instance | BindingFlags.NonPublic )!;

		private GameEventRegistry _eventRegistry;
		private MockLogger _logger;
		private ServiceCollection _registry;

		[SetUp]
		public void SetUp() {
			_eventRegistry = InputTestHelpers.CreateEventRegistry( out _logger );
			_registry = new ServiceCollection();
		}

		[TearDown]
		public void TearDown() {
			_eventRegistry.Dispose();
			_logger.Dispose();
			_registry.Dispose();
		}

		[Test]
		public void PushKeyboardEvent_DispatchesConfiguredButtonAction() {
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": [
				    {
					  "Id": "Jump",
				      "Name": "Jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				""" )
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem( _eventRegistry, defaultsPath );
			ButtonActionEventArgs? published = null;
			_eventRegistry.GetEvent<ButtonActionEventArgs>( $"Jump:{ButtonActionEventArgs.Name}", ButtonActionEventArgs.NameSpace )
				.Subscribe( ( in ButtonActionEventArgs args ) => published = args );

			using var inputSystem = new InputSystem( fileSystem.Object, cvarSystem, _logger, _eventRegistry, _registry );
			inputSystem.PushKeyboardEvent( new KeyboardEventArgs( KeyNum.Space, 100, true ) );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( published.HasValue, Is.True );
				Assert.That( published!.Value.Value, Is.True );
				Assert.That( published.Value.Phase, Is.EqualTo( InputActionPhase.Started ) );
			}
		}

		[Test]
		public void EngineInputEvent_WhenPaused_DoesNotDispatchConfiguredAction() {
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": [
				    {
					  "Id": "Jump",
				      "Name": "Jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				""" )
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem( _eventRegistry, defaultsPath );
			int publishCount = 0;
			_eventRegistry.GetEvent<ButtonActionEventArgs>( $"Jump:{ButtonActionEventArgs.Name}", ButtonActionEventArgs.NameSpace )
				.Subscribe( ( in ButtonActionEventArgs args ) => publishCount++ );

			using var inputSystem = new InputSystem( fileSystem.Object, cvarSystem, _logger, _eventRegistry, _registry );
			_eventRegistry.GetEvent<PauseStateChangedEventArgs>( PauseStateChangedEventArgs.Name, PauseStateChangedEventArgs.NameSpace )
				.Publish( new PauseStateChangedEventArgs( true ) );
			_eventRegistry.GetEvent<KeyboardEventArgs>( KeyboardEventArgs.Name, KeyboardEventArgs.NameSpace )
				.Publish( new KeyboardEventArgs( KeyNum.Space, 100, true ) );

			Assert.That( publishCount, Is.Zero );
		}

		[Test]
		public void EngineInputEvent_AfterUnpausing_DispatchesConfiguredAction() {
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": [
				    {
					  "Id": "Jump",
				      "Name": "Jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				""" )
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem( _eventRegistry, defaultsPath );
			ButtonActionEventArgs? published = null;
			_eventRegistry.GetEvent<ButtonActionEventArgs>( $"Jump:{ButtonActionEventArgs.Name}", ButtonActionEventArgs.NameSpace )
				.Subscribe( ( in ButtonActionEventArgs args ) => published = args );

			using var inputSystem = new InputSystem( fileSystem.Object, cvarSystem, _logger, _eventRegistry, _registry );
			_eventRegistry.GetEvent<PauseStateChangedEventArgs>( PauseStateChangedEventArgs.Name, PauseStateChangedEventArgs.NameSpace )
				.Publish( new PauseStateChangedEventArgs( true ) );
			_eventRegistry.GetEvent<PauseStateChangedEventArgs>( PauseStateChangedEventArgs.Name, PauseStateChangedEventArgs.NameSpace )
				.Publish( new PauseStateChangedEventArgs( false ) );
			_eventRegistry.GetEvent<KeyboardEventArgs>( KeyboardEventArgs.Name, KeyboardEventArgs.NameSpace )
				.Publish( new KeyboardEventArgs( KeyNum.Space, 100, true ) );

			Assert.That( published.HasValue, Is.True );
		}

		[Test]
		public void PushMouseMotionEvent_DispatchesConfiguredDeltaAction() {
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
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
				        "ControlId": "Delta",
				        "Sensitivity": 2.0,
				        "InvertY": true
				      }
				    }
				  ]
				}
				""" )
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem( _eventRegistry, defaultsPath );
			AxisActionEventArgs? published = null;
			_eventRegistry.GetEvent<AxisActionEventArgs>( $"Look:{AxisActionEventArgs.Name}", AxisActionEventArgs.NameSpace )
				.Subscribe( ( in AxisActionEventArgs args ) => published = args );

			using var inputSystem = new InputSystem( fileSystem.Object, cvarSystem, _logger, _eventRegistry, _registry );
			inputSystem.PushMouseMotionEvent( new MouseMotionEventArgs( 200, 4, 3 ) );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( published.HasValue, Is.True );
				Assert.That( published!.Value.Value, Is.EqualTo( new Vector2( 8.0f, -6.0f ) ) );
			}
		}

		[Test]
		public void PushMousePositionChangedEvent_UpdatesRegisteredSnapshotService() {
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": []
				}
				""" )
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem( _eventRegistry, defaultsPath );

			using var inputSystem = new InputSystem( fileSystem.Object, cvarSystem, _logger, _eventRegistry, _registry );
			using var locator = new ServiceLocator( _registry );
			var snapshot = locator.GetService<Nomad.Input.Interfaces.IInputSnapshotService>();

			inputSystem.PushMousePositionChangedEvent( new MousePositionChangedEventArgs( 250, 320, 180 ) );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( snapshot.MousePosition, Is.EqualTo( new Vector2( 320.0f, 180.0f ) ) );
				Assert.That( snapshot.GetAxis2D( InputDeviceSlot.Mouse, InputControlId.Position ), Is.EqualTo( new Vector2( 320.0f, 180.0f ) ) );
			}
		}

		[Test]
		public void PushGamepadAxisEvent_DispatchesConfiguredStickAction() {
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
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
				""" )
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem( _eventRegistry, defaultsPath );
			AxisActionEventArgs? published = null;
			_eventRegistry.GetEvent<AxisActionEventArgs>( $"Aim:{AxisActionEventArgs.Name}", Constants.Events.NAMESPACE )
				.Subscribe( ( in AxisActionEventArgs args ) => published = args );

			using var inputSystem = new InputSystem( fileSystem.Object, cvarSystem, _logger, _eventRegistry, _registry );
			SetMode( inputSystem, InputScheme.Gamepad );
			inputSystem.PushGamepadAxisEvent( new GamepadAxisEventArgs( GamepadStick.Left, 300, 0, new Vector2( 0.25f, -0.5f ) ) );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( published.HasValue, Is.True );
				Assert.That( published!.Value.Value, Is.EqualTo( new Vector2( 0.25f, -0.5f ) ) );
			}
		}

		[Test]
		public void PushGamepadButtonEvent_DispatchesConfiguredGamepadButtonAction() {
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": [
				    {
					  "Id": "Confirm",
				      "Name": "Confirm",
				      "ValueType": "Button",
				      "Scheme": "Gamepad",
				      "Bindings": { "DeviceId": "Gamepad1", "ControlId": "A" }
				    }
				  ]
				}
				""" )
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem( _eventRegistry, defaultsPath );
			ButtonActionEventArgs? published = null;
			_eventRegistry.GetEvent<ButtonActionEventArgs>( $"Confirm:{ButtonActionEventArgs.Name}", Constants.Events.NAMESPACE )
				.Subscribe( ( in ButtonActionEventArgs args ) => published = args );

			using var inputSystem = new InputSystem( fileSystem.Object, cvarSystem, _logger, _eventRegistry, _registry );
			SetMode( inputSystem, InputScheme.Gamepad );
			inputSystem.PushGamepadButtonEvent( new GamepadButtonEventArgs( GamepadButton.A, 400, 1, true ) );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( published.HasValue, Is.True );
				Assert.That( published!.Value.Value, Is.True );
			}
		}

		[Test]
		public void PushKeyboardEvent_DispatchesCompositeMovementActions() {
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": [
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
				""" )
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem( _eventRegistry, defaultsPath );
			AxisActionEventArgs? published = null;
			_eventRegistry.GetEvent<AxisActionEventArgs>( $"Move:{AxisActionEventArgs.Name}", AxisActionEventArgs.NameSpace )
				.Subscribe( ( in AxisActionEventArgs args ) => published = args );

			using var inputSystem = new InputSystem( fileSystem.Object, cvarSystem, _logger, _eventRegistry, _registry );
			inputSystem.PushKeyboardEvent( new KeyboardEventArgs( KeyNum.W, 500, true ) );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( published.HasValue, Is.True );
				Assert.That( published!.Value.Value, Is.EqualTo( new Vector2( 0.0f, 1.0f ) ) );
			}
		}

		private static void SetMode( InputSystem inputSystem, InputScheme mode ) {
			InputModeField.SetValue( inputSystem, mode );
		}
	}
}
