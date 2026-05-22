using System.Numerics;
using NUnit.Framework;
using Nomad.Input.Private.Services;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("State")]
	[Category("Unit")]
	public class InputStateServiceBranchTests {
		[Test]
		public void PressedBits_SetAndClearAcrossDevicesAndHighControlWords() {
			using var state = new InputStateService();

			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.Space, true );
			state.SetPressed( InputDeviceSlot.Gamepad3, (InputControlId)130, true );
			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.Space, false );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( state.IsPressed( InputDeviceSlot.Keyboard, InputControlId.Space ), Is.False );
				Assert.That( state.IsPressed( InputDeviceSlot.Gamepad3, (InputControlId)130 ), Is.True );
			}
		}

		[Test]
		public void AxisAndMouseState_RoundTripThroughSnapshotStorage() {
			using var state = new InputStateService();
			Vector2 axisValue = new( 0.25f, -0.75f );
			Vector2 mousePosition = new( 128.0f, 256.0f );
			Vector2 mouseDelta = new( -3.0f, 4.0f );

			state.SetAxis1D( InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, 0.5f );
			state.SetAxis2D( InputDeviceSlot.Gamepad0, InputControlId.LeftStick, axisValue );
			state.SetMousePosition( mousePosition );
			state.AddMouseDelta( mouseDelta );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( state.GetAxis1D( InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger ), Is.EqualTo( 0.5f ) );
				Assert.That( state.GetAxis2D( InputDeviceSlot.Gamepad0, InputControlId.LeftStick ), Is.EqualTo( axisValue ) );
				Assert.That( state.MousePosition, Is.EqualTo( mousePosition ) );
				Assert.That( state.GetAxis2D( InputDeviceSlot.Mouse, InputControlId.Position ), Is.EqualTo( mousePosition ) );
				Assert.That( state.MouseDelta, Is.EqualTo( mouseDelta ) );
				Assert.That( state.GetAxis2D( InputDeviceSlot.Mouse, InputControlId.Delta ), Is.EqualTo( mouseDelta ) );
			}
		}

		[Test]
		public void Dispose_CanBeCalledMoreThanOnce() {
			var state = new InputStateService();

			state.Dispose();

			Assert.DoesNotThrow( () => state.Dispose() );
		}
	}
}
