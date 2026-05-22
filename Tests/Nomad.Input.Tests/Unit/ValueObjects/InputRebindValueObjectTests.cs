using NUnit.Framework;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("ValueObjects")]
	[Category("Unit")]
	public class InputRebindValueObjectTests {
		[Test]
		public void InputRebindRequest_StoresConstructorValuesAndDefaultsPartToWhole() {
			var defaultPart = new InputRebindRequest( "Gameplay", "player.jump", 2 );
			var explicitPart = new InputRebindRequest( "Gameplay", "player.move", 1, InputRebindPart.Left );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( defaultPart.MappingName, Is.EqualTo( "Gameplay" ) );
				Assert.That( defaultPart.ActionId, Is.EqualTo( "player.jump" ) );
				Assert.That( defaultPart.BindingIndex, Is.EqualTo( 2 ) );
				Assert.That( defaultPart.Part, Is.EqualTo( InputRebindPart.Whole ) );
				Assert.That( explicitPart.Part, Is.EqualTo( InputRebindPart.Left ) );
			}
		}

		[Test]
		public void InputRebindResult_StoresRequestAndBinding() {
			var request = new InputRebindRequest( "Gameplay", "player.jump", 0 );
			var binding = InputTestHelpers.Button( Nomad.Core.Input.InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Enter );

			var result = new InputRebindResult( request, binding );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( result.Request, Is.EqualTo( request ) );
				Assert.That( result.Binding, Is.SameAs( binding ) );
			}
		}
	}
}
