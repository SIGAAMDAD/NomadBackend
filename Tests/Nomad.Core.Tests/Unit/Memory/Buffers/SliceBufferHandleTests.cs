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
    public sealed class SliceBufferHandleTests
    {
        [Test]
        public void SliceBufferHandle_SpanMemoryClearAndCopy_OperateOnParentSlice()
        {
            var parent = new StandardBufferHandle(5);
            parent.CopyFrom(new byte[] { 1, 2, 3, 4, 5 }, 0, 5);
            var slice = new SliceBufferHandle(parent, 1, 3);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(slice.Length, Is.EqualTo(3));
                Assert.That(slice.Buffer.ToArray(), Is.EqualTo(new byte[] { 2, 3, 4 }));
                Assert.That(slice.AsSpan().ToArray(), Is.EqualTo(new byte[] { 2, 3, 4 }));
                Assert.That(slice.AsMemory().ToArray(), Is.EqualTo(new byte[] { 2, 3, 4 }));
            }

            slice.CopyFrom(new byte[] { 9, 8 }, 0, 2);
            Assert.That(parent.ToArray(), Is.EqualTo(new byte[] { 1, 9, 8, 4, 5 }));

            slice.Clear(1, 1);
            Assert.That(parent.ToArray(), Is.EqualTo(new byte[] { 1, 9, 0, 4, 5 }));
        }
    }
}
