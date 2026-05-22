using System.Numerics;
using NUnit.Framework;
using Nomad.Input.Private.Services;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("State")]
	[Category("Unit")]
	public class InputStateServiceTests {
		[Test]
		public void SetPressed_TracksPressedStatePerDeviceAndControl() {
			using var state = new InputStateService();

			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.Space, true );

			Assert.That( state.IsPressed( InputDeviceSlot.Keyboard, InputControlId.Space ), Is.True );
		}

		[Test]
		public void SetPressed_FalseClearsPreviouslyPressedControl() {
			using var state = new InputStateService();

			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.Space, true );
			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.Space, false );

			Assert.That( state.IsPressed( InputDeviceSlot.Keyboard, InputControlId.Space ), Is.False );
		}

		[Test]
		public void GetAxis1D_ReturnsZeroForUnsetControls() {
			using var state = new InputStateService();

			float value = state.GetAxis1D( InputDeviceSlot.Keyboard, InputControlId.Space );

			Assert.That( value, Is.EqualTo( 0.0f ) );
		}

		[Test]
		public void SetAxis1D_StoresTriggerValues() {
			using var state = new InputStateService();

			state.SetAxis1D( InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, 0.75f );

			Assert.That( state.GetAxis1D( InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger ), Is.EqualTo( 0.75f ) );
		}

		[Test]
		public void GetAxis2D_ReturnsZeroForUnsetControls() {
			using var state = new InputStateService();

			Vector2 value = state.GetAxis2D( InputDeviceSlot.Gamepad0, InputControlId.LeftStick );

			Assert.That( value, Is.EqualTo( Vector2.Zero ) );
		}

		[Test]
		public void SetAxis2D_StoresStickValues() {
			using var state = new InputStateService();
			Vector2 expected = new( 0.25f, -0.75f );

			state.SetAxis2D( InputDeviceSlot.Gamepad0, InputControlId.LeftStick, expected );

			Assert.That( state.GetAxis2D( InputDeviceSlot.Gamepad0, InputControlId.LeftStick ), Is.EqualTo( expected ) );
		}

		[Test]
		public void SetMousePosition_UpdatesMousePositionAndSnapshotAxis() {
			using var state = new InputStateService();
			Vector2 position = new( 320.0f, 180.0f );

			state.SetMousePosition( position );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( state.MousePosition, Is.EqualTo( position ) );
				Assert.That( state.GetAxis2D( InputDeviceSlot.Mouse, InputControlId.Position ), Is.EqualTo( position ) );
			}
		}

		[Test]
		public void AddMouseDelta_UpdatesMouseDeltaAndSnapshotAxis() {
			using var state = new InputStateService();
			Vector2 delta = new( 5.0f, -3.0f );

			state.AddMouseDelta( delta );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( state.MouseDelta, Is.EqualTo( delta ) );
				Assert.That( state.GetAxis2D( InputDeviceSlot.Mouse, InputControlId.Delta ), Is.EqualTo( delta ) );
			}
		}

		[Test]
		public void AddMouseDelta_ReplacesPreviousDeltaInsteadOfAccumulating() {
			using var state = new InputStateService();

			state.AddMouseDelta( new Vector2( 5.0f, -3.0f ) );
			state.AddMouseDelta( new Vector2( -2.0f, 4.0f ) );

			Assert.That( state.MouseDelta, Is.EqualTo( new Vector2( -2.0f, 4.0f ) ) );
		}
	}
}
