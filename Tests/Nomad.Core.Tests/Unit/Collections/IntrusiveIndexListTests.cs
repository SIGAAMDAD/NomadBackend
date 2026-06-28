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
    public sealed class IntrusiveIndexListTests
    {
        [Test]
        public void IntrusiveIndexList_AddLastRemoveAndClear_MaintainLinks()
        {
            var list = new IntrusiveIndexList(4);

            list.AddLast(1);
            list.AddLast(2);
            list.AddLast(3);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(list.Head, Is.EqualTo(1));
                Assert.That(list.Tail, Is.EqualTo(3));
                Assert.That(list.Count, Is.EqualTo(3));
                Assert.That(list.Next(1), Is.EqualTo(2));
                Assert.That(list.Previous(3), Is.EqualTo(2));
            }

            list.Remove(2);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(list.Count, Is.EqualTo(2));
                Assert.That(list.Next(1), Is.EqualTo(3));
                Assert.That(list.Previous(3), Is.EqualTo(1));
            }

            list.Clear();
            Assert.That(list.Count, Is.EqualTo(0));
        }
    }
}
