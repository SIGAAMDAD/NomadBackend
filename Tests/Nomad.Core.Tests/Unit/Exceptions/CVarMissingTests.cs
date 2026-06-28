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
using Nomad.Core.Exceptions;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Exceptions")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class CVarMissingTests
    {
        [Test]
        public void CVarMissing_Constructor_CreatesExceptionWithMessage()
        {
            Exception exception = new CVarMissing("message");

            Assert.That(exception.Message, Does.Contain("message"));
        }
    }
}
