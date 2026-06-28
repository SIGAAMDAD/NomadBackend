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
using Nomad.Core.Memory;
using Nomad.Core.Util;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Memory")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class StringPoolTests
    {
        [SetUp]
        public void SetUp()
        {
            StringPool.Clear();
        }

        [Test]
        public void StringPool_InternAndFromInterned_RoundTripStrings()
        {
            InternString first = StringPool.Intern("hello");
            InternString second = StringPool.Intern("hello");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.EqualTo(second));
                Assert.That(StringPool.FromInterned(first), Is.EqualTo("hello"));
                Assert.That(StringPool.TryGetString(first, out string? value), Is.True);
                Assert.That(value, Is.EqualTo("hello"));
                Assert.That(StringPool.Intern(null), Is.EqualTo(InternString.Empty));
            }
        }

        [Test]
        public void StringPool_Clear_InvalidatesExistingInternedStrings()
        {
            InternString value = StringPool.Intern("lost");

            StringPool.Clear();

            Assert.Throws<StringNotInternedException>(() => StringPool.FromInterned(value));
        }
    }
}
