using System.Collections.Immutable;
using NUnit.Framework;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("ValueObjects")]
	[Category("Unit")]
	public class ButtonBindingTests {
		[Test]
		public void Constructor_WithoutModifiersUsesEmptyModifierSet() {
			var binding = new ButtonBinding( InputDeviceSlot.Keyboard, InputControlId.Space );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( binding.DeviceId, Is.EqualTo( InputDeviceSlot.Keyboard ) );
				Assert.That( binding.ControlId, Is.EqualTo( InputControlId.Space ) );
				Assert.That( binding.Modifiers, Is.Empty );
				Assert.That( binding.ModifierCount, Is.Zero );
			}
		}

		[Test]
		public void Constructor_BuildsMasksAcrossAllSupportedWords() {
			var binding = new ButtonBinding(
				InputDeviceSlot.Keyboard,
				InputControlId.Enter,
				ImmutableArray.Create(
					(InputControlId)1,
					(InputControlId)64,
					(InputControlId)128,
					(InputControlId)192 ) );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( binding.ModifierCount, Is.EqualTo( 4 ) );
				Assert.That( binding.ModifierMask0, Is.EqualTo( 1UL << 1 ) );
				Assert.That( binding.ModifierMask1, Is.EqualTo( 1UL ) );
				Assert.That( binding.ModifierMask2, Is.EqualTo( 1UL ) );
				Assert.That( binding.ModifierMask3, Is.EqualTo( 1UL ) );
			}
		}

	}
}
