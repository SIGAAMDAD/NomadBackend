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
    public class GamepadAxisEventTests {
        [Test]
        public void GamepadAxisEvent_Constructor_ExposesAssignedValues() {
            // Setup
            var value = new Vector2( 0.25f, -0.75f );
            var axisEvent = new GamepadAxisEventArgs( GamepadStick.Right, 123L, 7, value );

            // Assert
            using ( Assert.EnterMultipleScope() ) {
                Assert.That( axisEvent.Stick, Is.EqualTo( GamepadStick.Right ) );
                Assert.That( axisEvent.TimeStamp, Is.EqualTo( 123L ) );
                Assert.That( axisEvent.DeviceId, Is.EqualTo( 7 ) );
                Assert.That( axisEvent.Value, Is.EqualTo( value ) );
            }
        }

        [Test]
        public void GamepadAxisEvent_Equals_UsesStickAndValueOnly() {
            // Setup
            var left = new GamepadAxisEventArgs( GamepadStick.Left, 100L, 1, new Vector2( 1f, 0f ) );
            var sameMeaning = new GamepadAxisEventArgs( GamepadStick.Left, 200L, 99, new Vector2( 1f, 0f ) );
            var differentValue = new GamepadAxisEventArgs( GamepadStick.Left, 100L, 1, new Vector2( 0f, 1f ) );

            // Assert
            using ( Assert.EnterMultipleScope() ) {
                Assert.That( left, Is.EqualTo( sameMeaning ) );
                Assert.That( left, Is.Not.EqualTo( differentValue ) );
            }
        }

        [Test]
        public void GamepadAxisEvent_NotEquals_StickDifferentValueSame() {
            // Setup
            var left = new GamepadAxisEventArgs( GamepadStick.Left, 100L, 1, new Vector2( 1f, 0f ) );
            var same = new GamepadAxisEventArgs( GamepadStick.Right, 100L, 1, new Vector2( 1f, 0f ) );

            // Assert
            Assert.That( left, Is.Not.EqualTo( same ) );
        }

        [Test]
        public void GamepadAxisEvent_NotEquals_StickSameValueDifferent() {
            // Setup
            var left = new GamepadAxisEventArgs( GamepadStick.Left, 100L, 1, new Vector2( 1f, 0f ) );
            var same = new GamepadAxisEventArgs( GamepadStick.Left, 100L, 1, new Vector2( 2f, 0f ) );

            // Assert
            Assert.That( left, Is.Not.EqualTo( same ) );
        }
    }
}
