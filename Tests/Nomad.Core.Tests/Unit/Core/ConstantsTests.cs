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
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Nomad.Core;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Core")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class ConstantsTests
    {
        [Test]
        public void Constants_PublicValues_MatchExpectedFrameworkDefaults()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(Constants.WORDSIZE, Is.EqualTo(64));
                Assert.That(Constants.Audio.MAX_AUDIO_CHANNELS, Is.EqualTo(512));
                Assert.That(Constants.Audio.MIN_AUDIO_CHANNELS, Is.EqualTo(64));
                Assert.That(Constants.Multiplayer.MAX_PLAYERS, Is.EqualTo(16));
                Assert.That(Constants.FileSystem.MAXIMUM_MEMORY_STREAM_CAPACITY, Is.EqualTo(1024 * 1024 * 1024));
                Assert.That(Constants.Input.MAX_COOP_PLAYERS, Is.EqualTo(4));
                Assert.That(Constants.CVars.EngineUtils.Display.WINDOW_RESOLUTION, Is.EqualTo("display.WindowResolution"));
                Assert.That(Constants.Commands.Console.EXIT_COMMAND, Is.EqualTo("exit"));
            }
        }
    }
}
