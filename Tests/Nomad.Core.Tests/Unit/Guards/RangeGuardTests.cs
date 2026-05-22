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
using Nomad.Core.Compatibility.Guards;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
	[TestFixture]
	[Category("Nomad.Core")]
	[Category("Guards")]
	[Category("Unit")]
	[Category("UnitTests")]
	public class RangeGuardTests
	{
		private static void EnsureThrows(Action callback)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => callback());
		}

		private static void EnsureDoesNotThrow(Action callback)
		{
			Assert.DoesNotThrow(() => callback());
		}

#region LessThanOrEqual
		[Test]
		public void RangeGuard_ThrowIfLessThanOrEqual_ThrowsWhenLessThan()
		{
			EnsureThrows(() => RangeGuard.ThrowIfLessThanOrEqual(-1, 0));
		}

		[Test]
		public void RangeGuard_ThrowIfLessThanOrEqual_ThrowsWhenEqual()
		{
			EnsureThrows(() => RangeGuard.ThrowIfLessThanOrEqual(0, 0));
		}

		[Test]
		public void RangeGuard_ThrowIfLessThanOrEqual_DoesNotThrowWhenGreaterThan()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfLessThanOrEqual(1, 0));
		}
#endregion

#region LessThan
		[Test]
		public void RangeGuard_ThrowIfLessThan_ThrowsWhenLessThan()
		{
			EnsureThrows(() => RangeGuard.ThrowIfLessThan(-1, 0));
		}

		[Test]
		public void RangeGuard_ThrowIfLessThan_DoesNotThrowWhenEqual()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfLessThan(0, 0));
		}

		[Test]
		public void RangeGuard_ThrowIfLessThan_DoesNotThrowWhenGreaterThan()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfLessThan(1, 0));
		}
#endregion

#region GreaterThanOrEqual
		[Test]
		public void RangeGuard_ThrowIfGreaterThanOrEqual_ThrowsWhenGreaterThan()
		{
			EnsureThrows(() => RangeGuard.ThrowIfGreaterThanOrEqual(1, 0));
		}

		[Test]
		public void RangeGuard_ThrowIfGreaterThanOrEqual_ThrowsWhenEqual()
		{
			EnsureThrows(() => RangeGuard.ThrowIfGreaterThanOrEqual(0, 0));
		}

		[Test]
		public void RangeGuard_ThrowIfGreaterThanOrEqual_DoesNotThrowWhenLessThan()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfGreaterThanOrEqual(-1, 0));
		}
#endregion

#region GreaterThan
		[Test]
		public void RangeGuard_ThrowIfGreaterThan_ThrowsWhenGreaterThan()
		{
			EnsureThrows(() => RangeGuard.ThrowIfGreaterThan(1, 0));
		}

		[Test]
		public void RangeGuard_ThrowIfGreaterThan_DoesNotThrowWhenEqual()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfGreaterThan(0, 0));
		}

		[Test]
		public void RangeGuard_ThrowIfGreaterThanOr_DoesNotThrowWhenLessThan()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfGreaterThan(-1, 0));
		}
#endregion

#region Negative Int
		[Test]
		public void RangeGuard_ThrowIfNegative_Int_ThrowsWhenNegative()
		{
			EnsureThrows(() => RangeGuard.ThrowIfNegative(-1));
		}

		[Test]
		public void RangeGuard_ThrowIfNegative_Int_DoesNotThrowWhenZero()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfNegative(0));
		}

		[Test]
		public void RangeGuard_ThrowIfNegative_Int_DoesNotThrowWhenPositive()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfNegative(1));
		}
#endregion

#region Negative Float
		[Test]
		public void RangeGuard_ThrowIfNegative_Float_ThrowsWhenNegative()
		{
			EnsureThrows(() => RangeGuard.ThrowIfNegative(-1.0f));
		}

		[Test]
		public void RangeGuard_ThrowIfNegative_Float_DoesNotThrowWhenZero()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfNegative(0.0f));
		}

		[Test]
		public void RangeGuard_ThrowIfNegative_Float_DoesNotThrowWhenPositive()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfNegative(1.0f));
		}
#endregion

#region NegativeOrZero Int
		[Test]
		public void RangeGuard_ThrowIfNegativeOrZero_Int_ThrowsWhenNegative()
		{
			EnsureThrows(() => RangeGuard.ThrowIfNegativeOrZero(-1));
		}

		[Test]
		public void RangeGuard_ThrowIfNegativeOrZero_Int_ThrowsWhenZero()
		{
			EnsureThrows(() => RangeGuard.ThrowIfNegativeOrZero(0));
		}

		[Test]
		public void RangeGuard_ThrowIfNegativeOrZero_Int_DoesNotThrowWhenPositive()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfNegativeOrZero(1));
		}
