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
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Nomad.Core.Compatibility.Guards;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
	[TestFixture]
	[Category("Nomad.Core")]
	[Category("Guards")]
	[Category("Unit")]
	public class MemoryGuardTests
	{
		[Test]
		public void MemoryGuard_ThrowIfUnaligned_ThrowsWhenUnaligned()
		{
			int alignment = 16;
			IntPtr ptr = Marshal.AllocHGlobal(alignment * 2 + 16);
			nuint addr = (nuint)ptr;
			nuint aligned = (addr + (nuint)(alignment - 1)) & ~(nuint)(alignment - 1);
			IntPtr unaligned = (IntPtr)(aligned + 1);

			Assert.Throws<ArgumentException>(() => MemoryGuard.ThrowIfUnaligned(unaligned, alignment));
			Marshal.FreeHGlobal(ptr);
		}

		[Test]
		public void MemoryGuard_ThrowIfUnaligned_DoesNotThrowWhenAligned()
		{
			unsafe {
				int size = 64;
				int alignment = 16;
				int alignedSize = ( ( size ) + ( alignment ) - 1 ) & ~( ( alignment ) - 1 );
				IntPtr ptr = Marshal.AllocHGlobal(alignedSize);
				Assert.DoesNotThrow(() => MemoryGuard.ThrowIfUnaligned(ptr, alignment));
				Marshal.FreeHGlobal(ptr);
			}
		}

		[Test]
		public void MemoryGuard_ThrowIfNullPtr_ThrowsWhenNull()
		{
			Assert.Throws<ArgumentNullException>(() => MemoryGuard.ThrowIfNullPtr(IntPtr.Zero));
		}

		[Test]
		public void MemoryGuard_ThrowIfNullPtr_DoesNotThrowWhenValidPtr()
		{
			IntPtr ptr = Marshal.AllocHGlobal(24);
			Assert.DoesNotThrow(() => MemoryGuard.ThrowIfNullPtr(ptr));
			Marshal.FreeHGlobal(ptr);
		}
	}
}