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

using Nomad.Networking.Session;
using NUnit.Framework;

namespace Nomad.Networking.Tests.Session
{
    [TestFixture]
    [Category("Nomad.Networking")]
    [Category("Session")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class NetworkSessionFailureReasonTests
    {
        [Test]
        public void Values_AreStableProtocolValues()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That((byte)NetworkSessionFailureReason.None, Is.EqualTo(0));
                Assert.That((byte)NetworkSessionFailureReason.Unknown, Is.EqualTo(1));
                Assert.That((byte)NetworkSessionFailureReason.SessionNotFound, Is.EqualTo(2));
                Assert.That((byte)NetworkSessionFailureReason.SessionFull, Is.EqualTo(3));
                Assert.That((byte)NetworkSessionFailureReason.Timeout, Is.EqualTo(4));
                Assert.That((byte)NetworkSessionFailureReason.Cancelled, Is.EqualTo(5));
                Assert.That((byte)NetworkSessionFailureReason.PlatformUnavailable, Is.EqualTo(6));
            }
        }
    }
}
