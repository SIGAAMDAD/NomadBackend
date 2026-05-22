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

using Nomad.Core.Logger;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("IO.Sinks")]
    [Category("Unit")]
    [Category("UnitTests")]
    public class SinkBaseTests
    {
        [Test]
        public void SinkBase_Dispose_FlushesAndMarksDisposed()
        {
            var sink = new TestSink();

            sink.Dispose();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(sink.FlushCount, Is.EqualTo(1));
                Assert.That(sink.IsDisposed, Is.True);
            }
        }

        [Test]
        public void SinkBase_Dispose_IsIdempotent()
        {
            var sink = new TestSink();

            sink.Dispose();
            sink.Dispose();

            Assert.That(sink.FlushCount, Is.EqualTo(1));
        }

        private sealed class TestSink : SinkBase
        {
            public int FlushCount { get; private set; }
            public bool IsDisposed => isDisposed;

            public override void Print(string message)
            {
            }

            public override void Clear()
            {
            }

            public override void Flush()
            {
                FlushCount++;
            }
        }
    }
}
