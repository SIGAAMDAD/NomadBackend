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
using Nomad.Core.Collections;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Collections")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class HandleTests
    {
        [Test]
        public void Handle_Constructor_AssignsIndexAndGeneration()
        {
            var handle = new Handle(3, 4);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handle.Index, Is.EqualTo(3));
                Assert.That(handle.Generation, Is.EqualTo(4));
                Assert.That(handle, Is.EqualTo(new Handle(3, 4)));
                Assert.That(handle, Is.Not.EqualTo(new Handle(3, 5)));
            }
        }
    }
}
