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
	public class InputBindingDefinitionExtensionBranchTests {
		[Test]
		public void Clone_ForDefaultArrayReturnsDefaultAndForNullBindingThrows() {
			ImmutableArray<InputBindingDefinition> bindings = default;

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( bindings.Clone().IsDefault, Is.True );
				Assert.That( () => InputBindingDefinitionExtensions.Clone( (InputBindingDefinition)null ), Throws.TypeOf<System.NullReferenceException>() );
			}
		}

		[Test]
		public void Clone_CopiesEveryBindingKindWithoutSharingDefinitions() {
			var original = ImmutableArray.Create(
				InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space, InputControlId.Shift ),
				InputTestHelpers.Axis1D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, 0.1f, 2.0f, 3.0f, true ),
				InputTestHelpers.Composite1D( InputScheme.KeyboardAndMouse, InputControlId.S, InputControlId.W, 2.0f, false ),
				InputTestHelpers.Axis2D( InputScheme.Gamepad, InputDeviceSlot.Gamepad1, InputControlId.RightStick, 0.2f, 1.5f, 2.0f, 3.0f, true, true ),
				InputTestHelpers.Composite2D( InputScheme.KeyboardAndMouse, InputControlId.W, InputControlId.S, InputControlId.A, InputControlId.D, 2.0f, 3.0f, false ),
				InputTestHelpers.Delta2D( InputScheme.KeyboardAndMouse, InputDeviceSlot.Mouse, InputControlId.Delta, 4.0f, 5.0f, 6.0f, true, true ) );

			ImmutableArray<InputBindingDefinition> clone = original.Clone();
			clone[0].Button = new ButtonBinding( InputDeviceSlot.Keyboard, InputControlId.Enter );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( clone.Length, Is.EqualTo( original.Length ) );
				for ( int i = 1; i < original.Length; i++ ) {
					Assert.That( clone[i].ContentEquals( original[i] ), Is.True );
					Assert.That( ReferenceEquals( clone[i], original[i] ), Is.False );
				}
				Assert.That( original[0].Button.ControlId, Is.EqualTo( InputControlId.Space ) );
			}
		}

		[Test]
		public void ContentEquals_CoversReferenceNullKindAndFieldMismatches() {
			var button = InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space, InputControlId.Shift );
			var buttonDifferentModifier = InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space, InputControlId.Ctrl );
			var axis1D = InputTestHelpers.Axis1D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger );
			var axis1DDifferent = InputTestHelpers.Axis1D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.RightTrigger );
			var composite1D = InputTestHelpers.Composite1D( InputScheme.KeyboardAndMouse, InputControlId.S, InputControlId.W );
			var composite1DDifferent = InputTestHelpers.Composite1D( InputScheme.KeyboardAndMouse, InputControlId.A, InputControlId.W );
			var axis2D = InputTestHelpers.Axis2D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.LeftStick );
			var axis2DDifferent = InputTestHelpers.Axis2D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.RightStick );
			var composite2D = InputTestHelpers.Composite2D( InputScheme.KeyboardAndMouse, InputControlId.W, InputControlId.S, InputControlId.A, InputControlId.D );
			var composite2DDifferent = InputTestHelpers.Composite2D( InputScheme.KeyboardAndMouse, InputControlId.UpArrow, InputControlId.S, InputControlId.A, InputControlId.D );
			var delta = InputTestHelpers.Delta2D( InputScheme.KeyboardAndMouse, InputDeviceSlot.Mouse, InputControlId.Delta );
			var deltaDifferent = InputTestHelpers.Delta2D( InputScheme.KeyboardAndMouse, InputDeviceSlot.Mouse, InputControlId.Position );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( button.ContentEquals( button ), Is.True );
				Assert.That( button.ContentEquals( null ), Is.False );
				Assert.That( InputBindingDefinitionExtensions.ContentEquals( null, button ), Is.False );
				Assert.That( button.ContentEquals( InputTestHelpers.Button( InputScheme.Gamepad, InputDeviceSlot.Keyboard, InputControlId.Space ) ), Is.False );
				Assert.That( button.ContentEquals( axis1D ), Is.False );
				Assert.That( button.ContentEquals( buttonDifferentModifier ), Is.False );
				Assert.That( axis1D.ContentEquals( axis1DDifferent ), Is.False );
				Assert.That( composite1D.ContentEquals( composite1DDifferent ), Is.False );
				Assert.That( axis2D.ContentEquals( axis2DDifferent ), Is.False );
				Assert.That( composite2D.ContentEquals( composite2DDifferent ), Is.False );
				Assert.That( delta.ContentEquals( deltaDifferent ), Is.False );
				Assert.That( new InputBindingDefinition { Kind = (InputBindingKind)255 }.ContentEquals( new InputBindingDefinition { Kind = (InputBindingKind)255 } ), Is.False );
			}
		}
	}
}
