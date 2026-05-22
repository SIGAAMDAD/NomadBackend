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
using NUnit.Framework;
using Nomad.Core.Collections;

namespace Nomad.Core.Tests
{
    [TestFixture]
    public sealed class FixedList8Tests
    {
        private const int InlineCapacity = 8;

        [Test]
        public void Constructor_CreatesEmptyInlineList()
        {
            using FixedList8<int> list = new FixedList8<int>();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(0));
                Assert.That(list.Capacity, Is.EqualTo(InlineCapacity));
            }
        }

        [Test]
        public void Add_WithinInlineCapacity_StoresValuesWithoutHeapPromotion()
        {
            using FixedList8<int> list = new FixedList8<int>();

            for (int i = 0; i < InlineCapacity; i++)
            {
                list.Add(i * 10);
            }

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(InlineCapacity));
                Assert.That(list.Capacity, Is.EqualTo(InlineCapacity));
            }

            AssertSequence(list, 0, 10, 20, 30, 40, 50, 60, 70);
        }

        [Test]
        public void Add_NinthElement_PromotesToHeapAndPreservesInlineValues()
        {
            using FixedList8<int> list = new FixedList8<int>();

            for (int i = 0; i < InlineCapacity + 1; i++)
            {
                list.Add(i);
            }

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(InlineCapacity + 1));
                Assert.That(list.Capacity, Is.GreaterThan(InlineCapacity));
            }

            AssertSequence(list, 0, 1, 2, 3, 4, 5, 6, 7, 8);
        }

        [Test]
        public void Add_WhenHeapIsNotFull_AppendsWithoutChangingCapacity()
        {
            using FixedList8<int> list = new FixedList8<int>();

            for (int i = 0; i < InlineCapacity + 1; i++)
            {
                list.Add(i);
            }

            int capacityBefore = list.Capacity;

            list.Add(9);

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(10));
                Assert.That(list.Capacity, Is.EqualTo(capacityBefore));
            }

            AssertSequence(list, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9);
        }

        [Test]
        public void Add_WhenHeapIsFull_GrowsAndPreservesValueTypeValues()
        {
            using FixedList8<int> list = new FixedList8<int>();

            for (int i = 0; i < InlineCapacity + 1; i++)
            {
                list.Add(i);
            }

            int capacityBefore = list.Capacity;

            while (list.Count < capacityBefore)
            {
                list.Add(list.Count);
            }

            Assert.That(list.Count, Is.EqualTo(capacityBefore));

            list.Add(capacityBefore);

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(capacityBefore + 1));
                Assert.That(list.Capacity, Is.GreaterThan(capacityBefore));
            }

            for (int i = 0; i < list.Count; i++)
            {
                Assert.That(list[i], Is.EqualTo(i), $"Value at index {i} was not preserved.");
            }
        }

        [Test]
        public void Add_WhenReferenceTypeHeapIsFull_GrowsAndPreservesReferenceValues()
        {
            using FixedList8<string> list = new FixedList8<string>();

            for (int i = 0; i < InlineCapacity + 1; i++)
            {
                list.Add($"value-{i}");
            }

            int capacityBefore = list.Capacity;

            while (list.Count < capacityBefore)
            {
                list.Add($"value-{list.Count}");
            }

            list.Add($"value-{capacityBefore}");

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(capacityBefore + 1));
                Assert.That(list.Capacity, Is.GreaterThan(capacityBefore));
                Assert.That(list[0], Is.EqualTo("value-0"));
                Assert.That(list[InlineCapacity], Is.EqualTo($"value-{InlineCapacity}"));
                Assert.That(list[capacityBefore], Is.EqualTo($"value-{capacityBefore}"));
            }
        }

        [Test]
        public void Add_AllowsNullReferenceValues()
        {
            using FixedList8<string?> list = new FixedList8<string?>();

            list.Add("before");
            list.Add(null);
            list.Add("after");

            AssertSequence<string?>(list, "before", null, "after");
        }

        [Test]
        public void Indexer_ThrowsForNegativeIndex()
        {
            using FixedList8<int> list = new FixedList8<int>();
            list.Add(123);

            ArgumentOutOfRangeException? exception = Assert.Throws<ArgumentOutOfRangeException>(() => ReadAt(list, -1));

            Assert.That(exception?.ParamName, Is.EqualTo("index"));
        }

        [Test]
        public void Indexer_ThrowsForIndexEqualToCountOnEmptyList()
        {
            using FixedList8<int> list = new FixedList8<int>();

            ArgumentOutOfRangeException? exception = Assert.Throws<ArgumentOutOfRangeException>(() => ReadAt(list, 0));

            Assert.That(exception?.ParamName, Is.EqualTo("index"));
        }

        [Test]
        public void Indexer_ThrowsForIndexEqualToCountOnInlineList()
        {
            using FixedList8<int> list = new FixedList8<int>();
            list.Add(1);
            list.Add(2);

            ArgumentOutOfRangeException? exception = Assert.Throws<ArgumentOutOfRangeException>(() => ReadAt(list, 2));

            Assert.That(exception?.ParamName, Is.EqualTo("index"));
        }

        [Test]
        public void Indexer_ThrowsForIndexEqualToCountOnHeapList()
        {
            using FixedList8<int> list = new FixedList8<int>();

            for (int i = 0; i < InlineCapacity + 1; i++)
            {
                list.Add(i);
            }

            ArgumentOutOfRangeException? exception = Assert.Throws<ArgumentOutOfRangeException>(() => ReadAt(list, InlineCapacity + 1));

            Assert.That(exception?.ParamName, Is.EqualTo("index"));
        }

        [Test]
        public void Indexer_ReturnsRefToInlineValue()
        {
            using FixedList8<int> list = new FixedList8<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            ref int value = ref list[1];
            value = 200;

            AssertSequence(list, 1, 200, 3);
        }

        [Test]
        public void Indexer_ReturnsRefToHeapValue()
        {
            using FixedList8<int> list = new FixedList8<int>();

            for (int i = 0; i < InlineCapacity + 1; i++)
            {
                list.Add(i);
            }

            ref int value = ref list[InlineCapacity];
            value = 800;

            AssertSequence(list, 0, 1, 2, 3, 4, 5, 6, 7, 800);
        }

        [Test]
        public void Indexer_AssignmentMutatesInlineValue()
        {
            using FixedList8<string> list = new FixedList8<string>();
            list.Add("a");
            list.Add("b");

            list[0] = "changed";

            AssertSequence(list, "changed", "b");
        }

        [Test]
        public void Indexer_AssignmentMutatesHeapValue()
        {
            using FixedList8<string> list = new FixedList8<string>();

            for (int i = 0; i < InlineCapacity + 1; i++)
            {
                list.Add($"value-{i}");
            }

            list[InlineCapacity] = "changed";

            Assert.That(list[InlineCapacity], Is.EqualTo("changed"));
        }

        [Test]
        public void Clear_EmptyList_LeavesListEmptyAndInline()
        {
            using FixedList8<int> list = new FixedList8<int>();

            list.Clear();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(0));
                Assert.That(list.Capacity, Is.EqualTo(InlineCapacity));
            }
        }

        [Test]
        public void Clear_ValueTypeInlineList_ResetsCountAndAllowsReuse()
        {
            using FixedList8<int> list = new FixedList8<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            list.Clear();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(0));
                Assert.That(list.Capacity, Is.EqualTo(InlineCapacity));
            }

            list.Add(42);

            AssertSequence(list, 42);
        }

        [Test]
        public void Clear_ValueTypeHeapList_ResetsCountKeepsHeapCapacityAndAllowsReuse()
        {
            using FixedList8<int> list = new FixedList8<int>();

            for (int i = 0; i < InlineCapacity + 5; i++)
            {
                list.Add(i);
            }

            int capacityBefore = list.Capacity;

            list.Clear();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(0));
                Assert.That(list.Capacity, Is.EqualTo(capacityBefore));
            }

            list.Add(99);
            list.Add(100);

            AssertSequence(list, 99, 100);
        }

        [Test]
        public void Clear_ReferenceTypeInlineList_ResetsCountAndAllowsReuse()
        {
            using FixedList8<string?> list = new FixedList8<string?>();
            list.Add("a");
            list.Add(null);
            list.Add("c");

            list.Clear();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(0));
                Assert.That(list.Capacity, Is.EqualTo(InlineCapacity));
            }

            list.Add("new");

            AssertSequence<string?>(list, "new");
        }

        [Test]
        public void Clear_ReferenceTypeHeapList_ResetsCountKeepsHeapCapacityAndAllowsReuse()
        {
            using FixedList8<string?> list = new FixedList8<string?>();

            for (int i = 0; i < InlineCapacity + 4; i++)
            {
                list.Add($"value-{i}");
            }

            int capacityBefore = list.Capacity;

            list.Clear();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(0));
                Assert.That(list.Capacity, Is.EqualTo(capacityBefore));
            }

            list.Add(null);
            list.Add("after-clear");

            AssertSequence<string?>(list, null, "after-clear");
        }

        [Test]
        public void Clear_StructContainingReference_UsesReferenceCleanupPathAndAllowsReuse()
        {
            using FixedList8<ReferenceStruct> list = new FixedList8<ReferenceStruct>();
            list.Add(new ReferenceStruct("a"));
            list.Add(new ReferenceStruct("b"));

            list.Clear();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(0));
                Assert.That(list.Capacity, Is.EqualTo(InlineCapacity));
            }

            list.Add(new ReferenceStruct("c"));

            Assert.That(list[0].Value, Is.EqualTo("c"));
        }

        [Test]
        public void RemoveAtSwapBack_ThrowsForNegativeIndex()
        {
            using FixedList8<int> list = new FixedList8<int>();
            list.Add(1);

            ArgumentOutOfRangeException? exception = Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAtSwapBack(-1));

            Assert.That(exception?.ParamName, Is.EqualTo("index"));
        }

        [Test]
        public void RemoveAtSwapBack_ThrowsForIndexEqualToCountOnEmptyList()
        {
            using FixedList8<int> list = new FixedList8<int>();

            ArgumentOutOfRangeException? exception = Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAtSwapBack(0));

            Assert.That(exception?.ParamName, Is.EqualTo("index"));
        }

        [Test]
        public void RemoveAtSwapBack_ThrowsForIndexEqualToCountOnInlineList()
        {
            using FixedList8<int> list = new FixedList8<int>();
            list.Add(1);
            list.Add(2);

            ArgumentOutOfRangeException? exception = Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAtSwapBack(2));

            Assert.That(exception?.ParamName, Is.EqualTo("index"));
        }

        [Test]
        public void RemoveAtSwapBack_ThrowsForIndexEqualToCountOnHeapList()
        {
            using FixedList8<int> list = new FixedList8<int>();

            for (int i = 0; i < InlineCapacity + 1; i++)
            {
                list.Add(i);
            }

            ArgumentOutOfRangeException? exception = Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAtSwapBack(InlineCapacity + 1));

            Assert.That(exception?.ParamName, Is.EqualTo("index"));
        }

        [Test]
        public void RemoveAtSwapBack_RemovingLastInlineValueTypeElementOnlyDecrementsCount()
        {
            using FixedList8<int> list = new FixedList8<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);

            list.RemoveAtSwapBack(2);

            AssertSequence(list, 10, 20);
        }

        [Test]
        public void RemoveAtSwapBack_RemovingMiddleInlineValueTypeElementReplacesItWithLastElement()
        {
            using FixedList8<int> list = new FixedList8<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);
            list.Add(40);

            list.RemoveAtSwapBack(1);

            AssertSequence(list, 10, 40, 30);
        }

        [Test]
        public void RemoveAtSwapBack_RemovingFirstInlineReferenceTypeElementReplacesItWithLastElement()
        {
            using FixedList8<string?> list = new FixedList8<string?>();
            list.Add("first");
            list.Add(null);
            list.Add("last");

            list.RemoveAtSwapBack(0);

            AssertSequence<string?>(list, "last", null);
        }

        [Test]
        public void RemoveAtSwapBack_RemovingLastInlineReferenceTypeElementOnlyDecrementsCount()
        {
            using FixedList8<string?> list = new FixedList8<string?>();
            list.Add("first");
            list.Add(null);
            list.Add("last");

            list.RemoveAtSwapBack(2);

            AssertSequence<string?>(list, "first", null);
        }

        [Test]
        public void RemoveAtSwapBack_RemovingMiddleHeapValueTypeElementReplacesItWithLastElement()
        {
            using FixedList8<int> list = new FixedList8<int>();

            for (int i = 0; i < InlineCapacity + 4; i++)
            {
                list.Add(i);
            }

            list.RemoveAtSwapBack(2);

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(InlineCapacity + 3));
                Assert.That(list[0], Is.EqualTo(0));
                Assert.That(list[1], Is.EqualTo(1));
                Assert.That(list[2], Is.EqualTo(InlineCapacity + 3));
                Assert.That(list[3], Is.EqualTo(3));
            }
        }

        [Test]
        public void RemoveAtSwapBack_RemovingMiddleHeapReferenceTypeElementReplacesItWithLastElement()
        {
            using FixedList8<string?> list = new FixedList8<string?>();

            for (int i = 0; i < InlineCapacity + 4; i++)
            {
                list.Add($"value-{i}");
            }

            list.RemoveAtSwapBack(2);

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(InlineCapacity + 3));
                Assert.That(list[0], Is.EqualTo("value-0"));
                Assert.That(list[1], Is.EqualTo("value-1"));
                Assert.That(list[2], Is.EqualTo($"value-{InlineCapacity + 3}"));
                Assert.That(list[3], Is.EqualTo("value-3"));
            }
        }

        [Test]
        public void RemoveAtSwapBack_RemovingLastHeapReferenceTypeElementOnlyDecrementsCount()
        {
            using FixedList8<string?> list = new FixedList8<string?>();

            for (int i = 0; i < InlineCapacity + 4; i++)
            {
                list.Add($"value-{i}");
            }

            list.RemoveAtSwapBack(InlineCapacity + 3);

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(InlineCapacity + 3));
                Assert.That(list[InlineCapacity + 2], Is.EqualTo($"value-{InlineCapacity + 2}"));
            }
        }

        [Test]
        public void RemoveSwapBack_ReturnsFalseForEmptyList()
        {
            using FixedList8<int> list = new FixedList8<int>();

            bool removed = list.RemoveSwapBack(123);

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(removed, Is.False);
                Assert.That(list.Count, Is.EqualTo(0));
            }
        }

        [Test]
        public void RemoveSwapBack_ReturnsFalseWhenItemIsMissingAndDoesNotMutateList()
        {
            using FixedList8<int> list = new FixedList8<int>();
            list.Add(1);
            list.Add(2);
            list.Add(3);

            bool removed = list.RemoveSwapBack(99);

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(removed, Is.False);
                AssertSequence(list, 1, 2, 3);
            }
        }

        [Test]
        public void RemoveSwapBack_UsesDefaultComparerAndRemovesFirstMatchFromInlineList()
        {
            using FixedList8<int> list = new FixedList8<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);
            list.Add(20);

            bool removed = list.RemoveSwapBack(20);

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(removed, Is.True);
                AssertSequence(list, 10, 20, 30);
            }
        }

        [Test]
        public void RemoveSwapBack_UsesDefaultComparerAndRemovesFirstMatchFromHeapList()
        {
            using FixedList8<int> list = new FixedList8<int>();

            for (int i = 0; i < InlineCapacity + 4; i++)
            {
                list.Add(i);
            }

            bool removed = list.RemoveSwapBack(2);

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(removed, Is.True);
                Assert.That(list.Count, Is.EqualTo(InlineCapacity + 3));
                Assert.That(list[2], Is.EqualTo(InlineCapacity + 3));
            }
        }

        [Test]
        public void RemoveSwapBack_UsesCustomComparerWhenProvided()
        {
            using FixedList8<string> list = new FixedList8<string>();
            list.Add("Alpha");
            list.Add("Bravo");
            list.Add("Charlie");

            bool removedWithoutComparer = list.RemoveSwapBack("bravo");
            bool removedWithComparer = list.RemoveSwapBack("bravo", StringComparer.OrdinalIgnoreCase);

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(removedWithoutComparer, Is.False);
                Assert.That(removedWithComparer, Is.True);
                AssertSequence(list, "Alpha", "Charlie");
            }
        }

        [Test]
        public void RemoveSwapBack_RemovesNullReferenceValueWithDefaultComparer()
        {
            using FixedList8<string?> list = new FixedList8<string?>();
            list.Add("before");
            list.Add(null);
            list.Add("after");

            bool removed = list.RemoveSwapBack(null);

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(removed, Is.True);
                AssertSequence<string?>(list, "before", "after");
            }
        }

        [Test]
        public void RemoveSwapBack_StopsAfterRemovingFirstMatch()
        {
            using FixedList8<int> list = new FixedList8<int>();
            list.Add(1);
            list.Add(2);
            list.Add(2);
            list.Add(3);

            bool removed = list.RemoveSwapBack(2);

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(removed, Is.True);
                AssertSequence(list, 1, 3, 2);
            }
        }

        [Test]
        public void Dispose_InlineList_ResetsCountAndLeavesInlineCapacity()
        {
            FixedList8<int> list = new FixedList8<int>();
            list.Add(1);
            list.Add(2);

            list.Dispose();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(0));
                Assert.That(list.Capacity, Is.EqualTo(InlineCapacity));
            }

            list.Add(99);

            AssertSequence(list, 99);

            list.Dispose();
        }

        [Test]
        public void Dispose_HeapValueTypeList_ReturnsToInlineModeAndAllowsReuse()
        {
            FixedList8<int> list = new FixedList8<int>();

            for (int i = 0; i < InlineCapacity + 5; i++)
            {
                list.Add(i);
            }

            Assert.That(list.Capacity, Is.GreaterThan(InlineCapacity));

            list.Dispose();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(0));
                Assert.That(list.Capacity, Is.EqualTo(InlineCapacity));
            }

            list.Add(500);
            list.Add(600);

            AssertSequence(list, 500, 600);

            list.Dispose();
        }

        [Test]
        public void Dispose_HeapReferenceTypeList_ReturnsToInlineModeAndAllowsReuse()
        {
            FixedList8<string?> list = new FixedList8<string?>();

            for (int i = 0; i < InlineCapacity + 5; i++)
            {
                list.Add($"value-{i}");
            }

            Assert.That(list.Capacity, Is.GreaterThan(InlineCapacity));

            list.Dispose();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(0));
                Assert.That(list.Capacity, Is.EqualTo(InlineCapacity));
            }

            list.Add(null);
            list.Add("after-dispose");

            AssertSequence<string?>(list, null, "after-dispose");

            list.Dispose();
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            FixedList8<int> list = new FixedList8<int>();

            for (int i = 0; i < InlineCapacity + 3; i++)
            {
                list.Add(i);
            }

            list.Dispose();
            list.Dispose();
            list.Dispose();

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(0));
                Assert.That(list.Capacity, Is.EqualTo(InlineCapacity));
            }
        }

        [Test]
        public void MixedOperations_MaintainExpectedCountCapacityAndValues()
        {
            using FixedList8<int> list = new FixedList8<int>();

            for (int i = 0; i < InlineCapacity + 6; i++)
            {
                list.Add(i);
            }

            int heapCapacity = list.Capacity;

            list.RemoveAtSwapBack(0);
            list.RemoveSwapBack(5);
            list[1] = 1234;
            list.Add(9999);

			using ( Assert.EnterMultipleScope() ) {
                Assert.That(list.Count, Is.EqualTo(InlineCapacity + 5));
                Assert.That(list.Capacity, Is.EqualTo(heapCapacity));
                Assert.That(list[0], Is.EqualTo(InlineCapacity + 5));
                Assert.That(list[1], Is.EqualTo(1234));
                Assert.That(list[list.Count - 1], Is.EqualTo(9999));
            }
        }

        private static T ReadAt<T>(FixedList8<T> list, int index)
        {
            return list[index];
        }

        private static void AssertSequence<T>(FixedList8<T> list, params T[] expected)
        {
            Assert.That(list.Count, Is.EqualTo(expected.Length), "Count mismatch.");

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(list[i], Is.EqualTo(expected[i]), $"Mismatch at index {i}.");
            }
        }

        private readonly struct ReferenceStruct
        {
            public ReferenceStruct(string? value)
            {
                Value = value;
            }

            public string? Value { get; }
        }
    }
}
