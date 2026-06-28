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
    public sealed class DirtySetTests
    {
        [Test]
        public void DirtySet_MarkDirtyMarkCleanAndClear_TrackDirtyIds()
        {
            var set = new DirtySet(1, 1);

            Assert.That(set.MarkDirty(5), Is.True);
            Assert.That(set.MarkDirty(5), Is.False);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(set.Count, Is.EqualTo(1));
                Assert.That(set.IsDirty(5), Is.True);
                Assert.That(set.GetDirtyId(0), Is.EqualTo(5));
                Assert.That(set.DirtyIds.ToArray(), Is.EqualTo(new[] { 5 }));
            }

            Assert.That(set.MarkClean(5), Is.True);
            Assert.That(set.MarkClean(5), Is.False);
            set.MarkDirty(8);
            set.ClearDirty();
            Assert.That(set.Count, Is.EqualTo(0));
        }
    }
}
