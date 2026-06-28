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
using Nomad.Core.Physics.ValueObjects;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("ValueObjects")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class PhysicsObjectHandleTests
    {
        [Test]
        public void PhysicsObjectHandle_EqualityOperatorsAndInvalid_UseId()
        {
            var handle = new PhysicsObjectHandle(7);
            var same = new PhysicsObjectHandle(7);
            var other = new PhysicsObjectHandle(8);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handle, Is.EqualTo(same));
                Assert.That(handle == same, Is.True);
                Assert.That(handle != other, Is.True);
                Assert.That(handle.IsValid, Is.True);
                Assert.That(PhysicsObjectHandle.Invalid.IsValid, Is.False);
            }
        }
    }
}
