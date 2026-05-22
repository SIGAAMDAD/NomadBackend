using NUnit.Framework;
using Nomad.Core.Util;
using Nomad.Input.Private.ValueObjects;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("ValueObjects")]
	[Category("Unit")]
	public class PrivateValueObjectTests {
		[Test]
		public void LookupKeys_ExposeValuesAndUseValueEquality() {
			var button = new ButtonLookupKey( InputDeviceSlot.Keyboard, InputControlId.Space, true );
			var sameButton = new ButtonLookupKey( InputDeviceSlot.Keyboard, InputControlId.Space, true );
			var differentButton = new ButtonLookupKey( InputDeviceSlot.Keyboard, InputControlId.Space, false );
			var axis = new AxisLookupKey( InputDeviceSlot.Gamepad0, InputControlId.LeftStick );
			var sameAxis = new AxisLookupKey( InputDeviceSlot.Gamepad0, InputControlId.LeftStick );
			var differentAxis = new AxisLookupKey( InputDeviceSlot.Gamepad0, InputControlId.RightStick );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( button.Device, Is.EqualTo( InputDeviceSlot.Keyboard ) );
				Assert.That( button.Control, Is.EqualTo( InputControlId.Space ) );
				Assert.That( button.Pressed, Is.True );
				Assert.That( button.Equals( sameButton ), Is.True );
				Assert.That( button.Equals( (object)sameButton ), Is.True );
				Assert.That( button.Equals( differentButton ), Is.False );
				Assert.That( button.Equals( "nope" ), Is.False );
				Assert.That( button.GetHashCode(), Is.EqualTo( sameButton.GetHashCode() ) );
				Assert.That( axis.Device, Is.EqualTo( InputDeviceSlot.Gamepad0 ) );
				Assert.That( axis.Control, Is.EqualTo( InputControlId.LeftStick ) );
				Assert.That( axis.Equals( sameAxis ), Is.True );
				Assert.That( axis.Equals( (object)sameAxis ), Is.True );
				Assert.That( axis.Equals( differentAxis ), Is.False );
				Assert.That( axis.Equals( "nope" ), Is.False );
				Assert.That( axis.GetHashCode(), Is.EqualTo( sameAxis.GetHashCode() ) );
			}
		}

		[Test]
		public void SimplePrivateValueObjects_StoreConstructorAndInitValues() {
			var bucket = new Bucket( 4, 7 );
			var actionInfo = new CompiledActionInfo( new InternString( "player.jump" ) );
			var match = new BindingMatch { Binding = default, Score = 42 };
			var matchSet = new BindingMatchSet( new[] { 1, 3 }, new[] { 10, 20 } );
			var resolved = new ResolvedAction(
				new InternString( "player.jump" ),
				2,
				InputValueType.Button,
				InputActionPhase.Started,
				123,
				buttonValue: true );
			var bind = new Bind { Name = "Jump", ValueType = "Button", Scheme = "KeyboardAndMouse", Priority = 5, Binding = new InputMapping { Name = "Space" } };
			var mapping = new BindMapping { Bindings = new System.Collections.Generic.Dictionary<string, Bind> { ["Jump"] = bind } };

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( bucket.Start, Is.EqualTo( 4 ) );
				Assert.That( bucket.Length, Is.EqualTo( 7 ) );
				Assert.That( actionInfo.ActionId.ToString(), Is.EqualTo( "player.jump" ) );
				Assert.That( match.Score, Is.EqualTo( 42 ) );
				Assert.That( matchSet.Length, Is.EqualTo( 2 ) );
				Assert.That( matchSet.BindingIndices[1], Is.EqualTo( 3 ) );
				Assert.That( matchSet.Scores[0], Is.EqualTo( 10 ) );
				Assert.That( resolved.ActionId.ToString(), Is.EqualTo( "player.jump" ) );
				Assert.That( resolved.ActionIndex, Is.EqualTo( 2 ) );
				Assert.That( resolved.ButtonValue, Is.True );
				Assert.That( resolved.TimeStamp, Is.EqualTo( 123 ) );
				Assert.That( bind.Priority, Is.EqualTo( 5 ) );
				Assert.That( bind.Binding.Name, Is.EqualTo( "Space" ) );
				Assert.That( mapping.Bindings["Jump"], Is.SameAs( bind ) );
			}
		}
	}
}
