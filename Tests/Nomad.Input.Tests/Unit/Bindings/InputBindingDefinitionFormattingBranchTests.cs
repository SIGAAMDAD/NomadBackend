using NUnit.Framework;
using Nomad.Core.Input;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Bindings")]
	[Category("Unit")]
	public class InputBindingDefinitionFormattingBranchTests {
		[Test]
		public void ToString_FormatsAllSpecialControlsAndFallbackPascalCase() {
			using ( Assert.EnterMultipleScope() ) {
				Assert.That( InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Num0 ).ToString(), Is.EqualTo( "Keyboard: 0" ) );
				Assert.That( InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Period ).ToString(), Is.EqualTo( "Keyboard: ." ) );
				Assert.That( InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.SemiColon ).ToString(), Is.EqualTo( "Keyboard: ;" ) );
				Assert.That( InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Colon ).ToString(), Is.EqualTo( "Keyboard: :" ) );
				Assert.That( InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Grave ).ToString(), Is.EqualTo( "Keyboard: `" ) );
				Assert.That( InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.BackSpace ).ToString(), Is.EqualTo( "Keyboard: Backspace" ) );
				Assert.That( InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Mouse, InputControlId.X1 ).ToString(), Is.EqualTo( "Mouse: Button 4" ) );
				Assert.That( InputTestHelpers.Button( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.LeftStickButton ).ToString(), Is.EqualTo( "Gamepad 1: Left Stick Click" ) );
				Assert.That( InputTestHelpers.Button( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.DPadRight ).ToString(), Is.EqualTo( "Gamepad 1: D-Pad Right" ) );
				Assert.That( InputTestHelpers.Button( InputScheme.KeyboardAndMouse, (InputDeviceSlot)99, InputControlId.RightArrow ).ToString(), Is.EqualTo( "99: Right Arrow" ) );
				Assert.That( new InputBindingDefinition { Kind = (InputBindingKind)255 }.ToString(), Is.EqualTo( "Unbound" ) );
			}
		}

		[Test]
		public void ToString_CompositeWithPartialUnboundStillFormatsEachPart() {
			var axis1D = InputTestHelpers.Composite1D( InputScheme.KeyboardAndMouse, InputControlId.None, InputControlId.W );
			var axis2D = InputTestHelpers.Composite2D( InputScheme.KeyboardAndMouse, InputControlId.None, InputControlId.S, InputControlId.None, InputControlId.D );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( axis1D.ToString(), Is.EqualTo( "Negative: Unbound, Positive: W" ) );
				Assert.That( axis2D.ToString(), Is.EqualTo( "Up: Unbound, Down: S, Left: Unbound, Right: D" ) );
			}
		}
	}
}
