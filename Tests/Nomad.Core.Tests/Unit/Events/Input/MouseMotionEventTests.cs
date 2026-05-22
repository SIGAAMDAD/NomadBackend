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
using Nomad.Core.Input;
using NUnit.Framework;

namespace Nomad.Core.Tests {
	[TestFixture]
	[Category( "Nomad.Core" )]
	[Category( "Events.Input" )]
	[Category( "Unit" )]
	[Category( "UnitTests" )]
	public class MouseMotionEventTests {
		[Test]
		public void MouseMotionEvent_Constructor_ExposesAssignedValues() {
			// Setup
			var motionEvent = new MouseMotionEventArgs( 100L, 0, 1 );

			// Assert
			using ( Assert.EnterMultipleScope() ) {
				Assert.That( motionEvent.TimeStamp, Is.EqualTo( 100L ) );
				Assert.That( motionEvent.RelativeX, Is.Zero );
				Assert.That( motionEvent.RelativeY, Is.EqualTo( 1 ) );
			}
		}

		[Test]
		public void MouseMotionEvent_Equals_SameRelativeValues() {
			// Setup
			var a = new MouseMotionEventArgs( 100L, 1, 2 );
			var b = new MouseMotionEventArgs( 100L, 1, 2 );

			// Assert
			Assert.That( a, Is.EqualTo( b ) );
		}

		[Test]
		public void MouseMotionEvent_NotEquals_DifferentRelativeValues() {
			// Setup
			var a = new MouseMotionEventArgs( 100L, 2, 2 );
			var b = new MouseMotionEventArgs( 100L, 1, 2 );

			// Assert
			Assert.That( a, Is.Not.EqualTo( b ) );
		}

		[Test]
		public void MouseMotionEvent_Equals_DifferentTimestampSameRelativeValues() {
			// Setup
			var a = new MouseMotionEventArgs( 200L, 1, 2 );
			var b = new MouseMotionEventArgs( 100L, 1, 2 );

			// Assert
			Assert.That( a, Is.EqualTo( b ) );
		}
	}
}
