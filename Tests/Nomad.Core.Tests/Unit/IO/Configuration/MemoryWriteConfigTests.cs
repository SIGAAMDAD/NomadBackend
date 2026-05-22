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

using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.Memory.Buffers;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("IO.Configuration")]
    [Category("Unit")]
    [Category("UnitTests")]
    public class MemoryWriteConfigTests
    {
        [Test]
        public void MemoryWriteConfig_UsesMemoryStreamTypeAndRetainsOptions()
        {
            var buffer = new NullBufferHandle();
            var config = new MemoryWriteConfig
            {
                Buffer = buffer,
                Strategy = AllocationStrategy.Pooled,
                MaxCapacity = 4096,
                InitialCapacity = 256,
                FixedSize = true,
                Append = true
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(config.Type, Is.EqualTo(StreamType.Memory));
                Assert.That(config.Buffer, Is.SameAs(buffer));
                Assert.That(config.Strategy, Is.EqualTo(AllocationStrategy.Pooled));
                Assert.That(config.MaxCapacity, Is.EqualTo(4096));
                Assert.That(config.InitialCapacity, Is.EqualTo(256));
                Assert.That(config.FixedSize, Is.True);
                Assert.That(config.Append, Is.True);
            }
        }
    }
}
