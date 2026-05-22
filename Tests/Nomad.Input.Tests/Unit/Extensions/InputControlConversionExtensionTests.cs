using System;
using NUnit.Framework;
using Nomad.Core.Input;
using Nomad.Core.Input.ValueObjects;
using Nomad.Input.Private.Extensions;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Extensions")]
	[Category("Unit")]
	public class InputControlConversionExtensionTests {
		[Test]
		public void KeyNum_ToControlId_MapsSequentialKeyboardValuesAndRejectsInvalidValues() {
			using ( Assert.EnterMultipleScope() ) {
				Assert.That( KeyNum.Q.ToControlId(), Is.EqualTo( InputControlId.Q ) );
				Assert.That( KeyNum.Enter.ToControlId(), Is.EqualTo( InputControlId.Enter ) );
				Assert.That( () => KeyNum.Count.ToControlId(), Throws.TypeOf<ArgumentOutOfRangeException>() );
				Assert.That( () => ((KeyNum)255).ToControlId(), Throws.TypeOf<ArgumentOutOfRangeException>() );
			}
		}

		[Test]
		public void MouseButton_ToControlId_MapsAllButtonsAndRejectsInvalidValues() {
			using ( Assert.EnterMultipleScope() ) {
				Assert.That( MouseButton.Left.ToControlId(), Is.EqualTo( InputControlId.Left ) );
				Assert.That( MouseButton.Right.ToControlId(), Is.EqualTo( InputControlId.Right ) );
				Assert.That( MouseButton.Middle.ToControlId(), Is.EqualTo( InputControlId.Middle ) );
				Assert.That( MouseButton.WheelDown.ToControlId(), Is.EqualTo( InputControlId.WheelDown ) );
				Assert.That( MouseButton.WheelUp.ToControlId(), Is.EqualTo( InputControlId.WheelUp ) );
				Assert.That( MouseButton.X1.ToControlId(), Is.EqualTo( InputControlId.X1 ) );
				Assert.That( MouseButton.X2.ToControlId(), Is.EqualTo( InputControlId.X2 ) );
				Assert.That( () => ((MouseButton)255).ToControlId(), Throws.TypeOf<ArgumentOutOfRangeException>() );
			}
		}

		[Test]
		public void GamepadButton_ToControlId_MapsAllButtonsAndRejectsInvalidValues() {
			using ( Assert.EnterMultipleScope() ) {
				Assert.That( GamepadButton.A.ToControlId(), Is.EqualTo( InputControlId.GamepadA ) );
				Assert.That( GamepadButton.B.ToControlId(), Is.EqualTo( InputControlId.GamepadB ) );
				Assert.That( GamepadButton.X.ToControlId(), Is.EqualTo( InputControlId.GamepadX ) );
				Assert.That( GamepadButton.Y.ToControlId(), Is.EqualTo( InputControlId.GamepadY ) );
				Assert.That( GamepadButton.Back.ToControlId(), Is.EqualTo( InputControlId.Back ) );
				Assert.That( GamepadButton.Guide.ToControlId(), Is.EqualTo( InputControlId.Guide ) );
				Assert.That( GamepadButton.Start.ToControlId(), Is.EqualTo( InputControlId.Start ) );
				Assert.That( GamepadButton.LeftStick.ToControlId(), Is.EqualTo( InputControlId.LeftStickButton ) );
				Assert.That( GamepadButton.RightStick.ToControlId(), Is.EqualTo( InputControlId.RightStickButton ) );
				Assert.That( GamepadButton.LeftShoulder.ToControlId(), Is.EqualTo( InputControlId.LeftShoulder ) );
				Assert.That( GamepadButton.RightShoulder.ToControlId(), Is.EqualTo( InputControlId.RightShoulder ) );
				Assert.That( GamepadButton.DPadUp.ToControlId(), Is.EqualTo( InputControlId.DPadUp ) );
				Assert.That( GamepadButton.DPadDown.ToControlId(), Is.EqualTo( InputControlId.DPadDown ) );
				Assert.That( GamepadButton.DPadLeft.ToControlId(), Is.EqualTo( InputControlId.DPadLeft ) );
				Assert.That( GamepadButton.DPadRight.ToControlId(), Is.EqualTo( InputControlId.DPadRight ) );
				Assert.That( () => ((GamepadButton)255).ToControlId(), Throws.TypeOf<ArgumentOutOfRangeException>() );
			}
		}

		[Test]
		public void GamepadStick_ToControlId_MapsSticksAndRejectsInvalidValues() {
			using ( Assert.EnterMultipleScope() ) {
				Assert.That( GamepadStick.Left.ToControlId(), Is.EqualTo( InputControlId.LeftStick ) );
				Assert.That( GamepadStick.Right.ToControlId(), Is.EqualTo( InputControlId.RightStick ) );
				Assert.That( () => ((GamepadStick)255).ToControlId(), Throws.TypeOf<ArgumentOutOfRangeException>() );
			}
		}
	}
}
