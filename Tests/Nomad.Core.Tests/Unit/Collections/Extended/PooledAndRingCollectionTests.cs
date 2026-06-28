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

namespace Nomad.Core.Tests.Collections
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Collections")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class PooledAndRingCollectionTests
    {
        [Test]
        public void PooledQueue_EnqueueDequeueWrapGrowAndDispose_WorkAsFifo()
        {
            using var queue = new PooledQueue<int>(initialCapacity: 2);

            queue.Enqueue(1);
            queue.Enqueue(2);
            Assert.That(queue.Dequeue(), Is.EqualTo(1));
            queue.Enqueue(3);
            queue.Enqueue(4);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(queue.Count, Is.EqualTo(3));
                Assert.That(queue.PeekRef(), Is.EqualTo(2));
                Assert.That(queue.GetFromOldest(1), Is.EqualTo(3));
                Assert.That(queue.GetFromNewest(0), Is.EqualTo(4));
                Assert.That(queue.Dequeue(), Is.EqualTo(2));
                Assert.That(queue.TryDequeue(out int value), Is.True);
                Assert.That(value, Is.EqualTo(3));
            }

            queue.Clear();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(queue.Count, Is.EqualTo(0));
                Assert.That(queue.TryDequeue(out int missing), Is.False);
                Assert.That(missing, Is.EqualTo(0));
            }
        }

        [Test]
        public void PooledQueue_AfterDispose_ThrowsObjectDisposedExceptionOnUse()
        {
            var queue = new PooledQueue<int>();

            queue.Dispose();

            Assert.Throws<ObjectDisposedException>(() => queue.Enqueue(1));
        }

        [Test]
        public void PooledStack_PushPopCopyAndToArray_WorkAsLifo()
        {
            using var stack = new PooledStack<int>(initialCapacity: 1);

            stack.Push(1);
            stack.Push(2);
            stack.Push(3);
            int[] copy = new int[3];
            stack.CopyTo(copy);
            int[] array = stack.ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stack.Count, Is.EqualTo(3));
                Assert.That(stack.Capacity, Is.GreaterThanOrEqualTo(3));
                Assert.That(stack.GetFromBottom(0), Is.EqualTo(1));
                Assert.That(stack.GetFromTop(0), Is.EqualTo(3));
                Assert.That(copy, Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(array, Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(stack.Pop(), Is.EqualTo(3));
                Assert.That(stack.TryPop(out int value), Is.True);
                Assert.That(value, Is.EqualTo(2));
            }

            stack.Clear();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stack.Count, Is.EqualTo(0));
                Assert.That(stack.TryPop(out int missing), Is.False);
                Assert.That(missing, Is.EqualTo(0));
            }
        }

        [Test]
        public void PooledStack_AfterDispose_ThrowsObjectDisposedExceptionOnUse()
        {
            var stack = new PooledStack<int>();

            stack.Dispose();

            Assert.Throws<ObjectDisposedException>(() => stack.Push(1));
        }

        [Test]
        public void FixedRingBuffer_PushPastCapacity_OverwritesOldestValues()
        {
            var ring = new FixedRingBuffer<int>(capacity: 3);

            ring.PushBack(1);
            ring.PushBack(2);
            ring.PushBack(3);
            ring.PushBack(4);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ring.Count, Is.EqualTo(3));
                Assert.That(ring.Capacity, Is.EqualTo(3));
                Assert.That(ring.GetFromNewest(0), Is.EqualTo(4));
                Assert.That(ring.GetFromNewest(1), Is.EqualTo(3));
                Assert.That(ring.GetFromNewest(2), Is.EqualTo(2));
            }

            ring.Clear();
            Assert.That(ring.Count, Is.EqualTo(0));
        }

        [Test]
        public void FixedRingBuffer_InvalidCapacity_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FixedRingBuffer<int>(0));
        }

        [Test]
        public void HandlePool_AllocateFreeReuseAndGeneration_PreventStaleHandles()
        {
            var pool = new HandlePool<string>(initialCapacity: 1);
            var neverAllocated = new Handle(0, 0);

            Assert.That(pool.IsAlive(neverAllocated), Is.False);

            Handle first = pool.Allocate("alpha");
            ref string value = ref pool.Get(first);
            value = "beta";
            bool freed = pool.Free(first);
            bool freedAgain = pool.Free(first);
            Handle second = pool.Allocate("gamma");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.IsValid, Is.True);
                Assert.That(freed, Is.True);
                Assert.That(freedAgain, Is.False);
                Assert.That(pool.IsAlive(first), Is.False);
                Assert.That(pool.IsAlive(second), Is.True);
                Assert.That(second.Index, Is.EqualTo(first.Index));
                Assert.That(second.Generation, Is.EqualTo(first.Generation + 1));
                Assert.That(pool.Get(second), Is.EqualTo("gamma"));
            }
        }

        [Test]
        public void DoubleBuffer_Swap_ExchangesCurrentAndNext()
        {
            var buffer = new DoubleBuffer<int>(1, 2);

            buffer.Swap();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(buffer.Current, Is.EqualTo(2));
                Assert.That(buffer.Next, Is.EqualTo(1));
            }
        }
    }
}
