using System;
using NUnit.Framework;
using Nomad.Core.Input;
using Nomad.Input.Private.Services;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Loading")]
	[Category("Unit")]
	public class BindLoaderBranchTests {
		[Test]
		public void LoadBindDatabase_WhenFileIsMissing_ReturnsFalseAndEmptyBinds() {
			using var logger = new MockLogger();
			var fileSystem = new InputFileSystemFixture();
			var loader = new BindLoader( fileSystem.Object, logger );

			bool loaded = loader.LoadBindDatabase( "Missing.json", out var binds );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( loaded, Is.False );
				Assert.That( binds, Is.Empty );
			}
		}

		[Test]
		public void LoadBindDatabase_LoadsObjectPayloadWithFallbackNameAndLegacyButtonId() {
			using var logger = new MockLogger();
			var fileSystem = new InputFileSystemFixture(
				( "ObjectPayload.json", """
				{
				  "Bindings": {
				    "Jump": {
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Binding": { "DeviceId": "Keyboard", "ButtonId": "Space" }
				    }
				  }
				}
				""" ) );
			var loader = new BindLoader( fileSystem.Object, logger );

			bool loaded = loader.LoadBindDatabase( "ObjectPayload.json", out var binds );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( loaded, Is.True );
				Assert.That( binds.Length, Is.EqualTo( 1 ) );
				Assert.That( binds[0].Name, Is.EqualTo( "Jump" ) );
				Assert.That( binds[0].Id, Is.EqualTo( "Jump" ) );
				Assert.That( binds[0].Bindings[0].Button.ControlId, Is.EqualTo( InputControlId.Space ) );
			}
		}

		[Test]
		public void LoadBindDatabase_ParsesEveryBindingKindAndDeviceAlias() {
			using var logger = new MockLogger();
			var fileSystem = new InputFileSystemFixture(
				( "Kinds.json", """
				{
				  "Bindings": [
				    {
				      "Name": "AllKinds",
				      "Id": "AllKinds",
				      "ValueType": "Vector2",
				      "Scheme": "Gamepad",
				      "Bindings": [
				        { "Kind": "Button", "DeviceId": "Gamepad1", "ControlId": "A" },
				        { "Kind": "Axis1D", "DeviceId": "Gamepad2", "ControlId": "LeftTrigger", "Deadzone": 0.1, "Sensitivity": 2.0, "Scale": 3.0, "Invert": true, "ResponseCurve": "Cubic" },
				        { "Kind": "Axis2D", "DeviceId": "Gamepad3", "ControlId": "RightStick", "Deadzone": 0.2, "Sensitivity": 3.0, "ScaleX": 4.0, "ScaleY": 5.0, "InvertX": true, "InvertY": true },
				        { "Kind": "Delta2D", "DeviceId": "MouseMotion", "Sensitivity": 6.0, "ScaleX": 7.0, "ScaleY": 8.0, "InvertX": true, "InvertY": true },
				        { "Kind": "Axis1DComposite", "Negative": "S", "Positive": "W", "Scale": 2.0, "Normalize": false },
				        { "Kind": "Axis2DComposite", "Up": "W", "Down": "S", "Left": "A", "Right": "D", "ScaleX": 2.0, "ScaleY": 3.0, "Normalize": false }
				      ]
				    }
				  ]
				}
				""" ) );
			var loader = new BindLoader( fileSystem.Object, logger );

			bool loaded = loader.LoadBindDatabase( "Kinds.json", out var binds );
			var bindings = binds[0].Bindings;

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( loaded, Is.True );
				Assert.That( bindings[0].Button.DeviceId, Is.EqualTo( InputDeviceSlot.Gamepad1 ) );
				Assert.That( bindings[0].Button.ControlId, Is.EqualTo( InputControlId.GamepadA ) );
				Assert.That( bindings[1].Axis1D.DeviceId, Is.EqualTo( InputDeviceSlot.Gamepad2 ) );
				Assert.That( bindings[1].Axis1D.ResponseCurve, Is.EqualTo( ResponseCurve.Cubic ) );
				Assert.That( bindings[2].Axis2D.DeviceId, Is.EqualTo( InputDeviceSlot.Gamepad3 ) );
				Assert.That( bindings[2].Axis2D.InvertY, Is.True );
				Assert.That( bindings[3].Delta2D.ControlId, Is.EqualTo( InputControlId.Delta ) );
				Assert.That( bindings[4].Axis1DComposite.Normalize, Is.False );
				Assert.That( bindings[5].Axis2DComposite.ScaleY, Is.EqualTo( 3.0f ) );
			}
		}

		[Test]
		public void LoadBindDatabase_InvalidPayloadsThrowUsefulExceptions() {
			using var logger = new MockLogger();
			var fileSystem = new InputFileSystemFixture(
				( "MissingBindings.json", "{}" ),
				( "InvalidBindingsType.json", "{ \"Bindings\": 5 }" ),
				( "MissingBindingPayload.json", "{ \"Bindings\": [ { \"Name\": \"Jump\", \"Id\": \"Jump\", \"ValueType\": \"Button\", \"Scheme\": \"KeyboardAndMouse\" } ] }" ),
				( "InvalidModifiersType.json", "{ \"Bindings\": [ { \"Name\": \"Jump\", \"Id\": \"Jump\", \"ValueType\": \"Button\", \"Scheme\": \"KeyboardAndMouse\", \"Binding\": { \"DeviceId\": \"Keyboard\", \"ControlId\": \"Space\", \"Modifiers\": \"Shift\" } } ] }" ),
				( "InvalidModifierElement.json", "{ \"Bindings\": [ { \"Name\": \"Jump\", \"Id\": \"Jump\", \"ValueType\": \"Button\", \"Scheme\": \"KeyboardAndMouse\", \"Binding\": { \"DeviceId\": \"Keyboard\", \"ControlId\": \"Space\", \"Modifiers\": [1] } } ] }" ),
				( "InvalidDevice.json", "{ \"Bindings\": [ { \"Name\": \"Jump\", \"Id\": \"Jump\", \"ValueType\": \"Button\", \"Scheme\": \"KeyboardAndMouse\", \"Binding\": { \"DeviceId\": \"Toaster\", \"ControlId\": \"Space\" } } ] }" ),
				( "InvalidBindingKind.json", "{ \"Bindings\": [ { \"Name\": \"Jump\", \"Id\": \"Jump\", \"ValueType\": \"Button\", \"Scheme\": \"KeyboardAndMouse\", \"Binding\": { \"Kind\": \"Button\", \"DeviceId\": \"Keyboard\", \"ControlId\": \"Nope\" } } ] }" ),
				( "ConflictingValueType.json", "{ \"Bindings\": [ { \"Name\": \"A\", \"Id\": \"Same\", \"ValueType\": \"Button\", \"Scheme\": \"KeyboardAndMouse\", \"Binding\": { \"DeviceId\": \"Keyboard\", \"ControlId\": \"Space\" } }, { \"Name\": \"B\", \"Id\": \"Same\", \"ValueType\": \"Float\", \"Scheme\": \"KeyboardAndMouse\", \"Binding\": { \"DeviceId\": \"Keyboard\", \"ControlId\": \"Enter\" } } ] }" ) );
			var loader = new BindLoader( fileSystem.Object, logger );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( () => loader.LoadBindDatabase( "MissingBindings.json", out _ ), Throws.Exception );
				Assert.That( () => loader.LoadBindDatabase( "InvalidBindingsType.json", out _ ), Throws.Exception );
				Assert.That( () => loader.LoadBindDatabase( "MissingBindingPayload.json", out _ ), Throws.Exception );
				Assert.That( () => loader.LoadBindDatabase( "InvalidModifiersType.json", out _ ), Throws.Exception );
				Assert.That( () => loader.LoadBindDatabase( "InvalidModifierElement.json", out _ ), Throws.Exception );
				Assert.That( () => loader.LoadBindDatabase( "InvalidDevice.json", out _ ), Throws.Exception );
				Assert.That( () => loader.LoadBindDatabase( "InvalidBindingKind.json", out _ ), Throws.Exception );
				Assert.That( () => loader.LoadBindDatabase( "ConflictingValueType.json", out _ ), Throws.Exception );
			}
		}
	}
}