#endregion

#region NegativeOrZero Float
		[Test]
		public void RangeGuard_ThrowIfNegativeOrZero_Float_ThrowsWhenNegative()
		{
			EnsureThrows(() => RangeGuard.ThrowIfNegativeOrZero(-1.0f));
		}

		[Test]
		public void RangeGuard_ThrowIfNegativeOrZero_Float_ThrowsWhenZero()
		{
			EnsureThrows(() => RangeGuard.ThrowIfNegativeOrZero(0.0f));
		}

		[Test]
		public void RangeGuard_ThrowIfNegativeOrZero_Float_DoesNotThrowWhenPositive()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfNegativeOrZero(1.0f));
		}
#endregion

#region Zero Int
		[Test]
		public void RangeGuard_ThrowIfZero_Int_ThrowsWhenZero()
		{
			EnsureThrows(() => RangeGuard.ThrowIfZero(0));
		}

		[Test]
		public void RangeGuard_ThrowIfZero_Int_DoesNotThrowWhenNotZero()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfZero(1));
		}
#endregion

#region Zero Float
		[Test]
		public void RangeGuard_ThrowIfZero_Float_ThrowsWhenZero()
		{
			EnsureThrows(() => RangeGuard.ThrowIfZero(0.0f));
		}

		[Test]
		public void RangeGuard_ThrowIfZero_Float_DoesNotThrowWhenNotZero()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfZero(1.0f));
		}
#endregion

#region OutOfRange Int
		[Test]
		public void RangeGuard_ThrowIfOutOfRange_Int_ThrowsWhenOutOfRange()
		{
			EnsureThrows(() => RangeGuard.ThrowIfOutOfRange(-1, 0, 1));
		}

		[Test]
		public void RangeGuard_ThrowIfOutOfRange_Int_DoesNotThrowWhenInRange()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfOutOfRange(0, 0, 1));
		}
#endregion

#region OutOfRange Float
		[Test]
		public void RangeGuard_ThrowIfOutOfRange_Float_ThrowsWhenOutOfRange()
		{
			EnsureThrows(() => RangeGuard.ThrowIfOutOfRange(-1.0f, 0.0f, 1.0f));
		}

		[Test]
		public void RangeGuard_ThrowIfOutOfRange_Float_DoesNotThrowWhenInRange()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfOutOfRange(0.0f, 0.0f, 1.0f));
		}
#endregion

#region Equal Int
		[Test]
		public void RangeGuard_ThrowIfEqual_Int_ThrowsWhenEqual()
		{
			EnsureThrows(() => RangeGuard.ThrowIfEqual(0, 0));
		}

		[Test]
		public void RangeGuard_ThrowIfEqual_Int_DoesNotThrowWhenNotEqual()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfEqual(0, 1));
		}
#endregion

#region Equal Float
		[Test]
		public void RangeGuard_ThrowIfEqual_Float_ThrowsWhenEqual()
		{
			EnsureThrows(() => RangeGuard.ThrowIfEqual(0.0f, 0.0f));
		}

		[Test]
		public void RangeGuard_ThrowIfEqual_Float_DoesNotThrowWhenNotEqual()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfEqual(0.0f, 1.0f));
		}
#endregion

#region NotEqual Int
		[Test]
		public void RangeGuard_ThrowIfNotEqual_Int_ThrowsWhenNotEqual()
		{
			EnsureThrows(() => RangeGuard.ThrowIfNotEqual(0, 1));
		}

		[Test]
		public void RangeGuard_ThrowIfNotEqual_Int_DoesNotThrowWhenEqual()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfNotEqual(0, 0));
		}
#endregion

#region NotEqual Float
		[Test]
		public void RangeGuard_ThrowIfNotEqual_Float_ThrowsWhenNotEqual()
		{
			EnsureThrows(() => RangeGuard.ThrowIfNotEqual(0.0f, 1.0f));
		}

		[Test]
		public void RangeGuard_ThrowIfNotEqual_Float_DoesNotThrowWhenEqual()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfNotEqual(0.0f, 0.0f));
		}
#endregion

#region PowerOfTwo
		[Test]
		public void RangeGuard_ThrowIfNotPowerOfTwo_ThrowsWhenNotPowerOfTwo()
		{
			EnsureThrows(() => RangeGuard.ThrowIfNotPowerOfTwo(47));
		}

		[Test]
		public void RangeGuard_ThrowIfNotPowerOfTwo_DoesNotThrowWhenPowerOfTwo()
		{
			EnsureDoesNotThrow(() => RangeGuard.ThrowIfNotPowerOfTwo(32));
		}
#endregion
	}
}