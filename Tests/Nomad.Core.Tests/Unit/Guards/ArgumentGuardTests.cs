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
	public class ArgumentGuardTests
	{
		private static void EnsureThrowsNull(Action callback)
		{
			Assert.Throws<ArgumentNullException>(() => callback());
		}

		private static void EnsureThrows(Action callback)
		{
			Assert.Throws<ArgumentException>(() => callback());
		}

		private static void EnsureDoesNotThrow(Action callback)
		{
			Assert.DoesNotThrow(() => callback());
		}

#region ThrowIfNull
		[Test]
		public void ArgumentGuard_ThrowIfNull_ThrowsWhenNull()
		{
			EnsureThrowsNull(() => ArgumentGuard.ThrowIfNull(null));
		}

		[Test]
		public void ArgumentGuard_ThrowIfNull_DoesNotThrowWhenNotNull()
		{
			object obj = new object();
			EnsureDoesNotThrow(() => ArgumentGuard.ThrowIfNull(obj));
		}
#endregion

#region ThrowIfNullOrEmpty
		[Test]
		public void ArgumentGuard_ThrowIfNullOrEmpty_ThrowsWhenNull()
		{
			EnsureThrowsNull(() => ArgumentGuard.ThrowIfNullOrEmpty(null));
		}

		[Test]
		public void ArgumentGuard_ThrowIfNullOrEmpty_ThrowsWhenEmpty()
		{
			EnsureThrows(() => ArgumentGuard.ThrowIfNullOrEmpty(string.Empty));
		}

		[Test]
		public void ArgumentGuard_ThrowIfNullOrEmpty_DoesNotThrowWhenNotNullOrEmpty()
		{
			string str = "Test";
			EnsureDoesNotThrow(() => ArgumentGuard.ThrowIfNullOrEmpty(str));
		}
#endregion

#region ThrowIfNullOrWhitespace
		[Test]
		public void ArgumentGuard_ThrowIfNullOrWhitespace_ThrowsWhenNull()
		{
			EnsureThrowsNull(() => ArgumentGuard.ThrowIfNullOrWhiteSpace(null));
		}

		[Test]
		public void ArgumentGuard_ThrowIfNullOrWhitespace_ThrowsWhenWhitespace()
		{
			EnsureThrows(() => ArgumentGuard.ThrowIfNullOrWhiteSpace("   "));
		}

		[Test]
		public void ArgumentGuard_ThrowIfNullOrWhitespace_DoesNotThrowWhenNotNullOrWhitespace()
		{
			string str = "Test";
			EnsureDoesNotThrow(() => ArgumentGuard.ThrowIfNullOrWhiteSpace(str));
		}
#endregion

#region ThrowIfDefault
		[Test]
		public void ArgumentGuard_ThrowIfDefault_ThrowsIfDefault()
		{
			int value = default;
			EnsureThrows(() => ArgumentGuard.ThrowIfDefault(value));
		}

		[Test]
		public void ArgumentGuard_ThrowIfDefault_DoesNotThrowIfNotDefault()
		{
			int value = 21;
			EnsureDoesNotThrow(() => ArgumentGuard.ThrowIfDefault(value));
		}
#endregion
	}
}