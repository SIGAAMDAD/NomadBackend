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
	public class StateGuardTests
	{
		[Test]
		public void StateGuard_ThrowIfTrue_ThrowsWhenTrue()
		{
			Assert.Throws<InvalidOperationException>(() => StateGuard.ThrowIfTrue(true));
		}

		[Test]
		public void StateGuard_ThrowIfTrue_DoesNotThrowWhenFalse()
		{
			Assert.DoesNotThrow(() => StateGuard.ThrowIfTrue(false));
		}

		[Test]
		public void StateGuard_ThrowIfFalse_ThrowsWhenFalse()
		{
			Assert.Throws<InvalidOperationException>(() => StateGuard.ThrowIfFalse(false));
		}

		[Test]
		public void StateGuard_ThrowIfFalse_DoesNotThrowWhenTrue()
		{
			Assert.DoesNotThrow(() => StateGuard.ThrowIfFalse(true));
		}
	}
}