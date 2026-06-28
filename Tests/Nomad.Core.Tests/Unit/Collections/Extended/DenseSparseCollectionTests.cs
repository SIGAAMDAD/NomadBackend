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
    public sealed class DenseSparseCollectionTests
    {
        [Test]
        public void DenseIdSet_AddRemoveContainsClear_TracksOccupancyAcrossGrowth()
        {
            var set = new DenseIdSet(initialCapacity: 1);

            bool first = set.Add(0);
            bool duplicate = set.Add(0);
            bool grown = set.Add(130);
            bool removedMissing = set.Remove(99);
            bool removed = set.Remove(0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.True);
                Assert.That(duplicate, Is.False);
                Assert.That(grown, Is.True);
                Assert.That(removedMissing, Is.False);
                Assert.That(removed, Is.True);
                Assert.That(set.Contains(0), Is.False);
                Assert.That(set.Contains(130), Is.True);
                Assert.That(set.Count, Is.EqualTo(1));
                Assert.That(set.Capacity, Is.GreaterThanOrEqualTo(131));
            }

            set.Clear();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(set.Count, Is.EqualTo(0));
                Assert.That(set.Contains(130), Is.False);
            }
        }

        [Test]
        public void DenseIdMap_SetTryGetRemoveClear_TracksValuesAndReferenceCleanup()
        {
            var map = new DenseIdMap<string>(initialCapacity: 1);

            map.Set(2, "two");
            map.Set(130, "one-thirty");
            map.Set(2, "updated");
            bool found = map.TryGetValue(2, out string value);
            bool missing = map.TryGetValue(3, out string missingValue);
            bool removed = map.Remove(2);
            bool removedAgain = map.Remove(2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(found, Is.True);
                Assert.That(value, Is.EqualTo("updated"));
                Assert.That(missing, Is.False);
                Assert.That(missingValue, Is.Null);
                Assert.That(removed, Is.True);
                Assert.That(removedAgain, Is.False);
                Assert.That(map.Contains(2), Is.False);
                Assert.That(map.Contains(130), Is.True);
                Assert.That(map.Count, Is.EqualTo(1));
            }

            map.Clear();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(map.Count, Is.EqualTo(0));
                Assert.That(map.Contains(130), Is.False);
            }
        }

        [Test]
        public void SparseSet_AddRemoveDenseIdsAndClear_MaintainsDenseRepresentation()
        {
            var set = new SparseSet(idCapacity: 1, denseCapacity: 1);

            bool addedTwo = set.Add(2);
            bool duplicateTwo = set.Add(2);
            bool addedFive = set.Add(5);
            bool removedMissing = set.Remove(9);
            bool removedTwo = set.Remove(2);
            int[] array = set.ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(addedTwo, Is.True);
                Assert.That(duplicateTwo, Is.False);
                Assert.That(addedFive, Is.True);
                Assert.That(removedMissing, Is.False);
                Assert.That(removedTwo, Is.True);
                Assert.That(set.Count, Is.EqualTo(1));
                Assert.That(set.Contains(2), Is.False);
                Assert.That(set.Contains(5), Is.True);
                Assert.That(set.GetDenseId(0), Is.EqualTo(5));
                Assert.That(array, Is.EqualTo(new[] { 5 }));
            }

            set.Clear();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(set.Count, Is.EqualTo(0));
                Assert.That(set.Contains(5), Is.False);
            }
        }

        [Test]
        public void DirtySet_MarkDirtyMarkCleanAndClear_TrackDirtyIds()
        {
            var dirty = new DirtySet(idCapacity: 1, dirtyCapacity: 1);

            bool first = dirty.MarkDirty(7);
            bool duplicate = dirty.MarkDirty(7);
            bool second = dirty.MarkDirty(9);
            bool cleanMissing = dirty.MarkClean(3);
            bool cleanExisting = dirty.MarkClean(7);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first, Is.True);
                Assert.That(duplicate, Is.False);
                Assert.That(second, Is.True);
                Assert.That(cleanMissing, Is.False);
                Assert.That(cleanExisting, Is.True);
                Assert.That(dirty.IsDirty(7), Is.False);
                Assert.That(dirty.IsDirty(9), Is.True);
                Assert.That(dirty.Count, Is.EqualTo(1));
                Assert.That(dirty.GetDirtyId(0), Is.EqualTo(9));
            }

            dirty.ClearDirty();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dirty.Count, Is.EqualTo(0));
                Assert.That(dirty.IsDirty(9), Is.False);
            }
        }

        [Test]
        public void BitMatrix_SetGetAndClear_MapsTwoDimensionalBits()
        {
            var matrix = new BitMatrix(3, 2);

            matrix.Set(0, 0, true);
            matrix.Set(2, 1, true);
            matrix.Set(0, 0, false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(matrix.Width, Is.EqualTo(3));
                Assert.That(matrix.Height, Is.EqualTo(2));
                Assert.That(matrix.Get(0, 0), Is.False);
                Assert.That(matrix.Get(2, 1), Is.True);
            }

            matrix.Clear();

            Assert.That(matrix.Get(2, 1), Is.False);
        }

        [Test]
        public void BitMatrix_InvalidDimensions_ThrowArgumentOutOfRangeException()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new BitMatrix(0, 1));
                Assert.Throws<ArgumentOutOfRangeException>(() => new BitMatrix(1, 0));
            }
        }
    }
}
