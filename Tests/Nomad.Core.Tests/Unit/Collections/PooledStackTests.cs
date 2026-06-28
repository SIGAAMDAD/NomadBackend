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
    public sealed class PooledStackTests
    {
        [Test]
        public void PooledStack_PushPopAccessorsAndCopy_OperateLifo()
        {
            using var stack = new PooledStack<int>(1);

            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            var copied = new int[3];
            stack.CopyTo(copied);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stack.Count, Is.EqualTo(3));
                Assert.That(stack.GetFromBottom(0), Is.EqualTo(1));
                Assert.That(stack.GetFromTop(0), Is.EqualTo(3));
                Assert.That(copied, Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(stack.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(stack.Pop(), Is.EqualTo(3));
                Assert.That(stack.TryPop(out int value), Is.True);
                Assert.That(value, Is.EqualTo(2));
            }

            stack.Clear();
            Assert.That(stack.TryPop(out _), Is.False);
        }
    }
}
