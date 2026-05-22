using System.Collections.Immutable;
using NUnit.Framework;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Bindings")]
	[Category("Unit")]
	public class InputBindingValueObjectTests {
		[Test]
		public void ButtonBinding_DefaultModifiersBecomeEmptyAndMasksAreZero() {
			var binding = new ButtonBinding( InputDeviceSlot.Keyboard, InputControlId.Space, default );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( binding.Modifiers.IsDefault, Is.False );
				Assert.That( binding.Modifiers, Is.Empty );
				Assert.That( binding.ModifierCount, Is.Zero );
				Assert.That( binding.ModifierMask0, Is.Zero );
				Assert.That( binding.ModifierMask1, Is.Zero );
				Assert.That( binding.ModifierMask2, Is.Zero );
				Assert.That( binding.ModifierMask3, Is.Zero );
			}
		}

		[Test]
		public void ButtonBinding_BuildsModifierMasksForConfiguredModifiers() {
			var binding = new ButtonBinding(
				InputDeviceSlot.Keyboard,
				InputControlId.Space,
				ImmutableArray.Create( InputControlId.Ctrl, InputControlId.Shift )
			);

			ulong expectedMask = (1UL << ((int)InputControlId.Ctrl & 63))
				| (1UL << ((int)InputControlId.Shift & 63));

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( binding.ModifierCount, Is.EqualTo( 2 ) );
				Assert.That( binding.ModifierMask0, Is.EqualTo( expectedMask ) );
				Assert.That( binding.ModifierMask1, Is.Zero );
				Assert.That( binding.ModifierMask2, Is.Zero );
				Assert.That( binding.ModifierMask3, Is.Zero );
			}
		}

		[Test]
		public void AxisAndDeltaConstructors_StoreProcessorSettings() {
			var axis1D = new Axis1DBinding( InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, 0.1f, 2.0f, 0.5f, true, ResponseCurve.Squared );
			var axis2D = new Axis2DBinding( InputDeviceSlot.Gamepad1, InputControlId.RightStick, 0.2f, 1.5f, 2.0f, 0.25f, true, true, ResponseCurve.Cubic );
			var delta = new Delta2DBinding( InputDeviceSlot.Mouse, InputControlId.Delta, 3.0f, 0.5f, 2.0f, true, true );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( axis1D.DeviceId, Is.EqualTo( InputDeviceSlot.Gamepad0 ) );
				Assert.That( axis1D.ControlId, Is.EqualTo( InputControlId.LeftTrigger ) );
				Assert.That( axis1D.Deadzone, Is.EqualTo( 0.1f ) );
				Assert.That( axis1D.Sensitivity, Is.EqualTo( 2.0f ) );
				Assert.That( axis1D.Scale, Is.EqualTo( 0.5f ) );
				Assert.That( axis1D.Invert, Is.True );
				Assert.That( axis1D.ResponseCurve, Is.EqualTo( ResponseCurve.Squared ) );

				Assert.That( axis2D.DeviceId, Is.EqualTo( InputDeviceSlot.Gamepad1 ) );
				Assert.That( axis2D.ControlId, Is.EqualTo( InputControlId.RightStick ) );
				Assert.That( axis2D.Deadzone, Is.EqualTo( 0.2f ) );
				Assert.That( axis2D.Sensitivity, Is.EqualTo( 1.5f ) );
				Assert.That( axis2D.ScaleX, Is.EqualTo( 2.0f ) );
				Assert.That( axis2D.ScaleY, Is.EqualTo( 0.25f ) );
				Assert.That( axis2D.InvertX, Is.True );
				Assert.That( axis2D.InvertY, Is.True );
				Assert.That( axis2D.ResponseCurve, Is.EqualTo( ResponseCurve.Cubic ) );

				Assert.That( delta.DeviceId, Is.EqualTo( InputDeviceSlot.Mouse ) );
				Assert.That( delta.ControlId, Is.EqualTo( InputControlId.Delta ) );
				Assert.That( delta.Sensitivity, Is.EqualTo( 3.0f ) );
				Assert.That( delta.ScaleX, Is.EqualTo( 0.5f ) );
				Assert.That( delta.ScaleY, Is.EqualTo( 2.0f ) );
				Assert.That( delta.InvertX, Is.True );
				Assert.That( delta.InvertY, Is.True );
			}
		}

		[Test]
		public void CompositeConstructors_StoreControlsAndNormalizationSettings() {
			var axis1D = new Axis1DCompositeBinding( InputControlId.S, InputControlId.W, 2.0f, false );
			var axis2D = new Axis2DCompositeBinding( InputControlId.W, InputControlId.S, InputControlId.A, InputControlId.D, false, 2.0f, 3.0f );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( axis1D.Negative, Is.EqualTo( InputControlId.S ) );
				Assert.That( axis1D.Positive, Is.EqualTo( InputControlId.W ) );
				Assert.That( axis1D.Scale, Is.EqualTo( 2.0f ) );
				Assert.That( axis1D.Normalize, Is.False );

				Assert.That( axis2D.Up, Is.EqualTo( InputControlId.W ) );
				Assert.That( axis2D.Down, Is.EqualTo( InputControlId.S ) );
				Assert.That( axis2D.Left, Is.EqualTo( InputControlId.A ) );
				Assert.That( axis2D.Right, Is.EqualTo( InputControlId.D ) );
				Assert.That( axis2D.Normalize, Is.False );
				Assert.That( axis2D.ScaleX, Is.EqualTo( 2.0f ) );
				Assert.That( axis2D.ScaleY, Is.EqualTo( 3.0f ) );
			}
		}
	}
}
