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

using NUnit.Framework;
using Nomad.Core.Util;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Attributes")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class ResultObjectAttributeTests
    {
        [Test]
        public void ResultObjectAttribute_Constructor_CreatesAttributeInstance()
        {
            var attribute = new ResultObjectAttribute();

            Assert.That(attribute, Is.Not.Null);
        }
    }
}
