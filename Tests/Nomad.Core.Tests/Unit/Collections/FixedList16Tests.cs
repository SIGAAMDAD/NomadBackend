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
using System.Collections.Generic;
using System.Reflection;
using Nomad.Core.Collections;
using NUnit.Framework;

namespace Nomad.Core.Tests.Collections
{
    [TestFixture]
    public sealed class FixedList16Tests
    {
        [Test]
        public void NewList_HasZeroCountAndInlineCapacity()
        {
            using var list = new FixedList16<int>();

            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(16));
        }

        [Test]
        public void Add_WhenBelowInlineCapacity_AppendsItemsInline()
        {
            using var list = new FixedList16<int>();

            for (var i = 0; i < 16; i++)
            {
                list.Add(i * 10);

                Assert.That(list.Count, Is.EqualTo(i + 1));
                Assert.That(list.Capacity, Is.EqualTo(16));
                Assert.That(list[i], Is.EqualTo(i * 10));
            }
        }

        [Test]
        public void Add_WhenInlineCapacityExceeded_MovesInlineItemsToHeapAndAppendsNewItem()
        {
            using var list = new FixedList16<int>();

            for (var i = 0; i < 17; i++)
            {
                list.Add(i);
            }

            Assert.That(list.Count, Is.EqualTo(17));
            Assert.That(list.Capacity, Is.GreaterThanOrEqualTo(48));

            for (var i = 0; i < 17; i++)
            {
                Assert.That(list[i], Is.EqualTo(i));
            }
        }

        [Test]
        public void Add_WhenHeapIsFull_GrowsHeapAndPreservesExistingItems()
        {
            using var list = new FixedList16<int>();

            for (var i = 0; i < 17; i++)
            {
                list.Add(i);
            }

            var oldCapacity = list.Capacity;

            while (list.Count < oldCapacity)
            {
                list.Add(list.Count);
            }

            Assert.That(list.Count, Is.EqualTo(oldCapacity));

            list.Add(123456);

            Assert.That(list.Count, Is.EqualTo(oldCapacity + 1));
            Assert.That(list.Capacity, Is.GreaterThanOrEqualTo(oldCapacity * 2));
            Assert.That(list[oldCapacity], Is.EqualTo(123456));

            for (var i = 0; i < oldCapacity; i++)
            {
                Assert.That(list[i], Is.EqualTo(i));
            }
        }

        [Test]
        public void Indexer_WhenIndexIsValidInline_ReturnsStoredValue()
        {
            using var list = new FixedList16<string>();

            list.Add("alpha");
            list.Add("beta");
            list.Add("gamma");

            Assert.That(list[0], Is.EqualTo("alpha"));
            Assert.That(list[1], Is.EqualTo("beta"));
            Assert.That(list[2], Is.EqualTo("gamma"));
        }

        [Test]
        public void Indexer_WhenIndexIsValidHeap_ReturnsStoredValue()
        {
            using var list = new FixedList16<string>();

            for (var i = 0; i < 32; i++)
            {
                list.Add("item-" + i);
            }

            Assert.That(list[0], Is.EqualTo("item-0"));
            Assert.That(list[15], Is.EqualTo("item-15"));
            Assert.That(list[16], Is.EqualTo("item-16"));
            Assert.That(list[31], Is.EqualTo("item-31"));
        }

        [Test]
        public void Indexer_WhenAssignedThroughRefReturn_UpdatesInlineValue()
        {
            using var list = new FixedList16<int>();

            list.Add(10);
            list.Add(20);

            list[1] = 99;

            Assert.That(list[0], Is.EqualTo(10));
            Assert.That(list[1], Is.EqualTo(99));
        }

        [Test]
        public void Indexer_WhenAssignedThroughRefReturn_UpdatesHeapValue()
        {
            using var list = new FixedList16<int>();

            for (var i = 0; i < 17; i++)
            {
                list.Add(i);
            }

            list[16] = 777;

            Assert.That(list[16], Is.EqualTo(777));
        }

