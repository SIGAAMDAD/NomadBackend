using System.Collections.Immutable;
using NUnit.Framework;
using Nomad.Core.Input;
using Nomad.Input.Extensions;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Bindings")]
	[Category("Unit")]
	public class InputBindingDefinitionExtensionTests {
		[Test]
		public void Clone_DefaultImmutableArray_ReturnsDefaultImmutableArray() {
			ImmutableArray<InputBindingDefinition> bindings = default;

			ImmutableArray<InputBindingDefinition> clone = bindings.Clone();

			Assert.That( clone.IsDefault, Is.True );
		}

		[Test]
		public void Clone_CreatesIndependentBindingDefinitions() {
			var original = ImmutableArray.Create(
				InputTestHelpers.Button(
					InputScheme.KeyboardAndMouse,
					InputDeviceSlot.Keyboard,
					InputControlId.Space,
					InputControlId.Ctrl
				),
				InputTestHelpers.Axis2D(
					InputScheme.Gamepad,
					InputDeviceSlot.Gamepad0,
					InputControlId.LeftStick,
					deadzone: 0.2f,
					sensitivity: 1.5f,
					scaleX: 2.0f,
					scaleY: 0.5f,
					invertX: true,
					invertY: true
				)
			);

			ImmutableArray<InputBindingDefinition> clone = original.Clone();
			clone[0].Button = new ButtonBinding( InputDeviceSlot.Keyboard, InputControlId.Enter );
			clone[1].Axis2D.ScaleX = 3.0f;

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( clone, Has.Length.EqualTo( original.Length ) );
				Assert.That( ReferenceEquals( original[0], clone[0] ), Is.False );
				Assert.That( original[0].Button.ControlId, Is.EqualTo( InputControlId.Space ) );
				Assert.That( original[1].Axis2D.ScaleX, Is.EqualTo( 2.0f ) );
			}
		}

		[Test]
		public void ContentEquals_ReturnsTrueForEquivalentBindingsAcrossAllKinds() {
			var bindings = new[] {
				InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space, InputControlId.Ctrl ),
				InputTestHelpers.Axis1D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, deadzone: 0.1f, sensitivity: 2.0f, scale: 0.5f, invert: true ),
				InputTestHelpers.Composite1D( InputScheme.KeyboardAndMouse, InputControlId.S, InputControlId.W, scale: 2.0f, normalize: false ),
				InputTestHelpers.Axis2D( InputScheme.Gamepad, InputDeviceSlot.Gamepad1, InputControlId.RightStick, deadzone: 0.2f, sensitivity: 1.5f, scaleX: 0.5f, scaleY: 2.0f, invertX: true, invertY: true ),
				InputTestHelpers.Composite2D( InputScheme.KeyboardAndMouse, InputControlId.W, InputControlId.S, InputControlId.A, InputControlId.D, scaleX: 2.0f, scaleY: 3.0f, normalize: false ),
				InputTestHelpers.Delta2D( InputScheme.KeyboardAndMouse, InputDeviceSlot.Mouse, InputControlId.Delta, sensitivity: 1.25f, scaleX: 2.0f, scaleY: 0.25f, invertX: true, invertY: true )
			};

			using ( Assert.EnterMultipleScope() ) {
				foreach ( InputBindingDefinition binding in bindings ) {
					Assert.That( binding.ContentEquals( binding.Clone() ), Is.True );
				}
			}
		}

		[Test]
		public void ContentEquals_DetectsDifferentSchemesKindsAndPayloads() {
			var button = InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space, InputControlId.Ctrl );
			var sameControlDifferentModifier = InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space, InputControlId.Shift );
			var samePayloadDifferentScheme = InputTestHelpers.Button( InputScheme.Gamepad, InputDeviceSlot.Keyboard, InputControlId.Space, InputControlId.Ctrl );
			var differentKind = InputTestHelpers.Axis1D( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( button.ContentEquals( sameControlDifferentModifier ), Is.False );
				Assert.That( button.ContentEquals( samePayloadDifferentScheme ), Is.False );
				Assert.That( button.ContentEquals( differentKind ), Is.False );
				Assert.That( button.ContentEquals( null! ), Is.False );
			}
		}
	}
}
