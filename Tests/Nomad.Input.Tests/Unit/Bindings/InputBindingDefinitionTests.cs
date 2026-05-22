using NUnit.Framework;
using Nomad.Core.Input;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Bindings")]
	[Category("Unit")]
	public class InputBindingDefinitionTests {
		[Test]
		public void ToString_FormatsButtonsWithDeviceAndModifiers() {
			var binding = InputTestHelpers.Button(
				InputScheme.KeyboardAndMouse,
				InputDeviceSlot.Keyboard,
				InputControlId.Space,
				InputControlId.Ctrl,
				InputControlId.Shift );

			Assert.That( binding.ToString(), Is.EqualTo( "Keyboard: Ctrl + Shift + Space" ) );
		}

		[Test]
		public void ToString_FormatsAxisBindingsUsingFriendlyDeviceNames() {
			var axis1D = InputTestHelpers.Axis1D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger );
			var axis2D = InputTestHelpers.Axis2D( InputScheme.Gamepad, InputDeviceSlot.Gamepad3, InputControlId.RightStick );
			var delta = InputTestHelpers.Delta2D( InputScheme.KeyboardAndMouse, InputDeviceSlot.Mouse, InputControlId.Delta );

			using (Assert.EnterMultipleScope())
			{
				Assert.That( axis1D.ToString(), Is.EqualTo( "Gamepad 1: Left Trigger" ) );
				Assert.That( axis2D.ToString(), Is.EqualTo( "Gamepad 4: Right Stick" ) );
				Assert.That( delta.ToString(), Is.EqualTo( "Mouse: Delta" ) );
			}
		}

		[Test]
		public void ToString_FormatsCompositeBindings() {
			var axis1D = InputTestHelpers.Composite1D( InputScheme.KeyboardAndMouse, InputControlId.S, InputControlId.W );
			var axis2D = InputTestHelpers.Composite2D( InputScheme.KeyboardAndMouse, InputControlId.UpArrow, InputControlId.DownArrow, InputControlId.LeftArrow, InputControlId.RightArrow );

			using (Assert.EnterMultipleScope())
			{
				Assert.That( axis1D.ToString(), Is.EqualTo( "Negative: S, Positive: W" ) );
				Assert.That( axis2D.ToString(), Is.EqualTo( "Up: Up Arrow, Down: Down Arrow, Left: Left Arrow, Right: Right Arrow" ) );
			}
		}

		[Test]
		public void ToString_ReturnsUnboundWhenBindingHasNoUsableControl() {
			var button = InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.None );
			var axis1D = InputTestHelpers.Axis1D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.None );
			var axis2D = InputTestHelpers.Axis2D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.None );
			var delta = InputTestHelpers.Delta2D( InputScheme.KeyboardAndMouse, InputDeviceSlot.Mouse, InputControlId.None );
			var composite1D = InputTestHelpers.Composite1D( InputScheme.KeyboardAndMouse, InputControlId.None, InputControlId.None );
			var composite2D = InputTestHelpers.Composite2D( InputScheme.KeyboardAndMouse, InputControlId.None, InputControlId.None, InputControlId.None, InputControlId.None );

			using (Assert.EnterMultipleScope())
			{
				Assert.That( button.ToString(), Is.EqualTo( "Unbound" ) );
				Assert.That( axis1D.ToString(), Is.EqualTo( "Unbound" ) );
				Assert.That( axis2D.ToString(), Is.EqualTo( "Unbound" ) );
				Assert.That( delta.ToString(), Is.EqualTo( "Unbound" ) );
				Assert.That( composite1D.ToString(), Is.EqualTo( "Unbound" ) );
				Assert.That( composite2D.ToString(), Is.EqualTo( "Unbound" ) );
			}
		}
	}
}