        [Test]
        public void Indexer_WhenIndexIsNegative_ThrowsArgumentOutOfRangeException()
        {
            using var list = new FixedList16<int>();

            list.Add(1);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _ = list[-1];
            });
        }

        [Test]
        public void Indexer_WhenIndexEqualsCount_ThrowsArgumentOutOfRangeException()
        {
            using var list = new FixedList16<int>();

            list.Add(1);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _ = list[1];
            });
        }

        [Test]
        public void Indexer_WhenListIsEmpty_ThrowsArgumentOutOfRangeException()
        {
            using var list = new FixedList16<int>();

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                _ = list[0];
            });
        }

        [Test]
        public void Clear_WhenValueTypeInline_ResetsCountAndKeepsInlineCapacity()
        {
            using var list = new FixedList16<int>();

            list.Add(1);
            list.Add(2);
            list.Add(3);

            list.Clear();

            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(16));
        }

        [Test]
        public void Clear_WhenReferenceTypeInline_ResetsCountAndKeepsInlineCapacity()
        {
            using var list = new FixedList16<string>();

            list.Add("a");
            list.Add("b");
            list.Add("c");

            list.Clear();

            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(16));
        }

        [Test]
        public void Clear_WhenValueTypeHeap_ResetsCountAndKeepsHeapCapacity()
        {
            using var list = new FixedList16<int>();

            for (var i = 0; i < 20; i++)
            {
                list.Add(i);
            }

            var capacity = list.Capacity;

            list.Clear();

            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(capacity));
        }

        [Test]
        public void Clear_WhenReferenceTypeHeap_ClearsHeapSlotsAndResetsCount()
        {
            using var list = new FixedList16<string>();

            for (var i = 0; i < 20; i++)
            {
                list.Add("item-" + i);
            }

            var heap = GetHeap(list);
            Assert.That(heap, Is.Not.Null);

            list.Clear();

            Assert.That(list.Count, Is.EqualTo(0));

            for (var i = 0; i < 20; i++)
            {
                Assert.That(heap![i], Is.Null);
            }
        }

        [Test]
        public void RemoveSwapBack_WhenItemExistsInlineWithDefaultComparer_RemovesItemAndReturnsTrue()
        {
            using var list = new FixedList16<int>();

            list.Add(10);
            list.Add(20);
            list.Add(30);
            list.Add(40);

            var removed = list.RemoveSwapBack(20);

            Assert.That(removed, Is.True);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0], Is.EqualTo(10));
            Assert.That(list[1], Is.EqualTo(40));
            Assert.That(list[2], Is.EqualTo(30));
        }

        [Test]
        public void RemoveSwapBack_WhenItemExistsHeapWithDefaultComparer_RemovesItemAndReturnsTrue()
        {
            using var list = new FixedList16<int>();

            for (var i = 0; i < 20; i++)
            {
                list.Add(i);
            }

            var removed = list.RemoveSwapBack(5);

            Assert.That(removed, Is.True);
            Assert.That(list.Count, Is.EqualTo(19));
            Assert.That(list[5], Is.EqualTo(19));
        }

        [Test]
        public void RemoveSwapBack_WhenItemDoesNotExist_ReturnsFalseAndDoesNotChangeCount()
        {
            using var list = new FixedList16<int>();

            list.Add(1);
            list.Add(2);
            list.Add(3);

            var removed = list.RemoveSwapBack(999);

            Assert.That(removed, Is.False);
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(3));
        }

        [Test]
        public void RemoveSwapBack_WhenCustomComparerMatches_RemovesItemAndReturnsTrue()
        {
            using var list = new FixedList16<string>();

            list.Add("Alpha");
            list.Add("Beta");
            list.Add("Gamma");

            var removed = list.RemoveSwapBack("beta", StringComparer.OrdinalIgnoreCase);

            Assert.That(removed, Is.True);
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0], Is.EqualTo("Alpha"));
            Assert.That(list[1], Is.EqualTo("Gamma"));
        }

        [Test]
        public void RemoveAtSwapBack_WhenRemovingMiddleInline_ReplacesRemovedItemWithLastItem()
        {
            using var list = new FixedList16<int>();

            list.Add(1);
            list.Add(2);
            list.Add(3);
            list.Add(4);

            list.RemoveAtSwapBack(1);

            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(4));
            Assert.That(list[2], Is.EqualTo(3));
        }

        [Test]
        public void RemoveAtSwapBack_WhenRemovingLastInline_DoesNotMoveAnotherItem()
        {
            using var list = new FixedList16<int>();

            list.Add(1);
            list.Add(2);
            list.Add(3);

            list.RemoveAtSwapBack(2);

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
        }

        [Test]
        public void RemoveAtSwapBack_WhenRemovingMiddleHeap_ReplacesRemovedItemWithLastItem()
        {
            using var list = new FixedList16<int>();

            for (var i = 0; i < 20; i++)
            {
                list.Add(i);
            }

            list.RemoveAtSwapBack(3);

            Assert.That(list.Count, Is.EqualTo(19));
            Assert.That(list[3], Is.EqualTo(19));
        }

        [Test]
        public void RemoveAtSwapBack_WhenRemovingLastHeap_DoesNotMoveAnotherItem()
        {
            using var list = new FixedList16<int>();

            for (var i = 0; i < 20; i++)
            {
                list.Add(i);
            }

            list.RemoveAtSwapBack(19);

            Assert.That(list.Count, Is.EqualTo(19));
            Assert.That(list[18], Is.EqualTo(18));
        }

        [Test]
        public void RemoveAtSwapBack_WhenReferenceTypeHeap_ClearsFormerLastSlot()
        {
            using var list = new FixedList16<string>();

            for (var i = 0; i < 20; i++)
            {
                list.Add("item-" + i);
            }

            var heap = GetHeap(list);
            Assert.That(heap, Is.Not.Null);

            list.RemoveAtSwapBack(5);

            Assert.That(list.Count, Is.EqualTo(19));
            Assert.That(list[5], Is.EqualTo("item-19"));
            Assert.That(heap![19], Is.Null);
        }

        [Test]
        public void RemoveAtSwapBack_WhenIndexIsNegative_ThrowsArgumentOutOfRangeException()
        {
            using var list = new FixedList16<int>();

            list.Add(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAtSwapBack(-1));
        }

        [Test]
        public void RemoveAtSwapBack_WhenIndexEqualsCount_ThrowsArgumentOutOfRangeException()
        {
            using var list = new FixedList16<int>();

            list.Add(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAtSwapBack(1));
        }

        [Test]
        public void RemoveAtSwapBack_WhenListIsEmpty_ThrowsArgumentOutOfRangeException()
        {
            using var list = new FixedList16<int>();

            Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAtSwapBack(0));
        }

        [Test]
        public void Dispose_WhenNoHeapExists_ResetsCountAndLeavesInlineCapacity()
        {
            var list = new FixedList16<int>();

            list.Add(1);
            list.Add(2);

            list.Dispose();

            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(16));
        }

        [Test]
        public void Dispose_WhenHeapExistsForValueType_ReturnsHeapAndResetsToInlineState()
        {
            var list = new FixedList16<int>();

            for (var i = 0; i < 20; i++)
            {
                list.Add(i);
            }

            Assert.That(list.Capacity, Is.GreaterThan(16));

            list.Dispose();

            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(16));
            Assert.That(GetHeap(list), Is.Null);
        }

        [Test]
        public void Dispose_WhenHeapExistsForReferenceType_ReturnsHeapAndResetsToInlineState()
        {
            var list = new FixedList16<string>();

            for (var i = 0; i < 20; i++)
            {
                list.Add("item-" + i);
            }

            Assert.That(list.Capacity, Is.GreaterThan(16));

            list.Dispose();

            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(16));
            Assert.That(GetHeap(list), Is.Null);
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var list = new FixedList16<int>();

            for (var i = 0; i < 20; i++)
            {
                list.Add(i);
            }

            Assert.DoesNotThrow(() =>
            {
                list.Dispose();
                list.Dispose();
                list.Dispose();
            });

            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(16));
        }

        [Test]
        public void Add_AfterDispose_UsesInlineStorageAgain()
        {
            var list = new FixedList16<int>();

            for (var i = 0; i < 20; i++)
            {
                list.Add(i);
            }

            list.Dispose();

            list.Add(42);

            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list.Capacity, Is.EqualTo(16));
            Assert.That(list[0], Is.EqualTo(42));

            list.Dispose();
        }

        [Test]
        public void ComplexSequence_AddRemoveClearDispose_MaintainsValidState()
        {
            var list = new FixedList16<string>();

            for (var i = 0; i < 18; i++)
            {
                list.Add("item-" + i);
            }

            Assert.That(list.Count, Is.EqualTo(18));
            Assert.That(list.Capacity, Is.GreaterThan(16));

            Assert.That(list.RemoveSwapBack("item-7"), Is.True);
            Assert.That(list.Count, Is.EqualTo(17));

            list.RemoveAtSwapBack(0);
            Assert.That(list.Count, Is.EqualTo(16));

            list.Clear();
            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.GreaterThan(16));

            list.Add("after-clear");
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo("after-clear"));

            list.Dispose();
            Assert.That(list.Count, Is.EqualTo(0));
            Assert.That(list.Capacity, Is.EqualTo(16));
        }

        private static T[]? GetHeap<T>(FixedList16<T> list)
        {
            var field = typeof(FixedList16<T>).GetField(
                "_heap",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null);

            return (T[]?)field!.GetValue(list);
        }
    }
}