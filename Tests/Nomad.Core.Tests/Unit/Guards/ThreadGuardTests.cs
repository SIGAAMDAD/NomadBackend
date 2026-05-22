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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
	[TestFixture]
	[Category("Nomad.Core")]
	[Category("Guards")]
	[Category("Unit")]
	public class ThreadGuardTests
	{
		[Test]
		public void ThreadGuard_ThrowIfNotMainThread_ThrowsFromDedicatedThread()
		{
			var thread = new Thread(() => Assert.Throws<InvalidOperationException>(() => ThreadGuard.ThrowIfNotMainThread()));
			thread.Start();
		}

		[Test]
		public void ThreadGuard_ThrowIfNotMainThread_ThrowsFromTask()
		{
			Task.Run(() => Assert.Throws<InvalidOperationException>(() => ThreadGuard.ThrowIfNotMainThread()));
		}

		[Test]
		public void ThreadGuard_ThrowIfNotMainThread_DoesNotThrowFromMainThread()
		{
			Assert.DoesNotThrow(() => ThreadGuard.ThrowIfNotMainThread());
		}

		[Test]
		public void ThreadGuard_ThrowIfWrongThread_ThrowsFromSeparateThread()
		{
			int threadId = Environment.CurrentManagedThreadId;
			var thread = new Thread(() => Assert.Throws<InvalidOperationException>(() => ThreadGuard.ThrowIfWrongThread(threadId)));
		}

		[Test]
		public void ThreadGuard_ThrowIfWrongThread_DoesNotThreadFromCorrectThread()
		{
			Assert.DoesNotThrow(() => ThreadGuard.ThrowIfWrongThread(Environment.CurrentManagedThreadId));
		}
	}
}