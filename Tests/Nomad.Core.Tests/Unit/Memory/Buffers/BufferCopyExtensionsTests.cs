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
using Nomad.Core.Memory.Buffers;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Buffers")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class BufferCopyExtensionsTests
    {
        [Test]
        public void BufferCopyExtensions_CopyFromAndCopyTo_CopyBetweenHandlesAndArrays()
        {
            var source = new StandardBufferHandle(4);
            var destination = new StandardBufferHandle(4);
            source.CopyFrom(new byte[] { 1, 2, 3, 4 }, 0, 4);

            destination.CopyFrom(source, 1, 2, 0);
            var array = new byte[4];
            destination.CopyTo(array, 0, 2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(destination.AsSpan(0, 2).ToArray(), Is.EqualTo(new byte[] { 2, 3 }));
                Assert.That(array[..2], Is.EqualTo(new byte[] { 2, 3 }));
            }
        }
    }
}
