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

using Nomad.Core.Input;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Events.Input")]
    [Category("Unit")]
    [Category("UnitTests")]
    public class GamepadButtonEventTests
    {
        [Test]
        public void GamepadButtonEvent_Constructor_ExposesAssignedValues()
        {
            var buttonEvent = new GamepadButtonEventArgs(GamepadButton.A, 321L, 4, true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(buttonEvent.Button, Is.EqualTo(GamepadButton.A));
                Assert.That(buttonEvent.DeviceId, Is.EqualTo(4));
                Assert.That(buttonEvent.TimeStamp, Is.EqualTo(321L));
                Assert.That(buttonEvent.Pressed, Is.True);
            }
        }

        [Test]
        public void GamepadButtonEvent_Equals_UsesButtonAndPressedOnly()
        {
            var pressed = new GamepadButtonEventArgs(GamepadButton.B, 100L, 1, true);
            var sameMeaning = new GamepadButtonEventArgs(GamepadButton.B, 200L, 2, true);
            var differentState = new GamepadButtonEventArgs(GamepadButton.B, 100L, 1, false);

            using (Assert.EnterMultipleScope())
            {
				Assert.That( pressed, Is.EqualTo( sameMeaning ) );
				Assert.That( pressed, Is.Not.EqualTo( differentState ) );
            }
        }

        [Test]
        public void GamepadButtonEvent_NotEquals_ButtonSameValueDifferent()
        {
            var pressed = new GamepadButtonEventArgs(GamepadButton.B, 100L, 1, true);
            var sameMeaning = new GamepadButtonEventArgs(GamepadButton.B, 200L, 1, false);

			Assert.That( pressed, Is.Not.EqualTo( sameMeaning ) );
        }

        [Test]
        public void GamepadButtonEvent_NotEquals_ButtonDifferentValueSame()
        {
            var pressed = new GamepadButtonEventArgs(GamepadButton.B, 100L, 1, true);
            var sameMeaning = new GamepadButtonEventArgs(GamepadButton.A, 100L, 1, true);

			Assert.That( pressed, Is.Not.EqualTo( sameMeaning ) );
        }
    }
}
