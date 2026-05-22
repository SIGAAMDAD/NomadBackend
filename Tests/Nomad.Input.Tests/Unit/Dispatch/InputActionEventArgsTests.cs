/*
===========================================================================
The Nomad Framework
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System.Numerics;
using NUnit.Framework;
using Nomad.Input.ValueObjects;
using Nomad.Core.Util;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Dispatch")]
	[Category("Unit")]
	public class InputActionEventArgsTests {
		[Test]
		public void ButtonActionEventArgs_ExposeConstructorValues() {
			var args = new ButtonActionEventArgs( new InternString( "player.jump" ), InputActionPhase.Started, true, 123 );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( args.ActionId.ToString(), Is.EqualTo( "player.jump" ) );
				Assert.That( args.Phase, Is.EqualTo( InputActionPhase.Started ) );
				Assert.That( args.Value, Is.True );
				Assert.That( args.TimeStamp, Is.EqualTo( 123 ) );
			}
		}

		[Test]
		public void FloatActionEventArgs_ExposeConstructorValues() {
			var args = new FloatActionEventArgs( new InternString( "player.throttle" ), InputActionPhase.Performed, 0.75f, 456 );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( args.ActionId.ToString(), Is.EqualTo( "player.throttle" ) );
				Assert.That( args.Phase, Is.EqualTo( InputActionPhase.Performed ) );
				Assert.That( args.Value, Is.EqualTo( 0.75f ) );
				Assert.That( args.TimeStamp, Is.EqualTo( 456 ) );
			}
		}

		[Test]
		public void AxisActionEventArgs_ExposeConstructorValues() {
			Vector2 value = new( 2.0f, -3.0f );
			var args = new AxisActionEventArgs( new InternString( "camera.look" ), InputActionPhase.Canceled, value, 789 );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( args.ActionId.ToString(), Is.EqualTo( "camera.look" ) );
				Assert.That( args.Phase, Is.EqualTo( InputActionPhase.Canceled ) );
				Assert.That( args.Value, Is.EqualTo( value ) );
				Assert.That( args.TimeStamp, Is.EqualTo( 789 ) );
			}
		}
	}
}
