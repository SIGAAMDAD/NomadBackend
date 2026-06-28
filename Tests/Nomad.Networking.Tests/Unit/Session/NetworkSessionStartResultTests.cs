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
    public sealed class NetworkSessionStartResultTests
    {
        [Test]
        public void Failed_CreatesFailureResultWithReason()
        {
            NetworkSessionStartResult result = NetworkSessionStartResult.Failed(NetworkSessionFailureReason.PlatformUnavailable);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Session, Is.Null);
                Assert.That(result.Reason, Is.EqualTo(NetworkSessionFailureReason.PlatformUnavailable));
            }
        }

        [Test]
        public void Started_CreatesSuccessResultWithSession()
        {
            var session = new NetworkSessionInfo { Mode = NetworkSessionMode.Host };

            NetworkSessionStartResult result = NetworkSessionStartResult.Started(session);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Success, Is.True);
                Assert.That(result.Session, Is.SameAs(session));
                Assert.That(result.Reason, Is.EqualTo(NetworkSessionFailureReason.None));
            }
        }
    }
}
