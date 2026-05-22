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
using Nomad.Core.Util;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
	[TestFixture]
	[Category("Nomad.Core")]
	[Category("Collections")]
	[Category("Unit")]
	public class PackedBitSetTests
	{
		[Test]
		public void PackedBitSet_Constructor_ThrowsWhenBitCountIsZeroOrNegative()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new PackedBitSet(0));
			Assert.Throws<ArgumentOutOfRangeException>(() => new PackedBitSet(-1));
		}

		[Test]
		public void PackedBitSet_Get_ReturnsFalseForUnsetBits()
		{
			var bitSet = new PackedBitSet(130);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(bitSet.Get(0), Is.False);
				Assert.That(bitSet.Get(63), Is.False);
				Assert.That(bitSet.Get(64), Is.False);
				Assert.That(bitSet.Get(129), Is.False);
			}
		}

		[Test]
		public void PackedBitSet_Set_True_MarksOnlyRequestedBit()
		{
			var bitSet = new PackedBitSet(130);

			bitSet.Set(64, true);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(bitSet.Get(63), Is.False);
				Assert.That(bitSet.Get(64), Is.True);
				Assert.That(bitSet.Get(65), Is.False);
			}
		}

		[Test]
		public void PackedBitSet_Set_False_ClearsPreviouslySetBit()
		{
			var bitSet = new PackedBitSet(130);
			bitSet.Set(64, true);

			bitSet.Set(64, false);

			Assert.That(bitSet.Get(64), Is.False);
		}

		[Test]
		public void PackedBitSet_Set_HandlesBitsAcrossWordBoundaries()
		{
			var bitSet = new PackedBitSet(130);

			bitSet.Set(0, true);
			bitSet.Set(63, true);
			bitSet.Set(64, true);
			bitSet.Set(129, true);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(bitSet.Get(0), Is.True);
				Assert.That(bitSet.Get(63), Is.True);
				Assert.That(bitSet.Get(64), Is.True);
				Assert.That(bitSet.Get(129), Is.True);
				Assert.That(bitSet.Get(1), Is.False);
				Assert.That(bitSet.Get(62), Is.False);
				Assert.That(bitSet.Get(65), Is.False);
				Assert.That(bitSet.Get(128), Is.False);
			}
		}

		[Test]
		public void PackedBitSet_Clear_ResetsAllBits()
		{
			var bitSet = new PackedBitSet(130);
			bitSet.Set(0, true);
			bitSet.Set(63, true);
			bitSet.Set(64, true);
			bitSet.Set(129, true);

			bitSet.Clear();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(bitSet.Get(0), Is.False);
				Assert.That(bitSet.Get(63), Is.False);
				Assert.That(bitSet.Get(64), Is.False);
				Assert.That(bitSet.Get(129), Is.False);
			}
		}
	}
}
